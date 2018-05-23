package kr.co.clicked.sensordeviceplugin;

import android.hardware.usb.UsbManager;
import android.os.SystemClock;

import com.felhr.usbserial.UsbSerialDevice;

import java.nio.ByteBuffer;

public class OpenBciSensorDevice extends UsbSensorDevice implements IBiosignalSensorDevice {
    private static final int CYTON_PACKET_SIZE = 33;
    private static final int CYTON_CHANNELS = 8;
    private static final byte CYTON_COMMAND_RESET = 'v';
    private static final byte CYTON_COMMAND_START = 'b';
    private static final byte CYTON_COMMAND_STOP = 's';
    private static final float CYTON_UV_PER_COUNT = 0.02235f;
    private static final int CYTON_DEFAULT_GAIN = 24;
    private static final long INVALID_RESET_TIME = -1;

    private enum State {
        Resetting,
        Normal,
        Dropping
    }

    public OpenBciSensorDevice(UsbManager usbManager) {
        super(usbManager);

        _commandBuffer = new byte[1];
        _lastPolledData = new BiosignalSensorData();
        _resetTime = INVALID_RESET_TIME;
    }

    private State _state;
    private long _resetTime;
    private byte[] _commandBuffer;
    private BiosignalSensorData _lastPolledData;

    private int parse24bitSignedInt(byte[] data, int offset) {
        int result = (
                ((0xFF & data[offset]) << 16) |
                ((0xFF & data[offset + 1]) << 8) |
                 (0xFF & data[offset + 2])
        );
        if ((result & 0x00800000) > 0) {
            result |= 0xFF000000;
        }
        else {
            result &= 0x00FFFFFF;
        }
        return result;
    }

    private float scaleFactor(int gain) {
        return 4.5f * 1000000 / gain / ((1 << 23) - 1);
    }

    private void sendCommand(UsbSerialDevice serialDevice, byte command) {
        _commandBuffer[0] = command;
        serialDevice.syncWrite(_commandBuffer, 1);
    }

    private boolean nextPacketValid(ByteBuffer data) {
        byte header = data.get(data.position());
        byte footer = data.get(data.position() + CYTON_PACKET_SIZE - 1);
        return ((header & 0xFF) == 0xA0) && ((footer & 0xF8) == 0xC0);
    }

    // implements UsbSensorDevice
    @Override
    protected int venderId() { return 1027; }

    @Override
    protected int productId() { return 24597; }

    @Override
    protected int baudrate() { return 115200; }

    @Override
    protected void connectionOpened(UsbSerialDevice serialDevice) {
        _state = State.Resetting;
        _resetTime = SystemClock.uptimeMillis();
        sendCommand(serialDevice, CYTON_COMMAND_RESET);
    }

    @Override
    protected void connectionWillBeClosed(UsbSerialDevice serialDevice) {
        sendCommand(serialDevice, CYTON_COMMAND_STOP);
    }

    @Override
    protected boolean parseReceivedData(UsbSerialDevice serialDevice, ByteBuffer data) {
        if (_state == State.Resetting) {
            if (data.remaining() >= 3 &&
                    new String(data.array(), data.arrayOffset() + data.limit() - 3, 3).equals("$$$")) {
                // TODO parse to check if OpenBCI board is turned on.
                data.position(data.limit());

                _state = State.Normal;
                _resetTime = INVALID_RESET_TIME;
                sendCommand(serialDevice, CYTON_COMMAND_START);
            }
            else if (SystemClock.uptimeMillis() - _resetTime > 1000) {
                _resetTime = SystemClock.uptimeMillis();
                sendCommand(serialDevice, CYTON_COMMAND_RESET);
            }
        }
        else if (_state == State.Normal) {
            while (data.remaining() >= CYTON_PACKET_SIZE) {
                if (nextPacketValid(data) == false) {
                    _state = State.Dropping;
                    return false;
                }
                /* byte header = */ data.get();
                /* byte sampleNum = */ data.get();

                float[] values = new float[BiosignalSensorData.CHANNELS];
                for (int i = 0; i < values.length; i++) {
                    values[i] = i < CYTON_CHANNELS ?
                                    parse24bitSignedInt(data.array(),
                                                        data.arrayOffset() + data.position() + i * 3
                                    ) * scaleFactor(CYTON_DEFAULT_GAIN) : 0;
                }
                _lastPolledData.setData(values);

                data.position(data.position() + 3 * CYTON_CHANNELS);
                data.position(data.position() + 6); // skip aux

                /* byte footer = */ data.get();
            }
        }
        else if (_state == State.Dropping) {
            while (data.remaining() >= CYTON_PACKET_SIZE) {
                if (nextPacketValid(data)) {
                    _state = State.Normal;
                    return false;
                }
                data.get();
            }
        }
        return true;
    }

    // implements IBiosignalSensorDevice
    @Override
    public BiosignalSensorData getCurrentValue() {
        return _lastPolledData;
    }
}
