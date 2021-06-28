/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;

public class AirVRControllerInputDevice : AXRInputSender {
    // implements AXRInputSender
    public override byte id => (byte)AXRInputDeviceID.Controller;

    public override void PendInputsPerFrame(AXRInputStream inputStream) {
        const OVRInput.Controller ltouch = OVRInput.Controller.LTouch;
        const OVRInput.Controller rtouch = OVRInput.Controller.RTouch;

        inputStream.PendAxis2D(this, (byte)AXRControllerControl.Axis2DLThumbstick, OVRInput.Get(OVRInput.RawAxis2D.LThumbstick, ltouch));
        inputStream.PendAxis2D(this, (byte)AXRControllerControl.Axis2DRThumbstick, OVRInput.Get(OVRInput.RawAxis2D.RThumbstick, rtouch));
        inputStream.PendAxis(this, (byte)AXRControllerControl.AxisLIndexTrigger, OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger, ltouch));
        inputStream.PendAxis(this, (byte)AXRControllerControl.AxisRIndexTrigger, OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger, rtouch));
        inputStream.PendAxis(this, (byte)AXRControllerControl.AxisLHandTrigger, OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger, ltouch));
        inputStream.PendAxis(this, (byte)AXRControllerControl.AxisRHandTrigger, OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger, rtouch));
        inputStream.PendByteAxis(this, (byte)AXRControllerControl.ButtonA, getByteAxis(OVRInput.Get(OVRInput.RawButton.A, rtouch)));
        inputStream.PendByteAxis(this, (byte)AXRControllerControl.ButtonB, getByteAxis(OVRInput.Get(OVRInput.RawButton.B, rtouch)));
        inputStream.PendByteAxis(this, (byte)AXRControllerControl.ButtonX, getByteAxis(OVRInput.Get(OVRInput.RawButton.X, ltouch)));
        inputStream.PendByteAxis(this, (byte)AXRControllerControl.ButtonY, getByteAxis(OVRInput.Get(OVRInput.RawButton.Y, ltouch)));
        inputStream.PendByteAxis(this, (byte)AXRControllerControl.ButtonStart, getByteAxis(OVRInput.Get(OVRInput.RawButton.Start, ltouch)));
        inputStream.PendByteAxis(this, (byte)AXRControllerControl.ButtonLThumbstick, getByteAxis(OVRInput.Get(OVRInput.RawButton.LThumbstick, ltouch)));
        inputStream.PendByteAxis(this, (byte)AXRControllerControl.ButtonRThumbstick, getByteAxis(OVRInput.Get(OVRInput.RawButton.RThumbstick, rtouch)));
    }

    private byte getByteAxis(bool button) {
        return button ? (byte)1 : (byte)0;
    }

    private Vector2 combineAxes(params Vector2[] axes) {
        Vector2 sum = Vector2.zero;
        foreach (var axis in axes) {
            sum += axis;
        }
        return new Vector2(
            Mathf.Clamp(sum.x, -1.0f, 1.0f),
            Mathf.Clamp(sum.y, -1.0f, 1.0f)
        );
    }

    private float combineAxes(params float[] axes) {
        var sum = 0.0f;
        foreach (var axis in axes) {
            sum += axis;
        }
        return Mathf.Clamp(sum, 0.0f, 1.0f);
    }
}
