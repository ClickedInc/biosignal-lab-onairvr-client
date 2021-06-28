/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

public class AXRTicker {
    private double _interval;
    private double _elapsedTimeFromLastExpire;
    private long _prevTicks;

    public bool isSet {
        get {
            return _interval > 0.0D;
        }
    }
    public bool tickOnFrame { get; private set; }

    public void Set(float ratePerSec) {
        if (ratePerSec > 0.0f) {
            _interval = 1.0D / ratePerSec;
            _elapsedTimeFromLastExpire = _interval;
            _prevTicks = System.DateTime.Now.Ticks;
        }
    }

    public void Reset() {
        _interval = 0.0D;
        tickOnFrame = false;
    }

    public void UpdatePerFrame() {
        if (isSet == false) {
            return;
        }
        
        long currentTicks = System.DateTime.Now.Ticks;
        _elapsedTimeFromLastExpire += (currentTicks - _prevTicks) / 10000000.0D;   // resolution of ticks is 100 ns

        if (_elapsedTimeFromLastExpire >= _interval) {
            tickOnFrame = true;
            _elapsedTimeFromLastExpire = _elapsedTimeFromLastExpire - _interval; // add remainings instead of just making it zero.

            if (_elapsedTimeFromLastExpire >= _interval) {  // prevent from accumulating elapsed time when frame time is longer than _interval
                _elapsedTimeFromLastExpire = _interval;
            }
        }
        else {
            tickOnFrame = false;
        }

        _prevTicks = currentTicks;
    }
}
