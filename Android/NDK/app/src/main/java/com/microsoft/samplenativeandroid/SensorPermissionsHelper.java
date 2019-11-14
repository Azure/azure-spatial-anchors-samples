// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

package com.microsoft.samplenativeandroid;

import android.Manifest;
import android.annotation.SuppressLint;
import android.app.Activity;
import android.bluetooth.BluetoothAdapter;
import android.content.Context;
import android.content.pm.PackageManager;
import android.net.wifi.WifiManager;
import android.support.v4.app.ActivityCompat;

public class SensorPermissionsHelper {

    enum PermissionsResult {
        Indeterminate,
        Allowed,
        Denied,
    }

    private static final int REQUEST_CODE_ALL_SENSORS = 1;

    public static boolean requestMissingPermissions(Activity activity) {
        updateSensorPermissions(activity);

        if (hasAllPermissionGranted(activity)) {
            return true;
        }

        activity.requestPermissions(
                new String[]{
                        Manifest.permission.ACCESS_FINE_LOCATION,
                        Manifest.permission.ACCESS_WIFI_STATE,
                        Manifest.permission.CHANGE_WIFI_STATE,
                        Manifest.permission.BLUETOOTH,
                        Manifest.permission.BLUETOOTH_ADMIN},
                REQUEST_CODE_ALL_SENSORS);

        return hasAllRequiredPermissionGranted(activity);
    }

    public static PermissionsResult onRequestPermissionsResult(Context context, int requestCode) {
        if (requestCode != REQUEST_CODE_ALL_SENSORS) {
            return PermissionsResult.Indeterminate;
        }

        updateSensorPermissions(context);

        if (!hasAllRequiredPermissionGranted(context)) {
            return PermissionsResult.Denied;
        }

        return PermissionsResult.Allowed;
    }

    private static void updateSensorPermissions(Context context) {
        // Retrieve permissions granted by the user
        boolean hasFineLocationPermission = hasPermission(context, Manifest.permission.ACCESS_FINE_LOCATION);
        boolean hasAccessCoarseLocationPermission = hasPermission(context, Manifest.permission.ACCESS_COARSE_LOCATION);
        boolean hasAnyLocationPermission = hasFineLocationPermission || hasAccessCoarseLocationPermission;
        boolean hasAccessWifiStatePermission = hasPermission(context, Manifest.permission.ACCESS_WIFI_STATE);
        boolean hasChangeWifiStatePermission = hasPermission(context, Manifest.permission.CHANGE_WIFI_STATE);
        boolean hasBluetoothPermission = hasPermission(context, Manifest.permission.BLUETOOTH);
        boolean hasBluetoothAdminPermission = hasPermission(context, Manifest.permission.BLUETOOTH_ADMIN);

        // Try to turn on Wi-Fi, if allowed
        boolean isWifiAllowed = hasAnyLocationPermission
                && hasAccessWifiStatePermission
                && hasChangeWifiStatePermission;
        boolean isWifiOn = isWifiAllowed && tryTurnOnWifi(context);

        // Try to turn on Bluetooth, if allowed
        boolean isBluetoothAllowed = hasAnyLocationPermission
                && hasBluetoothPermission
                && hasBluetoothAdminPermission;
        boolean isBluetoothOn = isBluetoothAllowed && tryTurnOnBluetooth();

        // Inform the native application of any permission changes
        JniInterface.updateGeoLocationPermission(hasAnyLocationPermission);
        JniInterface.updateWifiPermission(isWifiOn);
        JniInterface.updateBluetoothPermission(isBluetoothOn);
    }

    private static boolean hasAllRequiredPermissionGranted(Context context) {
        return hasPermission(context, Manifest.permission.ACCESS_FINE_LOCATION);
    }

    private static boolean hasAllPermissionGranted(Context context) {
        return hasAllRequiredPermissionGranted(context)
                && hasPermission(context, Manifest.permission.ACCESS_WIFI_STATE)
                && hasPermission(context, Manifest.permission.CHANGE_WIFI_STATE)
                && hasPermission(context, Manifest.permission.BLUETOOTH)
                && hasPermission(context, Manifest.permission.BLUETOOTH_ADMIN);
    }

    private static boolean hasPermission(Context context, String manifestPermission) {
        return ActivityCompat.checkSelfPermission(context, manifestPermission) == PackageManager.PERMISSION_GRANTED;
    }

    private static boolean tryTurnOnWifi(Context context) {
        WifiManager wifiManager = (WifiManager)context.getSystemService(Context.WIFI_SERVICE);
        if (wifiManager == null) {
            return false;
        }
        if (!wifiManager.isWifiEnabled()) {
            wifiManager.setWifiEnabled(true);
        }
        return wifiManager.isWifiEnabled();
    }

    @SuppressLint("MissingPermission")
    private static boolean tryTurnOnBluetooth() {
        BluetoothAdapter adapter = BluetoothAdapter.getDefaultAdapter();
        if (adapter == null) {
            return false;
        }
        if (!adapter.isEnabled()) {
            adapter.enable();
        }
        return adapter.isEnabled();
    }
}
