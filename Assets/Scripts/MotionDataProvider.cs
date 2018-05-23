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
    private const string ConfigFile = "/sdcard/onairvr/config.json";

    [Serializable]
    private struct Config {
        public string MotionDataInputEndpoint;
        public bool UseLoopbackForPredictedMotionOutput;
    }

    [Serializable]
    private struct Report {
        public float OverallLatency;
    }

    public static MotionDataProvider instance { get; private set; }

    public static void LoadOnce() {
        if (instance == null) {
            GameObject go = new GameObject("MotionDataProvider");
            go.AddComponent<MotionDataProvider>();
            Debug.Assert(instance != null);
        }
    }

    private SensorDeviceManager _sensorDeviceManager;
    private string _motionDataInputEndpoint;
    private string _statsReportEndpoint;
    private PushSocket _zmqPushMotionData;
    private PushSocket _zmqPushStats;
    private NetMQ.Msg _msgMotionData;
    private List<byte[]> _motionData;

    public string predictedMotionOutputEndpoint { get; private set; }

    private string convertEndpoint(string endpoint, bool loopback, int portDelta) {
        string[] tokens = endpoint.Split(':');
        Debug.Assert(tokens.Length == 3);

        return tokens[0] + ":" + (loopback ? "//127.0.0.1" : tokens[1]) + ":" + (int.Parse(tokens[2]) + portDelta).ToString();
    }

    void Awake() {
        if (instance != null) {
            new UnityException("[MotionDataProvider] ERROR: There must exist only one MotionDataProvider instance.");
        }
        instance = this;

        if (File.Exists(ConfigFile)) {
            Config config = JsonUtility.FromJson<Config>(File.ReadAllText(ConfigFile));
            
            _motionDataInputEndpoint = config.MotionDataInputEndpoint;
            predictedMotionOutputEndpoint = convertEndpoint(_motionDataInputEndpoint, config.UseLoopbackForPredictedMotionOutput, 2);
        }
        else {
            _motionDataInputEndpoint = "tcp://192.168.0.20:5555";
            predictedMotionOutputEndpoint = convertEndpoint(_motionDataInputEndpoint, false, 2);
        }

        _statsReportEndpoint = convertEndpoint(_motionDataInputEndpoint, false, 1);
                    
        _sensorDeviceManager = gameObject.AddComponent<SensorDeviceManager>();
        _zmqPushMotionData = new PushSocket();
        _zmqPushStats = new PushSocket();
        _msgMotionData = new NetMQ.Msg();
        _motionData = new List<byte[]>();

        AirVRClient.MessageReceived += onAirVRMessageReceived; 
        
        DontDestroyOnLoad(gameObject);
    }

    void Start() {
        _zmqPushMotionData.Connect(_motionDataInputEndpoint);
        _zmqPushStats.Connect(_statsReportEndpoint);
    }

    void Update() {
        byte[] motionData = _sensorDeviceManager.getNextMotionData();
        while (motionData != null) {
            _motionData.Add(motionData);
            motionData = _sensorDeviceManager.getNextMotionData();
        }

        if (_motionData.Count > 0) {
            Quaternion baseOvrOrientation = InputTracking.GetLocalRotation(XRNode.CenterEye);
            Quaternion baseSensorOrientation = MotionData.GetOrientation(_motionData[_motionData.Count -1]);
            
            for (int i = 0; i < _motionData.Count; i++) {
                MotionData.SetOrientation(_motionData[i], Quaternion.Inverse(baseSensorOrientation) *
                                                          MotionData.GetOrientation(_motionData[i]) *
                                                          baseOvrOrientation);
                
                _msgMotionData.InitPool(_motionData[i].Length);
                Array.Copy(_motionData[i], _msgMotionData.Data, _motionData[i].Length);

                _zmqPushMotionData.TrySend(ref _msgMotionData, TimeSpan.Zero, false);
            }
            
            _motionData.Clear();
        }
    }

    void OnDestroy() {
        _zmqPushMotionData.Close();
        _zmqPushStats.Close();
        _msgMotionData.Close();
        
        _zmqPushMotionData.Dispose();
        _zmqPushStats.Dispose();
        
        NetMQ.NetMQConfig.Cleanup(false);
    }
    
    // handle AirVRClientMessages
    private void onAirVRMessageReceived(AirVRClientMessage message) {
        if (message.From.Equals(AirVRClientMessage.FromMediaStream)) {
            if (message.Name.Equals("Stats")) {
                Report report = new Report();
                report.OverallLatency = message.LatencyFromInputToRenderVideo;
                
                _zmqPushStats.TrySendFrame(JsonUtility.ToJson(report));
            }
        }
    }
}
