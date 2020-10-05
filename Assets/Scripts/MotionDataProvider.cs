using System;
using System.IO;
using System.Runtime.InteropServices;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;
using UnityEngine.XR;

public class MotionDataProvider : MonoBehaviour {
    [DllImport(AirVRClient.LibPluginName)]
    private static extern void ocs_BeginGatherInput(ref long timestamp);

    [DllImport(AirVRClient.LibPluginName)]
    private static extern void ocs_BeginGatherInputWithTimestamp(long timestamp);

    [Serializable]
    private class PredictionConfig {
        public string motionDataInputEndpoint;
        public bool useLoopbackForPredictedMotionOutput;
        public bool bypassPrediction;
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
    private bool _bypassPrediction;
    private MotionData _motionData;
    private string _motionDataInputEndpoint;
    private PushSocket _zmqPushMotionData;
    private PushSocket _zmqPushProfile;
    private NetMQ.Msg _msgMotionData;

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

            _bypassPrediction = config.bypassPrediction;
            _motionDataInputEndpoint = convertEndpoint(config.motionDataInputEndpoint, false);
            predictedMotionOutputEndpoint = convertEndpoint(
                _motionDataInputEndpoint,
                config.useLoopbackForPredictedMotionOutput, 
                1
            );
        }
        else {
            _motionDataInputEndpoint = convertEndpoint("amqp://192.168.0.20:5555", false);
            predictedMotionOutputEndpoint = convertEndpoint(_motionDataInputEndpoint, false, 1);
        }

        if (_bypassPrediction) { return; }

        _motionData = new MotionData();
        _zmqPushMotionData = new PushSocket();
        _zmqPushProfile = new PushSocket();
        _msgMotionData = new NetMQ.Msg();

        AirVRClient.MessageReceived += onAirVRMessageReceived; 
        
