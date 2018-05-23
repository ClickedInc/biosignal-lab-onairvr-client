package kr.co.clicked.sensordeviceplugin;

import android.hardware.usb.UsbManager;

import com.felhr.usbserial.UsbSerialDevice;

import java.nio.ByteBuffer;

public class ArduinoInertiaSensorDevice extends UsbSensorDevice implements IInertiaSensorDevice {
    public ArduinoInertiaSensorDevice(UsbManager usbManager) {
        super(usbManager);
    }

    // implements UsbSensorDevice
    @Override
    protected int venderId() { return 10755; }

    @Override
    protected int productId() { return 67; }

    @Override
    protected int baudrate() { return 19200; }

//    @Override
//    protected byte commandStart() { return 'r'; }

    @Override
    protected void connectionOpened(UsbSerialDevice serialDevice) {}

    @Override
    protected void connectionWillBeClosed(UsbSerialDevice serialDevice) {}

    @Override
    protected boolean parseReceivedData(UsbSerialDevice serialDevice, ByteBuffer data) {
        return true;
    }

    // implements IInertiaSensorDevice
    @Override
    public InertiaSensorData getCurrentValue() { return null; }
}
