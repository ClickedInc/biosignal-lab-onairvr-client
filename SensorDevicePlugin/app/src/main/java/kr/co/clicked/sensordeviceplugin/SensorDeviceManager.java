package kr.co.clicked.sensordeviceplugin;

import android.content.Context;
import android.hardware.SensorManager;
import android.hardware.usb.UsbManager;
import android.os.Handler;
import android.os.HandlerThread;
import android.os.SystemClock;
import android.util.Log;

import java.util.ArrayList;
import java.util.List;
import java.util.NoSuchElementException;
import java.util.concurrent.ArrayBlockingQueue;

public class SensorDeviceManager extends HandlerThread {
    private static final String LogTag = "SensorDeviceManager";

    private class SensorDataPollTask implements Runnable {
        public SensorDataPollTask(float pollingRate, float intervalToReport) {
            super();

            _startTime = SystemClock.uptimeMillis();
            _pollingRate = pollingRate;
            _elapsedIntervalCount = 0;
            _intervalToReport = (long)(intervalToReport * 1000);
            _sampleNumber = (byte)0;
        }

        private float _pollingRate;
        private long _startTime;
        private long _elapsedIntervalCount;
        private long _polledCount;
        private long _intervalToReport;
        private byte _sampleNumber;

        private long nextTimeToPoll(long current) {
            long result;
            do {
                result = _startTime + (long)((++_elapsedIntervalCount) * (1000 / _pollingRate));
            } while (result <= current);

            _polledCount++;
            if (current - _startTime >= _intervalToReport) {
                Log.d(LogTag, String.format("sampling rate : %f/sec",
                        _polledCount * 1000.0 / (current - _startTime)));

                _startTime = current;
                _elapsedIntervalCount = 0;
                _polledCount = 0;
            }
            return result;
        }

        @Override
        public void run() {
            assert(_handler != null);

            long current = SystemClock.uptimeMillis();

            synchronized(this) {
                if (_currentBiosignalSensorDevice != null) {
                    _currentBiosignalSensorDevice.update();
                }
                if (_currentMotionSensorDevice != null) {
                    _currentMotionSensorDevice.update();
                }

                MotionData motionData =
                        MotionData.create(_sampleNumber,
                                          current / 1000.0,
                                          _currentBiosignalSensorDevice != null ? _currentBiosignalSensorDevice.getCurrentValue() : null,
                                          _currentMotionSensorDevice != null ? _currentMotionSensorDevice.getCurrentValue() : null);
                if (motionData != null) {
                    try {
                        _motionData.add(motionData);
                        _sampleNumber++;
                    }
                    catch (IllegalStateException e) {
                        MotionData.dispose(motionData);
                    }
                }
            }
            _handler.postAtTime(this, nextTimeToPoll(current));
        }
    }

    public SensorDeviceManager(Context context, float sampleRate) {
        super("SensorDeviceManager");

        _sampleRate = sampleRate;

        _usbManager = (UsbManager)context.getSystemService(Context.USB_SERVICE);
        assert(_usbManager != null);

        _sensorManager = (SensorManager)context.getSystemService(Context.SENSOR_SERVICE);
        assert(_sensorManager != null);

        _motionData = new ArrayBlockingQueue<>(4);
    }

    private UsbManager _usbManager;
    private SensorManager _sensorManager;
    private Handler _handler;
    private float _sampleRate;
    private SensorDataPollTask _pollTask;
    private ArrayBlockingQueue<MotionData> _motionData;

    private ArrayList<IBiosignalSensorDevice> _biosignalSensorDevices;
    private ArrayList<IInertiaSensorDevice> _motionSensorDevices;
    private IBiosignalSensorDevice _currentBiosignalSensorDevice;
    private IInertiaSensorDevice _currentMotionSensorDevice;

    private void createBiosignalSensorDevices() {
        _biosignalSensorDevices = new ArrayList<>();
        _biosignalSensorDevices.add(new OpenBciSensorDevice(_usbManager));
    }

    private void createMotionSensorDevices(Handler handler) {
        _motionSensorDevices = new ArrayList<>();
        _motionSensorDevices.add(new ArduinoInertiaSensorDevice(_usbManager));
        _motionSensorDevices.add(new AndroidInertiaSensorDevice(_sensorManager, handler, _sampleRate * 2));
    }

    private ISensorDevice getFirstAvailableSensorDevice(List devices) {
        for (int i = 0; i < devices.size(); i++) {
            if (devices.get(i) instanceof ISensorDevice && ((ISensorDevice)devices.get(i)).available()) {
                return (ISensorDevice)devices.get(i);
            }
        }
        return null;
    }

    private ISensorDevice updateCurrentSensorDevice(List candidates, ISensorDevice current) {
        ISensorDevice result = getFirstAvailableSensorDevice(candidates);
        if (current != result) {
            if (current != null) {
                current.close();
            }
            if (result.open()) {
                return result;
            }
        }
        return current;
    }

    private void updateCurrentSensorDevices() {
        for (ISensorDevice device : _biosignalSensorDevices) {
            device.updateDeviceStatus();
        }
        for (ISensorDevice device : _motionSensorDevices) {
            device.updateDeviceStatus();
        }

        synchronized (_pollTask) {
            _currentBiosignalSensorDevice =
                    (IBiosignalSensorDevice)updateCurrentSensorDevice(_biosignalSensorDevices,
                                                                      _currentBiosignalSensorDevice);
            _currentMotionSensorDevice =
                    (IInertiaSensorDevice)updateCurrentSensorDevice(_motionSensorDevices,
                                                                   _currentMotionSensorDevice);
        }
    }

    public void startup() {
        assert(_handler == null);
        assert(_pollTask == null);

        start();
        _handler = new Handler(getLooper());
        _pollTask = new SensorDataPollTask(_sampleRate, 5.0f);

        createBiosignalSensorDevices();
        createMotionSensorDevices(_handler);

        updateCurrentSensorDevices();

        _handler.post(_pollTask);
    }

    public byte[] getNextMotionData() {
        try {
            MotionData data = _motionData.remove();
            MotionData.dispose(data);
            return data.getData();
        }
        catch (NoSuchElementException e) {
            return null;
        }
    }

    public void shutdown() {
        assert(_handler != null);

        _handler.removeCallbacks(_pollTask);

        try {
            join();
        }
        catch (InterruptedException e) {
            e.printStackTrace();
        }

        if (_currentBiosignalSensorDevice != null) {
            _currentBiosignalSensorDevice.close();
            _currentBiosignalSensorDevice = null;
        }
        if (_currentMotionSensorDevice != null) {
            _currentMotionSensorDevice.close();
            _currentMotionSensorDevice = null;
        }

        _handler = null;
        _pollTask = null;
    }
}
