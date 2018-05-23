package kr.co.clicked.sensordeviceplugin;

import android.hardware.Sensor;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.Handler;
import android.util.Log;

public abstract class AndroidSensorDevice implements ISensorDevice, SensorEventListener {
    public AndroidSensorDevice(SensorManager sensorManager, Handler handler, float samplingRate) {
        super();

        _sensorManager = sensorManager;
        _handler = handler;
        _samplingPeriodUs = (int)(1000 * 1000 / samplingRate);
    }

    private SensorManager _sensorManager;
    private Handler _handler;
    private int _samplingPeriodUs;

    protected void registerSensorListener(int sensorType, int samplingPeriodUs) {
        Sensor sensor = _sensorManager.getDefaultSensor(sensorType);
        if (sensor != null) {
            _sensorManager.registerListener(this, sensor, samplingPeriodUs, _handler);
        }
        else {
            Log.d(this.getClass().getName(), "failed to register : " + sensorType);
        }
    }

    protected abstract void registerSensorListeners(int samplingPeriodUs);

    // implements ISensorDevice
    @Override
    public boolean available() { return true; }

    @Override
    public boolean open() {
        registerSensorListeners(_samplingPeriodUs);
        return true;
    }

    @Override
    public void update() {}

    @Override
    public void close() {
        _sensorManager.unregisterListener(this);
    }

    @Override
    public void updateDeviceStatus() {}
}
