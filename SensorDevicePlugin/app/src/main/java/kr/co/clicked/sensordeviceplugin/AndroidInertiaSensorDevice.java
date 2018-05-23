package kr.co.clicked.sensordeviceplugin;

import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorManager;
import android.os.Handler;
import android.util.Log;

public class AndroidInertiaSensorDevice extends AndroidSensorDevice implements IInertiaSensorDevice {
    public AndroidInertiaSensorDevice(SensorManager sensorManager, Handler handler, float samplingRate) {
        super(sensorManager, handler, samplingRate);

        _lastPolledData = new InertiaSensorData();
        _rotateCoordinate = new Quaternion((float)Math.sin(Math.PI / 4),
                                           0,
                                           0,
                                           (float)Math.cos(Math.PI / 4));
    }

    private Quaternion _rotateCoordinate;
    private InertiaSensorData _lastPolledData;

    // implements AndroidSensorDevice
    @Override
    protected void registerSensorListeners(int samplingPeriodUs) {
        registerSensorListener(Sensor.TYPE_ACCELEROMETER, samplingPeriodUs);
        registerSensorListener(Sensor.TYPE_GYROSCOPE, samplingPeriodUs);
        registerSensorListener(Sensor.TYPE_MAGNETIC_FIELD, samplingPeriodUs);
        registerSensorListener(Sensor.TYPE_ROTATION_VECTOR, samplingPeriodUs);
    }

    // implements IInertiaSensorDevice
    @Override
    public InertiaSensorData getCurrentValue() {
        return _lastPolledData;
    }

    // implements SensorEventListener
    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy) {
        // do nothing
    }

    @Override
    public void onSensorChanged(SensorEvent event) {
        switch (event.sensor.getType()) {
            case Sensor.TYPE_ACCELEROMETER:
                _lastPolledData.setAcceleration(event.values);
                break;
            case Sensor.TYPE_GYROSCOPE:
                _lastPolledData.setAngularVelocities(event.values);
                break;
            case Sensor.TYPE_MAGNETIC_FIELD:
                _lastPolledData.setMagneticField(event.values);
                break;
            case Sensor.TYPE_ROTATION_VECTOR:
                // convert coodinate system from Android to Unity
                // Step 1. change axes
                // Step 2. rotate coordinate as device's Z axis looks forward

                Quaternion orientation =
                        new Quaternion(event.values[1],
                                       -event.values[0],
                                       event.values[2],
                                       event.values[3]);
                orientation = _rotateCoordinate.times(orientation);

                _lastPolledData.setOrientation(new float[] {
                        orientation.x, orientation.y, orientation.z, orientation.w
                });
                break;
            default:
                break;
        }
    }
}
