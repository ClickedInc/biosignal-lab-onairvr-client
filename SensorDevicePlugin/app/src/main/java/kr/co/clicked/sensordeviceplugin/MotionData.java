package kr.co.clicked.sensordeviceplugin;

import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.concurrent.ArrayBlockingQueue;
import java.util.concurrent.BlockingQueue;

public class MotionData {
    private static class Pool {
        private BlockingQueue<MotionData> _items;

        public Pool(int size) {
            _items = new ArrayBlockingQueue<MotionData>(size);
            for (int i = 0; i < size; i++) {
                _items.add(new MotionData());
            }
        }

        public MotionData retain() {
            return _items.remove();
        }

        public void release(MotionData item) {
            item.clear();

            assert(_items.contains(item) == false);
            _items.add(item);
        }
    }

    private static Pool _pool = new Pool(10);

    private static int size() {
        return 1 +      // header (0xA0)
                1 +      // sample number
                BiosignalSensorData.SIZE +
                InertiaSensorData.SIZE +
                8 +      // timestamp
                1;       // footer (0xC7)
    }

    public static MotionData create(byte sampleNumber, long timeStamp, BiosignalSensorData biosignal, InertiaSensorData inertia) {
        MotionData result = _pool.retain();
        if (result != null) {
            result.fill(sampleNumber, timeStamp, biosignal, inertia);
        }
        return result;
    }

    public static void dispose(MotionData data) {
        _pool.release(data);
    }

    private MotionData() {
        _data = ByteBuffer.allocate(size());
        _data.order(ByteOrder.BIG_ENDIAN);
    }

    private ByteBuffer _data;

    private void fillWithZeros(ByteBuffer data, int count) {
        for (int i = 0; i < count; i++) {
            data.put((byte)0);
        }
    }

    private void fill(byte sampleNumber, long timeStamp, BiosignalSensorData biosignal, InertiaSensorData inertia) {
        _data.put((byte)0xA0);
        _data.put(sampleNumber);

        if (biosignal != null) {
            _data.put(biosignal.getData(), 0, BiosignalSensorData.SIZE);
        }
        else {
            fillWithZeros(_data, BiosignalSensorData.SIZE);
        }

        if (inertia != null) {
            _data.put(inertia.getData(), 0, InertiaSensorData.SIZE);
        }
        else {
            fillWithZeros(_data, InertiaSensorData.SIZE);
        }

        _data.putLong(timeStamp);
        _data.put((byte)0xC7);
    }

    private void clear() {
        _data.clear();
    }

    public byte[] getData() {
        return _data.array();
    }
}
