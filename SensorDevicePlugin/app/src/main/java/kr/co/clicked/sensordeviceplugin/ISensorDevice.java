package kr.co.clicked.sensordeviceplugin;

public interface ISensorDevice {
    boolean available();
    boolean open();
    void update();
    void close();

    void updateDeviceStatus();
}
