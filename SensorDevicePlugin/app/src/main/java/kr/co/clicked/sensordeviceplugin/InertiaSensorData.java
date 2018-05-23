package kr.co.clicked.sensordeviceplugin;

import java.nio.ByteBuffer;
import java.nio.ByteOrder;

public class InertiaSensorData {
    public static final int SIZE = 13 * 4;

    public InertiaSensorData() {
        _data = ByteBuffer.allocate(SIZE);
        _data.order(ByteOrder.BIG_ENDIAN);
    }

    private ByteBuffer _data;

    public byte[] getData() { return _data.array(); }

    private void setValue(float[] value, int offset, int length) {
        synchronized (this) {
            _data.position(offset);
            for (int i = 0; i < length; i++) {
                _data.putFloat(value[i]);
            }
        }
    }

    public void setAcceleration(float[] value) {
        setValue(value, 0, 3);
    }

    public void setAngularVelocities(float[] value) {
        setValue(value, 3 * 4, 3);
    }

    public void setMagneticField(float[] value) {
        setValue(value, 6 * 4, 3);
    }

    public void setOrientation(float[] value) {
        setValue(value, 9 * 4, 4);
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
