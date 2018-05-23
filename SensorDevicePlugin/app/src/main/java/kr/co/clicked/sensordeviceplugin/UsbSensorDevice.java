package kr.co.clicked.sensordeviceplugin;

import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbManager;
import android.util.Log;

import com.felhr.usbserial.UsbSerialDevice;
import com.felhr.usbserial.UsbSerialInterface;

import java.io.ByteArrayInputStream;
import java.nio.Buffer;
import java.nio.ByteBuffer;

public abstract class UsbSensorDevice implements ISensorDevice {
    public UsbSensorDevice(UsbManager usbManager) {
        _usbManager = usbManager;

        _recvBuffer = ByteBuffer.allocate(4 * 1024);
    }

    private UsbManager _usbManager;
    private UsbDevice _usbDevice;
    private UsbDeviceConnection _usbConnection;
    private UsbSerialDevice _serialDevice;
    private ByteBuffer _recvBuffer;

    protected abstract int venderId();
    protected abstract int productId();
    protected abstract int baudrate();

    protected abstract void connectionOpened(UsbSerialDevice serialDevice);
    protected abstract void connectionWillBeClosed(UsbSerialDevice serialDevice);
    protected abstract boolean parseReceivedData(UsbSerialDevice serialDevice, ByteBuffer data);

    // implements ISensorDevice
    @Override
    public boolean available() { return _usbDevice != null; }

    @Override
    public boolean open() {
        if (_usbDevice != null) {
            assert(_usbConnection == null);
            _usbConnection = _usbManager.openDevice(_usbDevice);
            _serialDevice = UsbSerialDevice.createUsbSerialDevice(_usbDevice, _usbConnection);
            if (_serialDevice != null && _serialDevice.syncOpen()) {
                _serialDevice.setBaudRate(baudrate());
                _serialDevice.setDataBits(UsbSerialInterface.DATA_BITS_8);
                _serialDevice.setStopBits(UsbSerialInterface.STOP_BITS_1);
                _serialDevice.setParity(UsbSerialInterface.PARITY_NONE);
                _serialDevice.setFlowControl(UsbSerialInterface.FLOW_CONTROL_OFF);

                connectionOpened(_serialDevice);
                return true;
            }
        }
        if (_serialDevice != null) {
            _serialDevice = null;
        }
        if (_usbConnection != null) {
            _usbConnection.close();
            _usbConnection = null;
        }
        return false;
    }

    @Override
    public void update() {
        if (_serialDevice != null) {
            int read = _serialDevice.syncRead(_recvBuffer.array(), _recvBuffer.position(), 1);
            if (read > 0) {
                assert(_recvBuffer.position() + read < _recvBuffer.capacity());
                _recvBuffer.position(_recvBuffer.position() + read);

                ByteBuffer data = ByteBuffer.wrap(_recvBuffer.array(), 0, _recvBuffer.position());
                while (parseReceivedData(_serialDevice, data) == false) {
                    // just repeat until all received data parsed
                }

                if (data.position() > 0) {
                    _recvBuffer.flip();
                    _recvBuffer.position(data.position());
                    _recvBuffer.compact();
                }
            }
        }
    }

    @Override
    public void close() {
        if (_serialDevice != null) {
            connectionWillBeClosed(_serialDevice);

            _serialDevice.syncClose();
            _serialDevice = null;
        }
        if (_usbConnection != null) {
            _usbConnection.close();
            _usbConnection = null;
        }
    }

    @Override
    public void updateDeviceStatus() {
        for (UsbDevice device : _usbManager.getDeviceList().values()) {
            if (device.getVendorId() == venderId() && device.getProductId() == productId()) {
                _usbDevice = device;
                assert(_usbConnection == null);
                return;
            }
        }

        _usbDevice = null;
        close();
    }
}
