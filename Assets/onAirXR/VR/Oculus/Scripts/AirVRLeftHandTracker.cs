/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;

public class AirVRLeftHandTracker : AirVRTrackerFeedback {
    // implements AirVRPointerBase
    protected override AXRInputDeviceID srcDevice => AXRInputDeviceID.LeftHandTracker;
    protected override OVRInput.Controller ovrController => OVRInput.Controller.LTouch;

    protected override Vector3 worldOriginPosition {
        get {
            return trackingOriginLocalToWorldMatrix.MultiplyPoint(
                OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch)
            );
        }
    }

    protected override Quaternion worldOriginOrientation {
        get {
            return cameraRoot.rotation * OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);
        }
    }
}
