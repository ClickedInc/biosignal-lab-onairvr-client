/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;
using UnityEngine.Assertions;
using System;

public class AirVRClient : MonoBehaviour, AirVRClientStateMachine.Context {
	public interface EventHandler {
        void AirVRClientFailed(string reason);
		void AirVRClientConnected();
		void AirVRClientPlaybackStarted();
		void AirVRClientPlaybackStopped();
		void AirVRClientDisconnected();
        void AirVRClientUserDataReceived(byte[] userData);
	}

	private static AirVRClient _instance;
    private static AirVRClientEventDispatcher _eventDispatcher;
    private static bool _checkCopyright;

	public delegate void OnAirVRMessageReceiveHandler(AirVRClientMessage messsage);
	public static event OnAirVRMessageReceiveHandler MessageReceived;

	public static EventHandler Delegate { private get; set; }
    public static bool automaticallyPauseWhenUserNotPresent { get; set; } = true;

    public static bool configured => _instance?._configured ?? false;
    public static bool connected => AXRClientPlugin.IsConnected();
    public static bool playing => AXRClientPlugin.IsPlaying();

    public static void LoadOnce(AirVRProfileBase profile, AirVRCameraBase camera) {
        if (_instance == null) {
            GameObject go = new GameObject("AirVRClient");
            go.AddComponent<AirVRClient>();

            Assert.IsTrue(_instance != null);
            _instance._profile = profile;
            if (profile.useSeperateVideoRenderTarget) {
                _instance._videoFrameRenderer = new AirVRVideoFrameRenderer(go, profile, camera);
            }

            AXRClientPlugin.SetProfile(JsonUtility.ToJson(profile.GetSerializable()));
        }
    }

    public static void Connect(string address, int port) {
		if (_instance != null) {
            AXRClientPlugin.SetProfile(JsonUtility.ToJson(_instance._profile.GetSerializable()));

            AXRClientPlugin.RequestConnect(address, port);
        }
    }

	public static void Play() {
		if (_instance != null) {
			_instance._stateMachine.TriggerPlayRequested();
		}
	}

	public static void Stop() {
		if (_instance != null) {
			_instance._stateMachine.TriggerStopRequested();
		}
	}

	public static void Disconnect() {
        AXRClientPlugin.RequestDisconnect();
	}

	public static void SendUserData(byte[] data) {
		if (_eventDispatcher != null) {
			_eventDispatcher.SendUserData(data);
		}
	}

	private AirVRProfileBase _profile;
    private AirVRVideoFrameRenderer _videoFrameRenderer;
	private AirVRClientStateMachine _stateMachine;
    private bool _configured;
        
    private void Awake() {
		if (_instance != null) {
			throw new UnityException("[ERROR] There must exist only one instance of AirVRClient.");
		}
		_instance = this;
		DontDestroyOnLoad(gameObject);

        if (_eventDispatcher == null) {
            _eventDispatcher = new AirVRClientEventDispatcher();
            _eventDispatcher.MessageReceived += onAirXRMessageReceived;
        }
    }

    private void Start() {
        if (Delegate == null) {
            throw new UnityException("[ERROR] Must set AirVRClient.Delegate.");
        }
        int result = AXRClientPlugin.Configure(AudioSettings.outputSampleRate, _profile.hasInput);
        if (result < 0 && result != -4) {
            Delegate.AirVRClientFailed("failed to init AirVRClient : " + result);
        }
        else {
            _configured = true;
        }
	    
	    _stateMachine = new AirVRClientStateMachine(this, _profile.delayToResumePlayback);
	}

    private void Update() {
        _eventDispatcher.DispatchEvent();
	    _stateMachine?.Update(!automaticallyPauseWhenUserNotPresent || _profile.isUserPresent, Time.deltaTime);
    }

    private void LateUpdate() {
        if (_videoFrameRenderer != null) {
            _videoFrameRenderer.Update();
        }
    }

	private void OnApplicationPause(bool pauseStatus) {
		_stateMachine?.UpdatePauseStatus(pauseStatus);
	}

	private void OnDestroy() {
        if (_eventDispatcher != null) {
            _eventDispatcher.MessageReceived -= onAirXRMessageReceived;
        }
        AXRClientPlugin.Cleanup();
	}

	// handle AirXRMessages
	private void onAirXRMessageReceived(AXRMessage message) {
        AirVRClientMessage clientMessage = message as AirVRClientMessage;
        Assert.IsNotNull(clientMessage);

        MessageReceived?.Invoke(clientMessage);

        if (clientMessage.IsSessionEvent()) {
            if (clientMessage.Name.Equals(AirVRClientMessage.NameSetupResponded)) {
                onAirVRSetupResponded(clientMessage);
            }
            else if (clientMessage.Name.Equals(AirVRClientMessage.NameRenderPrepared)) {
                onAirVRRenderPrepared(clientMessage);
            }
            else if (clientMessage.Name.Equals(AirVRClientMessage.NamePlayResponded)) {
                onAirVRPlayResponded(clientMessage);
            }
            else if (clientMessage.Name.Equals(AirVRClientMessage.NameStopResponded)) {
                onAirVRStopResponded(clientMessage);
            }
            else if (clientMessage.Name.Equals(AirVRClientMessage.NameDisconnected)) {
                onAirVRDisconnected(clientMessage);
            }
        }
		else if (message.Type.Equals(AXRMessage.TypeUserData)) {
			onAirVRUserDataReceived(message);
		}
	}
	
    private void onAirVRSetupResponded(AirVRClientMessage message) {
        AXRClientPlugin.PrepareRender();
	}

    private void onAirVRRenderPrepared(AirVRClientMessage message) {
        if (_videoFrameRenderer != null) {
            var texture = IntPtr.Zero;
            int width = 0, height = 0;

            if (AXRClientPlugin.GetVideoRenderTargetTexture(ref texture, ref width, ref height)) {
                _videoFrameRenderer.SetVideoFrameTexture(Texture2D.CreateExternalTexture(width, height, TextureFormat.RGBA32, false, false, texture));
                _videoFrameRenderer.enabled = true;
            }
        }
	    
	    _stateMachine.TriggerConnected();
	    Delegate.AirVRClientConnected();
	}

    private void onAirVRPlayResponded(AirVRClientMessage message) {
        Delegate.AirVRClientPlaybackStarted();
	}

    private void onAirVRStopResponded(AirVRClientMessage message) {
        Delegate.AirVRClientPlaybackStopped();
	}
	
    private void onAirVRDisconnected(AirVRClientMessage message) {
        if (_videoFrameRenderer != null) {
            _videoFrameRenderer.SetVideoFrameTexture(null);
            _videoFrameRenderer.enabled = false;
        }
	    
	    _stateMachine.TriggerDisconnected();
	    Delegate.AirVRClientDisconnected();
	}

    private void onAirVRUserDataReceived(AXRMessage message) {
		Delegate.AirVRClientUserDataReceived(message.Data_Decoded);
    }
	
	// implements AirVRClientStateMachine.Context
	void AirVRClientStateMachine.Context.RequestPlay() {
        AXRClientPlugin.RequestPlay();
	}

	void AirVRClientStateMachine.Context.RequestStop() {
        AXRClientPlugin.RequestStop();
	}
}
