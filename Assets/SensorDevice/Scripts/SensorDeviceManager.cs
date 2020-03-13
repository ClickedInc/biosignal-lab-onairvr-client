using System;
using System.Collections;
using System.Collections.Generic;
using NetMQ.Sockets;
using UnityEngine;

public class MotionData {
	private const int OrientationStart = 2 + 4 * 17;
	private const int TimestampStart = 2 + 4 * 21;

	public static Quaternion GetOrientation(byte[] data) {
		byte[] bytes = new byte[4 * 4];
		Buffer.BlockCopy(data, OrientationStart, bytes, 0, 4 * 4);

		Quaternion result = Quaternion.identity;
		for (int i = 0; i < 4; i++) {
			if (BitConverter.IsLittleEndian) {
				Array.Reverse(bytes, i * 4, 4);
			}

            // convert OpenGL to Unity
            result[i] = (i == 0 || i == 1 ? -1.0f : 1.0f) * BitConverter.ToSingle(bytes, i * 4);
        }
		return result;
	}

    public static int SetPosition(byte[] data, int offset, Vector3 position) {
        int next = offset;
        for (int i = 0; i < 3; i++) {
            byte[] bytes = BitConverter.GetBytes(position[i]);
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }
            Buffer.BlockCopy(bytes, 0, data, offset + i * 4, 4);
            next += 4;
        }
        return next;
    }

	public static void SetOrientation(byte[] data, Quaternion orientation) {
        SetOrientation(data, OrientationStart, orientation);
	}

    public static int SetOrientation(byte[] data, int offset, Quaternion orientation) {
        int next = offset;
        for (int i = 0; i < 4; i++) {
            // convert Unity to OpenGL
            byte[] bytes = BitConverter.GetBytes(
                (i == 0 || i == 1 ? -1.0f : 1.0f) * orientation[i]
            );
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }
            Buffer.BlockCopy(bytes, 0, data, offset + i * 4, 4);
            next += 4;
        }
        return next;
    }

	public static long GetTimestamp(byte[] data) {
		byte[] bytes = new byte[8];
		Buffer.BlockCopy(data, TimestampStart, bytes, 0, 8);

		if (BitConverter.IsLittleEndian) {
			Array.Reverse(bytes);
		}
		return BitConverter.ToInt64(bytes, 0);
	}

    public static void SetTimestamp(byte[] data, long timestamp) {
        byte[] bytes = BitConverter.GetBytes(timestamp);
        if (BitConverter.IsLittleEndian) {
            Array.Reverse(bytes);
        }
        Buffer.BlockCopy(bytes, 0, data, TimestampStart, bytes.Length);
    }

	public static string ToString(byte[] data) {
		int biosignalStart = 2;
		int accelerationStart = biosignalStart + 4 * 8;
		int angularVelocitiesStart = accelerationStart + 4 * 3;
		int magneticFieldStart = angularVelocitiesStart + 4 * 3;
		int orientationStart = magneticFieldStart + 4 * 3;
		int timeStampStart = orientationStart + 4 * 4;
		
		byte[] converted = data;
		if (BitConverter.IsLittleEndian) {
			converted = new byte[data.Length];
			Buffer.BlockCopy(data, 0, converted, 0, converted.Length);

			for (int i = biosignalStart; i < timeStampStart; i += 4) {
				Array.Reverse(converted, i, 4);
			}
			Array.Reverse(converted, timeStampStart, 8);
		}

		return string.Format("header: {0:X}, sample {1:X}, " +
							 "biosignal: {2} {3} {4} {5} {6} {7} {8} {9}, " +
							 "acceleration: {10} {11} {12}, " +
							 "angular velocities: {13} {14} {15}, " +
			                 "magnetic field: {16} {17} {18}, " +
			                 "orientation: {19} {20} {21} {22}, " +
			                 "timeStamp: {23}, " +
			                 "footer: {24:X}", 
							 converted[0], converted[1],
							 BitConverter.ToSingle(converted, biosignalStart), BitConverter.ToSingle(converted, biosignalStart + 4),
							 BitConverter.ToSingle(converted, biosignalStart + 4 * 2), BitConverter.ToSingle(converted, biosignalStart + 4 * 3),
							 BitConverter.ToSingle(converted, biosignalStart + 4 * 4), BitConverter.ToSingle(converted, biosignalStart + 4 * 5),
							 BitConverter.ToSingle(converted, biosignalStart + 4 * 6), BitConverter.ToSingle(converted, biosignalStart + 4 * 7),
							 BitConverter.ToSingle(converted, accelerationStart), BitConverter.ToSingle(converted, accelerationStart + 4), BitConverter.ToSingle(converted, accelerationStart + 4 * 2),
						     BitConverter.ToSingle(converted, angularVelocitiesStart), BitConverter.ToSingle(converted, angularVelocitiesStart + 4), BitConverter.ToSingle(converted, angularVelocitiesStart + 4 * 2),
							 BitConverter.ToSingle(converted, magneticFieldStart), BitConverter.ToSingle(converted, magneticFieldStart + 4), BitConverter.ToSingle(converted, magneticFieldStart + 4 * 2),
						     BitConverter.ToSingle(converted, orientationStart), BitConverter.ToSingle(converted, orientationStart + 4),
					         BitConverter.ToSingle(converted, orientationStart + 4 * 2), BitConverter.ToSingle(converted, orientationStart + 4 * 3),
							 BitConverter.ToDouble(converted, timeStampStart),
							 converted[timeStampStart + 8]
			);
	}
}

public class SensorDeviceManager : MonoBehaviour {
	private AndroidJavaObject _manager;

	void Awake() {
		AndroidJavaClass clsUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		Debug.Assert(clsUnityPlayer != null);
		AndroidJavaObject activity = clsUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
		Debug.Assert(activity != null);
		
		_manager = new AndroidJavaObject("kr.co.clicked.sensordeviceplugin.SensorDeviceManager", activity, 120.0f);
	}

	void Start() {
		_manager.Call("startup");
	}

	void OnDestroy() {
		_manager.Call("shutdown");
	}

	public byte[] getNextMotionData() {
        return (byte[])(Array)_manager.Call<sbyte[]>("getNextMotionData");
	}
}
