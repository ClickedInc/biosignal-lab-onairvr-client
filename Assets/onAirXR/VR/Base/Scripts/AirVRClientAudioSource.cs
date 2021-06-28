/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;

[RequireComponent(typeof(AudioSource))]

public class AirVRClientAudioSource : MonoBehaviour {
    void OnAudioFilterRead(float[] data, int channels) {
        if (AirVRClient.configured == false) { return; }

        if (AXRClientPlugin.GetAudioData(data, data.Length / channels, channels) == false) {
            System.Array.Clear(data, 0, data.Length);
        }
    }
}
