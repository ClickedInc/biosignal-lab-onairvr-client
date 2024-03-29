﻿/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the MIT license found in the LICENSE file 
  in the root folder of the project.

 ***********************************************************/

using System.IO;
using UnityEngine;
using UnityEngine.XR;

public class AirVRClientAppManager : Singleton<AirVRClientAppManager>, AirVRClient.EventHandler {
    public static string ConfigFile => Path.Combine(Application.persistentDataPath, "config.json");

    [SerializeField][Range(0.5f, 2.0f)] private float _renderScale = 1f;

    private AirVRCamera _camera;
    private GameObject _room;
    private Light _envLight;
    private AirVRRealWorldSpaceSetup _realWorldSpaceSetup;
    private AXRClientConfig _devConfig;

    private bool _lastUserPresent = false;

    public bool IsConnecting { get; private set; }
    public AirVRClientNotification Notification { get; private set; }
    public AirVRClientAppConfig Config { get; private set; }
    public AirVRClientInputModule InputModule { get; private set; }

    private void Awake() {
        _camera = FindObjectOfType<AirVRCamera>();
        _room = transform.Find("Room").gameObject;
        _envLight = transform.Find("EnvLight").GetComponent<Light>();

        Notification = FindObjectOfType<AirVRClientNotification>();
        Config = new AirVRClientAppConfig();
        InputModule = FindObjectOfType<AirVRClientInputModule>();

        AirVRClient.Delegate = this;
    }

    private void Start() {
        XRSettings.eyeTextureResolutionScale = _renderScale;

        if (Config.FirstPlay) {            
            AirVRClientUIManager.Instance.GuidePanel.StartGuide();
        }
    }

    public void Connect(string addressText, string portText, string userIDText) {   
        string message;

        if (!AirVRClientAppConfig.ValidateIPv4(addressText)) {
            message = "Please enter the correct ip address.";

            Notification.DisplayError(message);
            Debug.Log(message);
            return;
        }

        if (!AirVRClientAppConfig.ValidatePort(portText)) {
            message = "Please enter the correct port.";

            Notification.DisplayError(message);
            Debug.Log(message);
            return;
        }

        if (!AirVRClientAppConfig.ValidateUserID(userIDText)) {
            message = "Please enter the correct User ID.";

            Notification.DisplayError(message);
            Debug.Log(message);
            return;
        }

        string address = addressText;
        int port = int.Parse(portText);
        int userID = int.Parse(userIDText);

        Config.Save(address, port, userID, AirVRClientUIManager.Instance.SettingPanel.AutoPlay.Toggle.isOn, Config.FirstPlay);
        AirVRClientUIManager.Instance.CanvasGroup.blocksRaycasts = false;        
        AirVRClientUIManager.Instance.CanvasGroup.interactable = false;

        AirVRClientUIManager.Instance.SettingPanel.PlayButton.enabled = false;

        IsConnecting = true;
        Notification.DisplayConnecting();

        readDevConfig();

        _camera.profile.userID = userID.ToString();
        _camera.profile.videoMinBitrate = _devConfig.videoBitrate.min;
        _camera.profile.videoStartBitrate = _devConfig.videoBitrate.start;
        _camera.profile.videoMaxBitrate = _devConfig.videoBitrate.max;
        _camera.profile.videoFrameRate = _devConfig.videoFramerate;
        _camera.profile.videoResolution = (_devConfig.videoResolution.width, _devConfig.videoResolution.height);

        _camera.profile.profiler = new AirVRProfileBase.ProfilerSettings {
            Flags = _devConfig.profiler.flags,
            Filename = _devConfig.profiler.filename
        };

#if UNITY_ANDROID && !UNITY_EDITOR
        AirVRClient.Connect(address, port);
#endif
    }

    private void OnDisconnected() {
        _envLight.enabled = true;

        if (!_room.activeSelf)
            _room.SetActive(true);

        AirVRClientUIManager.Instance.Show();
        AirVRClientUIManager.Instance.SettingPanel.PlayButton.enabled = true;
        AirVRClientUIManager.Instance.CanvasGroup.blocksRaycasts = true;
        AirVRClientUIManager.Instance.CanvasGroup.interactable = true;

        if (IsConnecting)
        {
            string message = "Connection failed.";
            Notification.DisplayError(message);
            Debug.LogError(message);
        }

        IsConnecting = false;
    }

    private void readDevConfig() {
        if (Application.isEditor == false && File.Exists(ConfigFile)) {
            _devConfig = JsonUtility.FromJson<AXRClientConfig>(File.ReadAllText(ConfigFile));
        }
        else {
            _devConfig = new AXRClientConfig();
        }
    }

    // implements AirVRClient.EventHandler
    public void AirVRClientFailed(string reason) { }

    public void AirVRClientConnected() {
        _room.SetActive(false);
        IsConnecting = false;
        AirVRClientUIManager.Instance.Hide();
        AirVRClient.Play();

        _envLight.enabled = false;
    }

    public void AirVRClientPlaybackStarted() {}
    
    public void AirVRClientPlaybackStopped() { }

    public void AirVRClientDisconnected() {
        OnDisconnected();
    }

    public void AirVRClientUserDataReceived(byte[] userData) {
        // pong with received data to the server
        var data = string.Format("pong from {0} by {1}", System.Environment.MachineName, System.Text.Encoding.UTF8.GetString(userData));

        AirVRClient.SendUserData(System.Text.Encoding.UTF8.GetBytes(data));
    }
}
