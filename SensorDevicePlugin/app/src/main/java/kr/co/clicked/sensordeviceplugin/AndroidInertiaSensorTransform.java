package kr.co.clicked.sensordeviceplugin;

import android.util.Log;

import org.joml.Quaternionf;
import org.joml.Vector3f;

public class AndroidInertiaSensorTransform {
    public static Quaternionf transformRotationVector(Quaternionf value) {
        // convert coordinate from Android to OpenGL
        // Step 1. change axes
        // Step 2. rotate coordinate as device's Z axis looks forward

        Quaternionf axisConverted = new Quaternionf(-value.y, value.x, value.z, value.w);
        return axisConverted.rotateLocalX((float)(-Math.PI / 2));
    }

    public static Vector3f transformSensorValues(Vector3f value) {
        // convert coordinate from Android to OpenGL

        return new Vector3f(-value.y, value.x, value.z);
    }
}
