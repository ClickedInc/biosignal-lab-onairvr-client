using System;
using System.Collections;
using System.Collections.Generic;
using NetMQ.Sockets;
using UnityEngine;

public class MotionData {
	private const int OrientationStart = 2 + 4 * 17;

	public static Quaternion GetOrientation(byte[] data) {
		byte[] bytes = new byte[4 * 4];
		Array.Copy(data, OrientationStart, bytes, 0, 4 * 4);

		Quaternion result = Quaternion.identity;
		for (int i = 0; i < 4; i++) {
			if (BitConverter.IsLittleEndian) {
				Array.Reverse(bytes, i * 4, 4);
			}
			result[i] = BitConverter.ToSingle(bytes, i * 4);
		}
		return result;
	}

	public static void SetOrientation(byte[] data, Quaternion orientation) {
		for (int i = 0; i < 4; i++) {
			byte[] bytes = BitConverter.GetBytes(orientation[i]);
			if (BitConverter.IsLittleEndian) {
				Array.Reverse(bytes);
			}
			Array.Copy(bytes, 0, data, OrientationStart + i * 4, 4);
		}
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
			Array.Copy(data, converted, converted.Length);

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
		return _manager.Call<byte[]>("getNextMotionData");
	}
}
