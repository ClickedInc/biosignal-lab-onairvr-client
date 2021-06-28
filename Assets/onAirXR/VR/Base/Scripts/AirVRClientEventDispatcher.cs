/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using System;

public class AirVRClientEventDispatcher : AXREventDispatcher {
    protected override AXRMessage ParseMessageImpl(IntPtr source, string message) {
        return AirVRClientMessage.Parse(source, message);
    }

    protected override bool CheckMessageQueueImpl(out IntPtr source, out IntPtr data, out int length) {
		return AXRClientPlugin.CheckMessageQueue(out source, out data, out length);
    }

    protected override void RemoveFirstMessageFromQueueImpl() {
        AXRClientPlugin.RemoveFirstMessageFromQueue();
    }

    internal void SendUserData(byte[] data) {
        AXRClientPlugin.RequestSendUserData(data);
    }
}
