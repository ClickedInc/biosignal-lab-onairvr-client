/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

public enum AXRInputDeviceID : byte {
    HeadTracker = 0,
    LeftHandTracker = 1,
    RightHandTracker = 2,
    Controller = 3,
    TouchScreen = 4
}

public enum AXRInputDirection : byte {
    Up = 0,
    Down,
    Left,
    Right
}

public enum AXRDeviceStatus : byte {
    Unavailable = 0,
    Ready
}

public enum AXRHeadTrackerControl : byte {
    Pose = 0,
    Battery
}

public enum AXRHandTrackerControl : byte {
    Status = 0,
    Pose,
    Battery
}

public enum AXRHandTrackerFeedbackControl : byte {
    RenderOnClient = 0,
    RaycastHit,
    Vibration
}

public enum AXRControllerControl : byte {
    Axis2DLThumbstick = 0,
    Axis2DRThumbstick,
    AxisLIndexTrigger,
    AxisRIndexTrigger,
    AxisLHandTrigger,
    AxisRHandTrigger,
    ButtonA,
    ButtonB,
    ButtonX,
    ButtonY,
    ButtonStart,
    ButtonBack,
    ButtonLThumbstick,
    ButtonRThumbstick
}

public enum AXRTouchScreenControl : byte {
    TouchIndexStart = 0,
    TouchIndexEnd = 9
}

public enum AXRTouchPhase : byte {
    Ended = 0,
    Canceled,
    Stationary,
    Moved
}
