// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

package com.microsoft.samplenativeandroid;

import android.app.Activity;
import android.content.Context;
import android.content.res.AssetManager;

// JNI interface to native layer
public class JniInterface {
    static {
        System.loadLibrary("azurespatialanchorsapplication");
    }

    public static native void onCreate(AssetManager assetManager);

    public static native void onResume(Context context, Activity activity);

    public static native void onPause();

    public static native void onDestroy();

    public static native void onSurfaceCreated();

    public static native void onSurfaceChanged(int displayRotation, int width, int height);

    public static native void onDrawFrame();

    public static native void onTouched(float x, float y);

    public static native void onBasicButtonPress();

    public static native void onNearbyButtonPress();

    public static native void onCoarseRelocButtonPress();

    public static native void advanceDemo();

    public static native String getStatusText();

    public static native String getButtonText();

    public static native boolean showAdvanceButton();

    public static native boolean isSpatialAnchorsAccountSet();

    public static native void updateGeoLocationPermission(boolean isGranted);

    public static native void updateWifiPermission(boolean isGranted);

    public static native void updateBluetoothPermission(boolean isGranted);
}
