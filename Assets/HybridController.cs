using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class HybridController : MonoBehaviour {

  Text text;

  IEnumerator Start () {
    text = GetComponentInChildren<Text> ();

    // Make sure Gyroscope is enabled.
    Input.gyro.enabled = true;

    // Every 10 seconds toggle VR mode
    while (true) {
#if UNITY_ANDROID
      Debug.Log ("------------------------------");
      LogAndUpdateText ();
      yield return WatchScreenOrientation ();
      LogAndUpdateText ();
      yield return new WaitForSeconds (10f);
      yield return SwitchToVR ("daydream");

      Debug.Log ("------------------------------");
      LogAndUpdateText ();
      yield return WatchScreenOrientation ();
      LogAndUpdateText ();
      yield return new WaitForSeconds (10f);
      yield return SwitchOutOfVr ();
#endif // UNITY_ANDROID

      Debug.Log ("------------------------------");
      LogAndUpdateText ();
      yield return WatchScreenOrientation ();
      LogAndUpdateText ();
      yield return new WaitForSeconds (10f);
      yield return SwitchToVR ("cardboard");

      Debug.Log ("------------------------------");
      LogAndUpdateText ();
      yield return WatchScreenOrientation ();
      LogAndUpdateText ();
      yield return new WaitForSeconds (10f);
      yield return SwitchOutOfVr ();
    }
  }

  IEnumerator WatchScreenOrientation () {
    ScreenOrientation o = Screen.orientation;
    for (int i = 0; i < 180; i++) {
      ScreenOrientation n = Screen.orientation;
      if (n != o) {
        Debug.LogWarning ("After " + i + " frames: " + o + " => " + n);
        text.text = text.text + "\n\n<color=#f00>After " + i + " frames: " + o + " => " + n + "</color>\n";
        o = n;
      }
      yield return null;
    }
  }

  void LogAndUpdateText () {
    Debug.Log ("XRSettings.supportedDevices=" + string.Join (", ", XRSettings.supportedDevices) + ", XRSettings.loadedDeviceName='" + XRSettings.loadedDeviceName + "'");
    try {
      Debug.Log ("GvrSettings.ViewerPlatform=" + GvrSettings.ViewerPlatform);
    } catch (Exception e) {
      Debug.Log ("e=" + e);
    }

    string vp;
    try {
      vp = GvrSettings.ViewerPlatform.ToString ();
    } catch (Exception e) {
      vp = "Exception:" + e.ToString ();
    }

    text.fontSize = XRSettings.enabled ? 35 : 20;
    text.text =
      "Screen.orientation: <b>" + Screen.orientation + "</b>\n" +
      "XRSettings.loadedDeviceName: <b>'" + XRSettings.loadedDeviceName + "'</b>\n" +
      "GvrSettings.ViewerPlatform: <b>" + vp + "</b>";
  }

  IEnumerator SwitchToVR (string desiredDevice) {
    if (UnityEngine.XR.XRSettings.loadedDeviceName == desiredDevice) {
      // Workaround for issue # 826, VR device already loaded.
      yield break;
    }

    Debug.Log ("XRSettings.LoadDeviceByName('" + desiredDevice + "')");
    XRSettings.LoadDeviceByName (desiredDevice);

    // Wait one frame!
    yield return null;

    // Now it's ok to enable VR mode.
    XRSettings.enabled = true;
  }

  IEnumerator SwitchOutOfVr () {
    Debug.Log ("XRSettings.LoadDeviceByName('" + "" + "')");
    XRSettings.LoadDeviceByName (""); // Empty string loads the "None" device.

    // Wait one frame!
    yield return null;

    // Not needed, loading the None (`""`) device automatically sets `XRSettings.enabled` to `false`.
    // XRSettings.enabled = false;

    // If you only have one camera in your scene, you can just call `Camera.main.ResetAspect()` instead.
    ResetCameras ();
  }

  // Resets local rotation and calls `ResetAspect()` on all enabled VR cameras.
  void ResetCameras () {
    // Camera looping logic copied from GvrEditorEmulator.cs
    for (int i = 0; i < Camera.allCameras.Length; i++) {
      Camera cam = Camera.allCameras[i];
      if (cam.enabled && cam.stereoTargetEye != StereoTargetEyeMask.None) {

        // Reset local rotation. (Only required if you change the local rotation while in non-VR mode.)
        cam.transform.localRotation = Quaternion.identity;

        // Reset local position. (Only required if you change the local position while in non-VR mode.)
        cam.transform.localPosition = Vector3.zero;

        // Reset aspect ratio based on normal (non-VR) screen size.
        // Required in certain versions of Unity, see github.com/googlevr/gvr-unity-sdk/issues/628
        cam.ResetAspect ();

        // Don't need to reset camera `fieldOfView`, since it's restored to the original value automatically.
      }
    }
  }

  // Optional, allows user to drag left/right to rotate the world.
  private const float DRAG_RATE = .2f;
  float dragYawDegrees;

  void Update () {
    if (XRSettings.enabled) {
      // Unity takes care of updating camera transform in VR.
      return;
    }

    // http://android-developers.blogspot.com/2010/09/one-screen-turn-deserves-another.html
    // https://developer.android.com/guide/topics/sensors/sensors_overview.html#sensors-coords
    //
    //     y                                       x
    //     |  Gyro upright phone                   |  Gyro landscape left phone
    //     |                                       |
    //     |______ x                      y  ______|
    //     /                                       \
    //    /                                         \
    //   z                                           z
    //
    //
    //  y
    //  |  z   Unity
    //  | /
    //  |/_____ x
    //

    // Update dragYawDegrees based on user touch.
    CheckDrag ();

    transform.localRotation =
      // Allow user to drag left/right to adjust direction they're facing.
      Quaternion.Euler (0f, -dragYawDegrees, 0f) *

      // Neutral position is phone held upright, not flat on a table.
      Quaternion.Euler (90f, 0f, 0f) *

      // Sensor reading, assuming default `Input.compensateSensors == true`.
      Input.gyro.attitude *

      // So image is not upside down.
      Quaternion.Euler (0f, 0f, 180f);
  }

  void CheckDrag () {
    if (Input.touchCount != 1) {
      return;
    }

    Touch touch = Input.GetTouch (0);
    if (touch.phase != TouchPhase.Moved) {
      return;
    }

    dragYawDegrees += touch.deltaPosition.x * DRAG_RATE;
  }

}