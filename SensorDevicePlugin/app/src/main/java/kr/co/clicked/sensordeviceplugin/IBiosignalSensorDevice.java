package kr.co.clicked.sensordeviceplugin;

public interface IBiosignalSensorDevice extends ISensorDevice {
    BiosignalSensorData getCurrentValue();
}
