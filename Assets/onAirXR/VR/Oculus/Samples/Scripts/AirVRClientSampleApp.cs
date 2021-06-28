/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using System;
using System.IO;
using UnityEngine;

public class AirVRClientSampleApp : MonoBehaviour, AirVRClient.EventHandler {
    private AirVRCamera _camera;
    private TextMesh _textMessage;

    [SerializeField] private string _address = "127.0.0.1";
    [SerializeField] private int _port = 9090;

    private void Awake() {
        AirVRClient.Delegate = this;

        _camera = FindObjectOfType<AirVRCamera>();
        _textMessage = transform.Find("Message").GetComponent<TextMesh>();
    }

    private void Update() {
        if (OVRInput.GetDown(OVRInput.Button.One)) {
            connect();
		}
	}

    private void OnApplicationPause(bool pauseStatus) {
        if (Application.isEditor) { return; }

		if (pauseStatus && AirVRClient.playing) {
            AirVRClient.Stop();
		}
		else if (pauseStatus == false && AirVRClient.connected) {
            AirVRClient.Play();
		}
    }

    private void connect() {
        if (AirVRClient.connected) { return; }

        // profile settings
        _camera.profile.userID = "sample";
        _camera.profile.profiler = new AirVRProfileBase.ProfilerSettings {
            Flags = 0x03, // 0x01: frame by frame, written into the below file
                          // 0x02: periodic summary report in logcat
            Filename = Path.Combine(Application.persistentDataPath, string.Format("profile_{0}.frames", DateTime.Now.ToString("yyyyMMddHHmmss")))
        };

        AirVRClient.Connect(_address, _port);
    }

    // implements AirVRClient.EventHandler
    public void AirVRClientFailed (string reason) {
        Debug.Log("[ERROR] AirVRClient failed to initialize : " + reason);
    }

    public void AirVRClientConnected() {
        _textMessage.gameObject.SetActive(false);

        AirVRClient.Play();
    }

    public void AirVRClientPlaybackStarted() {}
    public void AirVRClientPlaybackStopped() {}

    public void AirVRClientDisconnected() {
        _textMessage.gameObject.SetActive(true);
    }

    public void AirVRClientUserDataReceived(byte[] userData) {}
}
