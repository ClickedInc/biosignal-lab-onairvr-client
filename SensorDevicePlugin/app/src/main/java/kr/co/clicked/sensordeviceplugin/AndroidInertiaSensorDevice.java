package kr.co.clicked.sensordeviceplugin;

import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorManager;
import android.os.Handler;
import android.util.Log;

import org.joml.Quaternionf;
import org.joml.Vector3f;

public class AndroidInertiaSensorDevice extends AndroidSensorDevice implements IInertiaSensorDevice {
    public AndroidInertiaSensorDevice(SensorManager sensorManager, Handler handler, float samplingRate) {
        super(sensorManager, handler, samplingRate);

        _lastPolledData = new InertiaSensorData();
    }

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
                _lastPolledData.setAcceleration(
                    AndroidInertiaSensorTransform.transformSensorValues(
                        new Vector3f(
                            event.values[0],
                            event.values[1],
                            event.values[2]
                        )
                    )
                );
                break;
            case Sensor.TYPE_GYROSCOPE:
                _lastPolledData.setAngularVelocities(
                    AndroidInertiaSensorTransform.transformSensorValues(
                        new Vector3f(
                                event.values[0],
                                event.values[1],
                                event.values[2]
                        )
                    )
                );
                break;
            case Sensor.TYPE_MAGNETIC_FIELD:
                _lastPolledData.setMagneticField(
                    AndroidInertiaSensorTransform.transformSensorValues(
                        new Vector3f(
                                event.values[0],
                                event.values[1],
                                event.values[2]
                        )
                    )
                );
                break;
            case Sensor.TYPE_ROTATION_VECTOR:
                _lastPolledData.setOrientation(
                    AndroidInertiaSensorTransform.transformRotationVector(
                        new Quaternionf(
                            event.values[0],
                            event.values[1],
                            event.values[2],
                            event.values[3]
                        )
                    )
                );
                break;
            default:
                break;
        }
    }
}