        DontDestroyOnLoad(gameObject);
    }

    void Start() {
        if (_bypassPrediction) { return; }

        _motionData.cameraProjection = _profile.leftEyeCameraNearPlane;

        _zmqPushMotionData.Connect(_motionDataInputEndpoint);

        if (shouldReportProfile) {
            _zmqPushProfile.Connect(
                convertEndpoint(_profile.profileReportEndpoint, false)
            );
        }
    }

    void LateUpdate() {
        if (_bypassPrediction || OVRManager.isHmdPresent == false) { return; }

        long timestamp = 0;
        ocs_BeginGatherInput(ref timestamp);

        _motionData.timestamp = timestamp;
        _motionData.leftEyePosition = getOvrNodePosition(XRNode.LeftEye, OVRPlugin.Node.EyeLeft);
        _motionData.rightEyePosition = getOvrNodePosition(XRNode.RightEye, OVRPlugin.Node.EyeRight);
        _motionData.headOrientation = getOvrNodeOrientation(XRNode.CenterEye, OVRPlugin.Node.EyeCenter);
        _motionData.headAcceleration = getOvrNodeAcceleration(XRNode.Head, OVRPlugin.Node.Head);
        _motionData.headAngularVelocity = getOvrNodeAngularVelocity(XRNode.Head, OVRPlugin.Node.Head);
        _motionData.rightHandPosition = getOvrNodePosition(XRNode.RightHand, OVRPlugin.Node.HandRight);
        _motionData.rightHandOrientation = getOvrNodeOrientation(XRNode.RightHand, OVRPlugin.Node.HandRight);
        _motionData.rightHandAcceleration = getOvrNodeAcceleration(XRNode.RightHand, OVRPlugin.Node.HandRight);
        _motionData.rightHandAngularVelocity = getOvrNodeAngularVelocity(XRNode.RightHand, OVRPlugin.Node.HandRight);

        _motionData.CopyTo(ref _msgMotionData);
        _zmqPushMotionData.TrySend(ref _msgMotionData, TimeSpan.Zero, false);
    }

    void OnDestroy() {
        if (_bypassPrediction) { return; }

        _zmqPushMotionData.Close();
        _msgMotionData.Close();
        
        _zmqPushMotionData.Dispose();

        if (shouldReportProfile) {
            _zmqPushProfile.Close();
            _zmqPushProfile.Dispose();
        }
        
        NetMQ.NetMQConfig.Cleanup(false);
    }

    private Vector3 getOvrNodePosition(XRNode nodeType, OVRPlugin.Node ovrNodeType) {
        var result = Vector3.zero;
        if (OVRNodeStateProperties.GetNodeStatePropertyVector3(nodeType, NodeStatePropertyType.Position, ovrNodeType, OVRPlugin.Step.Render, out result)) {
            return result;
        }
        else {
            return Vector3.zero;
        }
    }

    private Quaternion getOvrNodeOrientation(XRNode nodeType, OVRPlugin.Node ovrNodeType) {
        var result = Quaternion.identity;
        if (OVRNodeStateProperties.GetNodeStatePropertyQuaternion(nodeType, NodeStatePropertyType.Orientation, ovrNodeType, OVRPlugin.Step.Render, out result)) {
            return result;
        }
        else {
            return Quaternion.identity;
        }
    }

    private Vector3 getOvrNodeAcceleration(XRNode nodeType, OVRPlugin.Node ovrNodeType) {
        var result = Vector3.zero;
        if (OVRNodeStateProperties.GetNodeStatePropertyVector3(nodeType, NodeStatePropertyType.Acceleration, ovrNodeType, OVRPlugin.Step.Render, out result)) {
            return result;
        }
        else {
            return Vector3.zero;
        }
    }

    private Vector3 getOvrNodeAngularVelocity(XRNode nodeType, OVRPlugin.Node ovrNodeType) {
        var result = Vector3.zero;
        if (OVRNodeStateProperties.GetNodeStatePropertyVector3(nodeType, NodeStatePropertyType.AngularVelocity, ovrNodeType, OVRPlugin.Step.Render, out result)) {
            return result;
        }
        else {
            return Vector3.zero;
        }
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

    private class MotionData {
        private byte[] _data;
        private byte[] _temp;

        public MotionData() {
            _data = new byte[(int)Offset.Max];
            _temp = new byte[4 * 4];
        }

        public long timestamp {
            get { return getLong((int)Offset.Timestamp); }
            set { setLong((int)Offset.Timestamp, value); }
        }

        public Vector3 leftEyePosition {
            get { return getVector((int)Offset.LeftEyePosition); }
            set { setVector((int)Offset.LeftEyePosition, value); }
        }

        public Vector3 rightEyePosition {
            get { return getVector((int)Offset.RightEyePosition); }
            set { setVector((int)Offset.RightEyePosition, value); }
        }

        public Quaternion headOrientation {
            get { return getQuaternion((int)Offset.HeadOrientation); }
            set { setQuaternion((int)Offset.HeadOrientation, value); }
        }

        public Vector3 headAcceleration {
            get { return getVector((int)Offset.HeadAcceleration); }
            set { setVector((int)Offset.HeadAcceleration, value); }
        }

        public Vector3 headAngularVelocity {
            get { return getVector((int)Offset.HeadAngularVelocity); }
            set { setVector((int)Offset.HeadAngularVelocity, value); }
        }

        public float[] cameraProjection {
            get { return getProjection((int)Offset.CameraProjection); }
            set { setProjection((int)Offset.CameraProjection, value); }
        }

        public Vector3 rightHandPosition {
            get { return getVector((int)Offset.RightHandPosition); }
            set { setVector((int)Offset.RightHandPosition, value); }
        }

        public Quaternion rightHandOrientation {
            get { return getQuaternion((int)Offset.RightHandOrientation); }
            set { setQuaternion((int)Offset.RightHandOrientation, value); }
        }

        public Vector3 rightHandAcceleration {
            get { return getVector((int)Offset.RightHandAcceleration); }
            set { setVector((int)Offset.RightHandAcceleration, value); }
        }

        public Vector3 rightHandAngularVelocity {
            get { return getVector((int)Offset.RightHandAngularVelocity); }
            set { setVector((int)Offset.RightHandAngularVelocity, value); }
        }

        public void CopyTo(ref NetMQ.Msg message) {
            message.InitPool(_data.Length);

            Buffer.BlockCopy(_data, 0, message.Data, 0, _data.Length);
        }

        private long getLong(int offset) {
            Buffer.BlockCopy(_data, offset, _temp, 0, 8);
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(_temp, 0, 8);
            }
            return BitConverter.ToInt64(_temp, 0);
        }

        private void setLong(int offset, long value) {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }
            Buffer.BlockCopy(bytes, 0, _data, offset, bytes.Length);
        }

        private Vector3 getVector(int offset) {
            Buffer.BlockCopy(_data, offset, _temp, 0, 4 * 3);

            var result = Vector3.zero;
            for (int i = 0; i < 3; i++) {
                if (BitConverter.IsLittleEndian) {
                    Array.Reverse(_temp, i * 4, 4);
                }
                // convert OpenGL to Unity
                result[i] = (i == 2 ? -1.0f : 1.0f) * BitConverter.ToSingle(_temp, i * 4);
            }
            return result;
        }

        private void setVector(int offset, Vector3 value) {
            for (int i = 0; i < 3; i++) {
                // convert Unity to OpenGL
                var bytes = BitConverter.GetBytes((i == 2 ? -1.0f : 1.0f) * value[i]);
                if (BitConverter.IsLittleEndian) {
                    Array.Reverse(bytes);
                }
                Buffer.BlockCopy(bytes, 0, _data, offset + i * 4, bytes.Length);
            }
        }

        private Quaternion getQuaternion(int offset) {
            Buffer.BlockCopy(_data, offset, _temp, 0, 4 * 4);

            var result = Quaternion.identity;
            for (int i = 0; i < 4; i++) {
                if (BitConverter.IsLittleEndian) {
                    Array.Reverse(_temp, i * 4, 4);
                }
                // convert OpenGL to Unity
                result[i] = (i == 0 || i == 1 ? -1.0f : 1.0f) * BitConverter.ToSingle(_temp, i * 4);
            }
            return result;
        }

        private void setQuaternion(int offset, Quaternion value) {
            for (int i = 0; i < 4; i++) {
                // convert Unity to OpenGL
                var bytes = BitConverter.GetBytes((i == 0 || i == 1 ? -1.0f : 1.0f) * value[i]);
                if (BitConverter.IsLittleEndian) {
                    Array.Reverse(bytes);
                }
                Buffer.BlockCopy(bytes, 0, _data, offset + i * 4, bytes.Length);
            }
        }

        private float[] getProjection(int offset) {
            Buffer.BlockCopy(_data, offset, _temp, 0, 4 * 4);

            var result = new float[4];
            for (int i = 0; i < 4; i++) {
                if (BitConverter.IsLittleEndian) {
                    Array.Reverse(_temp, i * 4, 4);
                }
                result[i] = BitConverter.ToSingle(_temp, i * 4);
            }
            return result;
        }

        private void setProjection(int offset, float[] value) {
            for (int i = 0; i < 4; i++) {
                var bytes = BitConverter.GetBytes(value[i]);
                if (BitConverter.IsLittleEndian) {
                    Array.Reverse(bytes);
                }
                Buffer.BlockCopy(bytes, 0, _data, offset + i * 4, bytes.Length);
            }
        }

        private enum Offset : int {
            Timestamp = 0,
            LeftEyePosition = Timestamp + 8,
            RightEyePosition = LeftEyePosition + 4 * 3,
            HeadOrientation = RightEyePosition + 4 * 3,
            HeadAcceleration = HeadOrientation + 4 * 4,
            HeadAngularVelocity = HeadAcceleration + 4 * 3,
            CameraProjection = HeadAngularVelocity + 4 * 3,
            RightHandPosition = CameraProjection + 4 * 4,
            RightHandOrientation = RightHandPosition + 4 * 3,
            RightHandAcceleration = RightHandOrientation + 4 * 4,
            RightHandAngularVelocity = RightHandAcceleration + 4 * 3,

            Max = RightHandAngularVelocity + 4 * 3
        }
    }
}
