using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;
using UnityEngine.XR;

public class MotionDataProvider : MonoBehaviour {
    [DllImport(AirVRClient.LibPluginName)]
    private static extern void onairvr_BeginGatherInput(ref long timestamp);

    [DllImport(AirVRClient.LibPluginName)]
    private static extern void onairvr_BeginGatherInputWithTimestamp(long timestamp);

    [Serializable]
    private class PredictionConfig {
        public string motionDataInputEndpoint;
        public bool useLoopbackForPredictedMotionOutput;
        public string motionDataPerSec;
    }

    [Serializable]
    private class PredictionConfigReader {
        [SerializeField] private PredictionConfig prediction;

        public void ReadConfig(string fileFrom, PredictionConfig to) {
            try {
                prediction = to;
                JsonUtility.FromJsonOverwrite(File.ReadAllText(fileFrom), this);
            }
            catch (Exception e) {
                Debug.Log("[ERROR] failed to read prediction config : " + e.ToString());
            }
        }
    }

    public static MotionDataProvider instance { get; private set; }

    public static void LoadOnce(AirVRProfileBase profile) {
        if (instance == null) {
            GameObject go = new GameObject("MotionDataProvider");
            MotionDataProvider provider = go.AddComponent<MotionDataProvider>();
            Debug.Assert(instance != null);

            provider._profile = profile;
        }
    }

    private AirVRProfileBase _profile;
    private byte[] _cameraProjection;
    private SensorDeviceManager _sensorDeviceManager;
    private string _motionDataInputEndpoint;
    private PushSocket _zmqPushMotionData;
    private PushSocket _zmqPushProfile;
    private NetMQ.Msg _msgMotionData;
    private List<byte[]> _motionData;
    private bool _120HzMotionDataMode = true;

    private bool shouldReportProfile {
        get { return string.IsNullOrEmpty(_profile.profileReportEndpoint) == false; }
    }
    
    public string predictedMotionOutputEndpoint { get; private set; }

    private string convertEndpoint(string endpoint, bool loopback, int portDelta = 0) {
        string[] tokens = endpoint.Split(':');
        Debug.Assert(tokens.Length == 3);

        // use TCP transport layer for AMQP endpoint
        if (tokens[0].Equals("amqp")) {
            tokens[0] = "tcp";
        }

        return tokens[0] + ":" + (loopback ? "//127.0.0.1" : tokens[1]) + ":" + (int.Parse(tokens[2]) + portDelta).ToString();
    }

    void Awake() {
        if (instance != null) {
            new UnityException("[MotionDataProvider] ERROR: There must exist only one MotionDataProvider instance.");
        }
        instance = this;

        if (File.Exists(AirVRCamera.ConfigFile)) {
            PredictionConfig config = new PredictionConfig();
            new PredictionConfigReader().ReadConfig(AirVRCamera.ConfigFile, config);

            _motionDataInputEndpoint = convertEndpoint(config.motionDataInputEndpoint, false);
            predictedMotionOutputEndpoint = convertEndpoint(
                _motionDataInputEndpoint,
                config.useLoopbackForPredictedMotionOutput, 
                1
            );
            _120HzMotionDataMode = !config.motionDataPerSec.Equals("60");
        }
        else {
            _motionDataInputEndpoint = convertEndpoint("amqp://192.168.0.20:5555", false);
            predictedMotionOutputEndpoint = convertEndpoint(_motionDataInputEndpoint, false, 1);
        }

        _sensorDeviceManager = gameObject.AddComponent<SensorDeviceManager>();

        _zmqPushMotionData = new PushSocket();
        _zmqPushProfile = new PushSocket();
        _msgMotionData = new NetMQ.Msg();
        _motionData = new List<byte[]>();

        AirVRClient.MessageReceived += onAirVRMessageReceived; 
        
        DontDestroyOnLoad(gameObject);
    }

    void Start() {
        var cameraProjection = _profile.leftEyeCameraNearPlane;
        _cameraProjection = new byte[cameraProjection.Length * sizeof(float)];

        for (int i = 0, pos = 0; i < cameraProjection.Length; i++) {
            var value = BitConverter.GetBytes(cameraProjection[i]);
            Array.Reverse(value);

            Array.Copy(value, 0, _cameraProjection, pos, value.Length);
            pos += value.Length;
        }

        _zmqPushMotionData.Connect(_motionDataInputEndpoint);

        if (shouldReportProfile) {
            _zmqPushProfile.Connect(
                convertEndpoint(_profile.profileReportEndpoint, false)
            );
        }
    }

    void Update() {
        byte[] motionData = _sensorDeviceManager.getNextMotionData();
        while (motionData != null) {
            _motionData.Add(motionData);
            motionData = _sensorDeviceManager.getNextMotionData();
        }

        if (_motionData.Count > 0) {
            if (_120HzMotionDataMode) {
                updateIn120HzMode();
            }
            else {
                updateIn60HzMode();
            }
        }
    }

    void OnDestroy() {
        _zmqPushMotionData.Close();
        _msgMotionData.Close();
        
        _zmqPushMotionData.Dispose();

        if (shouldReportProfile) {
            _zmqPushProfile.Close();
            _zmqPushProfile.Dispose();
        }
        
        NetMQ.NetMQConfig.Cleanup(false);
    }

    private void updateIn60HzMode() {
        int lastIndex = _motionData.Count - 1;

        long timestamp = 0;
        onairvr_BeginGatherInput(ref timestamp);

        Debug.Log("update 60Hz timestamp: " + timestamp);

        MotionData.SetTimestamp(_motionData[lastIndex], timestamp);
        MotionData.SetOrientation(_motionData[lastIndex], InputTracking.GetLocalRotation(XRNode.CenterEye));

        sendMotionData(_motionData[lastIndex]);

        _motionData.Clear();
    }

    private void updateIn120HzMode() {
        Quaternion baseOvrOrientation = InputTracking.GetLocalRotation(XRNode.CenterEye);
        Quaternion baseSensorOrientation = MotionData.GetOrientation(_motionData[_motionData.Count - 1]);

        for (int i = 0; i < _motionData.Count; i++) {
            onairvr_BeginGatherInputWithTimestamp(MotionData.GetTimestamp(_motionData[i]));

            MotionData.SetOrientation(_motionData[i], baseOvrOrientation *
                                                      Quaternion.Inverse(baseSensorOrientation) *
                                                      MotionData.GetOrientation(_motionData[i]));

            sendMotionData(_motionData[i]);
        }

        _motionData.Clear();
    }

    private void sendMotionData(byte[] data) {
        _msgMotionData.InitPool(data.Length + _cameraProjection.Length);
        Array.Copy(data, _msgMotionData.Data, data.Length);
        Array.Copy(_cameraProjection, 0, _msgMotionData.Data, data.Length, _cameraProjection.Length);

        _zmqPushMotionData.TrySend(ref _msgMotionData, TimeSpan.Zero, false);
    }
    
    // handle AirVRClientMessages
    private void onAirVRMessageReceived(AirVRClientMessage message) {
        if (shouldReportProfile == false) {
            return;
        }

        if (message.From.Equals(AirVRClientMessage.FromMediaStream)) {
            if (message.Name.Equals("Profile")) {
                byte[] data = Convert.FromBase64String(message.Data); 
                
                _zmqPushProfile.TrySendFrame(data);
            }
        }
    }
}
