// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Android;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.Support.V4.App;
using Microsoft.Azure.SpatialAnchors;

namespace SampleXamarin
{
    public static class SensorPermissionsHelper
    {
        public enum PermissionsResult
        {
            Indeterminate,
            Allowed,
            Denied
        }

        private static readonly int RequestCodeAllSensors = 1;

        public static bool RequestMissingPermissions(Activity activity)
        {
            if (HasAllPermissionGranted(activity))
            {
                return true;
            }

            activity.RequestPermissions(
                    new string[]{
                        Manifest.Permission.AccessFineLocation,
                        Manifest.Permission.AccessWifiState,
                        Manifest.Permission.ChangeWifiState,
                        Manifest.Permission.Bluetooth,
                        Manifest.Permission.BluetoothAdmin},
                    RequestCodeAllSensors);

            return HasAllRequiredPermissionGranted(activity);
        }

        public static PermissionsResult OnRequestPermissionsResult(Context context, int requestCode)
        {
            if (requestCode != RequestCodeAllSensors)
            {
                return PermissionsResult.Indeterminate;
            }

            if (!HasAllRequiredPermissionGranted(context))
            {
                return PermissionsResult.Denied;
            }

            return PermissionsResult.Allowed;
        }

        public static void EnableAllowedSensors(Context context, PlatformLocationProvider locationProvider)
        {
            // Retrieve permissions granted by the user
            bool hasFineLocationPermission = HasPermission(context, Manifest.Permission.AccessFineLocation);
            bool hasAccessCoarseLocationPermission = HasPermission(context, Manifest.Permission.AccessCoarseLocation);
            bool hasAnyLocationPermission = hasFineLocationPermission || hasAccessCoarseLocationPermission;
            bool hasAccessWifiStatePermission = HasPermission(context, Manifest.Permission.AccessWifiState);
            bool hasChangeWifiStatePermission = HasPermission(context, Manifest.Permission.ChangeWifiState);
            bool hasBluetoothPermission = HasPermission(context, Manifest.Permission.Bluetooth);
            bool hasBluetoothAdminPermission = HasPermission(context, Manifest.Permission.BluetoothAdmin);

            // Try to turn on Wi-Fi, if allowed
            bool isWifiAllowed = hasAnyLocationPermission
                    && hasAccessWifiStatePermission
                    && hasChangeWifiStatePermission;
            bool isWifiOn = isWifiAllowed && TryTurnOnWifi(context);

            // Try to turn on Bluetooth, if allowed
            bool isBluetoothAllowed = hasAnyLocationPermission
                    && hasBluetoothPermission
                    && hasBluetoothAdminPermission;
            bool isBluetoothOn = isBluetoothAllowed && TryTurnOnBluetooth();

            // Configure location provider to use the allowed sensors
            var sensors = locationProvider.Sensors;
            sensors.GeoLocationEnabled = hasAnyLocationPermission;
            sensors.WifiEnabled = isWifiOn;
            sensors.BluetoothEnabled = isBluetoothOn;
        }

        private static bool HasAllRequiredPermissionGranted(Context context)
        {
            return HasPermission(context, Manifest.Permission.AccessFineLocation);
        }

        private static bool HasAllPermissionGranted(Context context)
        {
            return HasAllRequiredPermissionGranted(context)
                    && HasPermission(context, Manifest.Permission.AccessWifiState)
                    && HasPermission(context, Manifest.Permission.ChangeWifiState)
                    && HasPermission(context, Manifest.Permission.Bluetooth)
                    && HasPermission(context, Manifest.Permission.BluetoothAdmin);
        }

        private static bool HasPermission(Context context, string manifestPermission)
        {
            return ActivityCompat.CheckSelfPermission(context, manifestPermission) == Permission.Granted;
        }

        private static bool TryTurnOnWifi(Context context)
        {
            WifiManager wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);
            if (wifiManager == null)
            {
                return false;
            }
            if (!wifiManager.IsWifiEnabled)
            {
                wifiManager.SetWifiEnabled(true);
            }
            return wifiManager.IsWifiEnabled;
        }

        private static bool TryTurnOnBluetooth()
        {
            BluetoothAdapter adapter = BluetoothAdapter.DefaultAdapter;
            if (adapter == null)
            {
                return false;
            }
            if (!adapter.IsEnabled)
            {
                adapter.Enable();
            }
            return adapter.IsEnabled;
        }
    }
}