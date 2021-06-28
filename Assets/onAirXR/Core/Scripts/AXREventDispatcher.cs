/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using System;
using System.Runtime.InteropServices;

public abstract class AXREventDispatcher {
    public delegate void MessageReceiveHandler(AXRMessage message);
    public event MessageReceiveHandler MessageReceived;

    protected abstract AXRMessage ParseMessageImpl(IntPtr source, string message);
    protected abstract bool CheckMessageQueueImpl(out IntPtr source, out IntPtr data, out int length);
    protected abstract void RemoveFirstMessageFromQueueImpl();

    protected virtual void OnMessageReceived(AXRMessage message) {
        MessageReceived?.Invoke(message);
    }

    public void DispatchEvent() {
        IntPtr source = default(IntPtr);
        IntPtr data = default(IntPtr);
        int length = 0;

        while (CheckMessageQueueImpl(out source, out data, out length)) {
            var array = new byte[length];
            Marshal.Copy(data, array, 0, length);
            RemoveFirstMessageFromQueueImpl();

            OnMessageReceived(ParseMessageImpl(source, System.Text.Encoding.UTF8.GetString(array, 0, length)));
        }
    }
}
