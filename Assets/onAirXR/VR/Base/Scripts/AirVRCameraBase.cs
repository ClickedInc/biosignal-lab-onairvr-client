/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public abstract class AirVRCameraBase : MonoBehaviour {
    private Transform _thisTransform;
    private Camera _camera;
    private VideoRenderer _videoRenderer;
    private bool _aboutToDestroy;
    private Vector2 _savedCameraClipPlanes;

    [SerializeField] private AudioMixerGroup _audioMixerGroup = null;
    [SerializeField] private AirVRProfileBase.VideoBitrate _videoBitrate = AirVRProfileBase.VideoBitrate.Normal;
    [SerializeField] private ControllerOverlay _controllerOverlay = ControllerOverlay.Default;
    [SerializeField] private GameObject _leftControllerModel = null;
    [SerializeField] private GameObject _rightControllerModel = null;
    [SerializeField] private bool _enablePointer = true;
    [SerializeField] private Color _colorLaser = Color.white;
    [SerializeField] private Texture2D _pointerCookie = null;
    [SerializeField] private float _pointerCookieDepthScaleMultiplier = 0.015f;

    protected AirVRProfileBase.VideoBitrate videoBitrate => _videoBitrate;

    public HeadTrackerInputDevice headTracker { get; private set; }

    protected GameObject leftControllerModel {
        get {
            return _controllerOverlay == ControllerOverlay.Custom ? _leftControllerModel : Resources.Load<GameObject>("LeftControllerModel");
        }
    }
    protected GameObject rightControllerModel {
        get {
            return _controllerOverlay == ControllerOverlay.Custom ? _rightControllerModel : Resources.Load<GameObject>("RightControllerModel");
        }
    }

    protected AirVRTrackerFeedbackBase.PointerDesc pointerDesc {
        get {
            return new AirVRTrackerFeedbackBase.PointerDesc {
                enabled = _controllerOverlay == ControllerOverlay.Custom ? _enablePointer : true,
                colorLaser = _controllerOverlay == ControllerOverlay.Custom ? _colorLaser : Color.white,
                cookie = _controllerOverlay == ControllerOverlay.Custom ? _pointerCookie : Resources.Load<Texture2D>("PointerCookie"),
                cookieDepthScaleMultiplier = _controllerOverlay == ControllerOverlay.Custom ? _pointerCookieDepthScaleMultiplier : 0.015f
            };
        }
    }

    protected abstract void RecenterPose();

    public abstract AirVRProfileBase profile { get; }
    public abstract Matrix4x4 trackingSpaceToWorldMatrix { get; }

    protected virtual void Awake() {
        _thisTransform = transform;
        _camera = gameObject.GetComponent<Camera>();

        createVideoRenderer();
        createAudioRenderer();

        headTracker = new HeadTrackerInputDevice(_thisTransform);
    }

    protected virtual void Start() {
        runLoopOnEndOfFrame();

        MotionDataProvider.LoadOnce(profile);

        AirVRClient.LoadOnce(profile, this);
        AirVRInputManager.LoadOnce();

		AirVRClient.MessageReceived += onAirVRMesageReceived;

        AirVRInputManager.RegisterInputSender(headTracker);

        saveCameraClipPlanes(); // workaround for the very first disconnected event
    }

    private void OnPreRender() {
        _videoRenderer?.OnPreRender(_camera, headTracker, profile);
    }

    private void OnPostRender() {
        _videoRenderer?.OnPostRender(_camera, headTracker, profile);
    }

    private void OnDestroy() {
        _aboutToDestroy = true;

		AirVRClient.MessageReceived -= onAirVRMesageReceived;
    }

    private async void runLoopOnEndOfFrame() {
        while (_aboutToDestroy == false) {
            await Task.Yield();

            _videoRenderer?.OnEndOfFrame(_camera, headTracker, profile);
        }
    }

    private void createVideoRenderer() {
#if UNITY_EDITOR || UNITY_STANDALONE
        _videoRenderer = new WindowsVideoRenderer();
#elif UNITY_ANDROID
        _videoRenderer = new AndroidVideoRenderer();
#else
        _videoRenderer = null;
#endif
    }

    private void createAudioRenderer() {
        var go = new GameObject("AirVRAudioRenderer");
        go.transform.parent = transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        AudioSource audioSource = go.AddComponent<AudioSource>();
        audioSource.loop = false;

        if (_audioMixerGroup != null) {
            audioSource.outputAudioMixerGroup = _audioMixerGroup;

            // workaround for applying audio mixer group change
            audioSource.Stop();
            audioSource.Play();
        }

        go.AddComponent<AirVRClientAudioSource>();
    }

    private void onAirVRMesageReceived(AirVRClientMessage message) {
        if (message.IsSessionEvent()) {
            if (message.Name.Equals(AirVRClientMessage.NameConnected)) {
                saveCameraClipPlanes();
                
                AXRClientPlugin.EnableNetworkTimeWarp(true);
            }
            else if (message.Name.Equals(AirVRClientMessage.NameDisconnected)) {
                restoreCameraClipPlanes();
            }
        }
        else if (message.IsMediaStreamEvent()) {
            if (message.Name.Equals(AirVRClientMessage.NameCameraClipPlanes)) {
                setCameraClipPlanes(message.NearClip, message.FarClip);
            }
            else if (message.Name.Equals(AirVRClientMessage.NameEnableNetworkTimeWarp)) {
                AXRClientPlugin.EnableNetworkTimeWarp(message.Enable);
            }
        }
        else if (message.IsInputStreamEvent() && message.Name.Equals(AirVRClientMessage.NameRecenterPose)) {
            RecenterPose();
        }
	}

    private void saveCameraClipPlanes() {
        _savedCameraClipPlanes.x = _camera.nearClipPlane;
        _savedCameraClipPlanes.y = _camera.farClipPlane;
    }

    private void restoreCameraClipPlanes() {
        _camera.nearClipPlane = _savedCameraClipPlanes.x;
        _camera.farClipPlane = _savedCameraClipPlanes.y;
    }

    private void setCameraClipPlanes(float nearClip, float farClip) {
        _camera.nearClipPlane = Mathf.Min(nearClip, _camera.nearClipPlane);
        _camera.farClipPlane = Mathf.Max(farClip, _camera.farClipPlane);
    }

    private enum ControllerOverlay {
        Default,
        Custom
    }

    private AXRRenderCommand createRenderCommand(AirVRProfileBase profile, Camera camera) {
        return profile.useSeperateVideoRenderTarget ? new AXRImmediateRenderCommand() as AXRRenderCommand :
                                                      new AXRCameraEventRenderCommand(camera, CameraEvent.BeforeForwardOpaque) as AXRRenderCommand;
    }

    public class HeadTrackerInputDevice : AirVRTrackerInputDevice {
        private Transform _head;

        public Pose currentPose {
            get {
                if (realWorldSpace != null) {
                    var worldToRealWorldMatrix = realWorldSpace.realWorldToWorldMatrix.inverse;

                    return new Pose(
                        worldToRealWorldMatrix.MultiplyPoint(_head.position),
                        worldToRealWorldMatrix.rotation * _head.rotation
                    );
                }
                else {
                    return new Pose(_head.localPosition, _head.localRotation);
                }
            }
        }

        public HeadTrackerInputDevice(Transform head) {
            _head = head;
        }

        // implements AirVRTrackerInputDevice
        public override byte id => (byte)AXRInputDeviceID.HeadTracker;

        public override void PendInputsPerFrame(AXRInputStream inputStream) {
            var pose = currentPose;

            inputStream.PendPose(this, (byte)AXRHeadTrackerControl.Pose, pose.position, pose.rotation);
            inputStream.PendByteAxis(this, (byte)AXRHeadTrackerControl.Battery, (byte)Mathf.Round(SystemInfo.batteryLevel * 100));
        }
    }

    private abstract class VideoRenderer {
        private AXRRenderCommand _renderCommand;
        private bool _renderedOnce;

        public void OnPreRender(Camera camera, HeadTrackerInputDevice headTracker, AirVRProfileBase profile) {
            ensureRenderCommandCreated(camera, profile);
            if ((_renderCommand is AXRCameraEventRenderCommand) == false) { return; }

            renderFrame(camera, headTracker, profile);
        }

        public void OnPostRender(Camera camera, HeadTrackerInputDevice headTracker, AirVRProfileBase profile) {
            if ((_renderCommand is AXRImmediateRenderCommand) == false) {
                _renderCommand.Clear();
                return;
            }

            renderFrame(camera, headTracker, profile);
        }

        public void OnEndOfFrame(Camera camera, HeadTrackerInputDevice headTracker, AirVRProfileBase profile) {
            if (_renderedOnce == false) {
                ensureRenderCommandCreated(camera, profile);
                renderFrame(camera, headTracker, profile);
            }
            else {
                _renderedOnce = false;
            }

            OnFinishRenderFrame();
        }

        public abstract void OnRenderFrame(AXRClientPlugin.FrameType type, HeadTrackerInputDevice headTracker, AirVRProfileBase profile, AXRRenderCommand renderCommand);
        public abstract void OnFinishRenderFrame();

        private void ensureRenderCommandCreated(Camera camera, AirVRProfileBase profile) {
            if (_renderCommand != null) { return; }

            _renderCommand = profile.useSeperateVideoRenderTarget ? new AXRImmediateRenderCommand() as AXRRenderCommand :
                                                                    new AXRCameraEventRenderCommand(camera, CameraEvent.BeforeForwardOpaque) as AXRRenderCommand;
        }

        private void renderFrame(Camera camera, HeadTrackerInputDevice headTracker, AirVRProfileBase profile) {
            if (AirVRClient.playing) {
                OnRenderFrame(frameType(camera, _renderedOnce), headTracker, profile, _renderCommand);
            }
            _renderedOnce = true;
        }

        private AXRClientPlugin.FrameType frameType(Camera camera, bool renderedOnce) {
            if (camera == null) { return AXRClientPlugin.FrameType.Mono; }

            return camera.stereoEnabled && camera.stereoTargetEye == StereoTargetEyeMask.Both ?
                       (renderedOnce ? AXRClientPlugin.FrameType.StereoRight : AXRClientPlugin.FrameType.StereoLeft) :
                       AXRClientPlugin.FrameType.Mono;
        }
    }

    private class AndroidVideoRenderer : VideoRenderer {
        private int _viewNumber;

        public override void OnRenderFrame(AXRClientPlugin.FrameType type, HeadTrackerInputDevice headTracker, AirVRProfileBase profile, AXRRenderCommand renderCommand) {
            switch (type) {
                case AXRClientPlugin.FrameType.StereoLeft:
                    AXRClientPlugin.SetCameraOrientation(headTracker.currentPose.rotation, ref _viewNumber);
                    AXRClientPlugin.PreRenderVideoFrame(_viewNumber);

                    AXRClientPlugin.RenderVideoFrame(renderCommand, type);
                    break;
                case AXRClientPlugin.FrameType.StereoRight:
                    AXRClientPlugin.RenderVideoFrame(renderCommand, type, profile.useSingleTextureForEyes == false);
                    break;
                case AXRClientPlugin.FrameType.Mono:
                    AXRClientPlugin.SetCameraOrientation(headTracker.currentPose.rotation, ref _viewNumber);
                    AXRClientPlugin.PreRenderVideoFrame(_viewNumber);
                    AXRClientPlugin.RenderVideoFrame(renderCommand, type);
                    break;
            }
        }

        public override void OnFinishRenderFrame() {
            AXRClientPlugin.EndRenderVideoFrame();
        }
    }

    private class WindowsVideoRenderer : VideoRenderer {
        public override void OnRenderFrame(AXRClientPlugin.FrameType type, HeadTrackerInputDevice headTracker, AirVRProfileBase profile, AXRRenderCommand renderCommand) {
            // Currently, Windows supports monoscopic video only
            AXRClientPlugin.RenderVideoFrame(renderCommand, AXRClientPlugin.FrameType.Mono);
        }

        public override void OnFinishRenderFrame() {
            // do nothing
        }
    }
}
