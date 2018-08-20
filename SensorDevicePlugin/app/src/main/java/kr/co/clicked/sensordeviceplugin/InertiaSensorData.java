package kr.co.clicked.sensordeviceplugin;

import org.joml.Quaternionf;
import org.joml.Vector3f;

import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.Vector;

public class InertiaSensorData {
    public static final int SIZE = 13 * 4;

    public InertiaSensorData() {
        _data = ByteBuffer.allocate(SIZE);
        _data.order(ByteOrder.BIG_ENDIAN);
    }

    private ByteBuffer _data;

    public byte[] getData() { return _data.array(); }

    private void setValue(Quaternionf value, int offset) {
        synchronized (this) {
            _data.position(offset);
            _data.putFloat(value.x);
            _data.putFloat(value.y);
            _data.putFloat(value.z);
            _data.putFloat(value.w);
        }
    }

    private void setValue(Vector3f value, int offset) {
        synchronized (this) {
            _data.position(offset);
            _data.putFloat(value.x);
            _data.putFloat(value.y);
            _data.putFloat(value.z);
        }
    }

    public void setAcceleration(Vector3f value) {  setValue(value, 0); }

    public void setAngularVelocities(Vector3f value) {
        setValue(value, 3 * 4);
    }

    public void setMagneticField(Vector3f value) {
        setValue(value, 6 * 4);
    }

    public void setOrientation(Quaternionf value) {
        setValue(value, 9 * 4);
    }

    @Override
    public String toString() {
        return String.format("Acc:%f %f %f, Gyro:%f %f %f, Mag:%f %f %f, Rot:%f %f %f %f",
                _data.getFloat(0 * 4), _data.getFloat(1 * 4), _data.getFloat(2 * 4),
                _data.getFloat(3 * 4), _data.getFloat(4 * 4), _data.getFloat(5 * 4),
                _data.getFloat(6 * 4), _data.getFloat(7 * 4), _data.getFloat(8 * 4),
                _data.getFloat(9 * 4), _data.getFloat(10 * 4), _data.getFloat(11 * 4), _data.getFloat(12 * 4));
    }
}
