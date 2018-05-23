package kr.co.clicked.sensordeviceplugin;

import java.nio.ByteBuffer;
import java.nio.ByteOrder;

public class BiosignalSensorData {
    public static final int CHANNELS = 8;
    public static final int SIZE = 32;

    public BiosignalSensorData() {
        _data = ByteBuffer.allocate(SIZE);
        _data.order(ByteOrder.BIG_ENDIAN);
    }

    private ByteBuffer _data;

    public byte[] getData() {
        return _data.array();
    }

    public void setData(float[] data) {
        assert(data.length * 4 >= SIZE);

        _data.clear();
        for (int i = 0; i < data.length; i++) {
            _data.putFloat(data[i]);
        }
    }

    @Override
    public String toString() {
        return String.format("1:%f, 2:%f, 3:%f, 4:%f, 5:%f, 6:%f, 7:%f, 8:%f",
                _data.getFloat(0),
                _data.getFloat(1 * 4),
                _data.getFloat(2 * 4),
                _data.getFloat(3 * 4),
                _data.getFloat(4 * 4),
                _data.getFloat(5 * 4),
                _data.getFloat(6 * 4),
                _data.getFloat(7 * 4)
                );
    }
}
