// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using UnityEngine;
using UnityEngine.Android;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    public class SensorPermissionHelper
    {
        /// <summary>
        /// iOS only: Whether the "Access WiFi Information" entitlement should be injected.
        /// If available, the MAC address of the connected Wi-Fi access point can be used
        /// to help find nearby anchors.
        /// </summary>
        /// <remarks>
        /// This requires a paid Apple Developer account.
        /// </remarks>
        public static readonly bool HaveAccessWifiInformationEntitlement = false;

        const string androidWifiAccessPermission = "android.permission.ACCESS_WIFI_STATE";
        const string androidWifiChangePermission = "android.permission.CHANGE_WIFI_STATE";
        const string androidBluetoothPermission = "android.permission.BLUETOOTH";
        const string androidBluetoothAdminPermission = "android.permission.BLUETOOTH_ADMIN";

        public static void RequestSensorPermissions()
        {
#if UNITY_ANDROID
            RequestPermissionIfNotGiven(Permission.FineLocation);
            // Fine location implies coarse location

            RequestPermissionIfNotGiven(androidWifiAccessPermission);
            RequestPermissionIfNotGiven(androidWifiChangePermission);
            RequestPermissionIfNotGiven(androidBluetoothAdminPermission);
            RequestPermissionIfNotGiven(androidBluetoothPermission);
#endif
        }

        public static bool HasGeoLocationPermission()
        {
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(Permission.FineLocation) || Permission.HasUserAuthorizedPermission(Permission.CoarseLocation);
#else
            return true;
#endif
        }

        public static bool HasWifiPermission()
        {
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(Permission.CoarseLocation) &&
                Permission.HasUserAuthorizedPermission(androidWifiAccessPermission) &&
                Permission.HasUserAuthorizedPermission(androidWifiChangePermission) &&
                IsWiFiEnabled();
#elif UNITY_IOS
            return HaveAccessWifiInformationEntitlement;
#else
            return true;
#endif
        }

        public static bool HasBluetoothPermission()
        {
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(Permission.CoarseLocation) &&
                Permission.HasUserAuthorizedPermission(androidBluetoothPermission) &&
                Permission.HasUserAuthorizedPermission(androidBluetoothAdminPermission) &&
                IsBluetoothEnabled();
#else
            return true;
#endif
        }

        private static void RequestPermissionIfNotGiven(string permission)
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(permission))
            {
                Permission.RequestUserPermission(permission);
            }
#endif
        }

        private static bool IsBluetoothEnabled()
        {
        #if UNITY_ANDROID
            using (AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
            {
                try
                {
                    Debug.Log("Requesting android bluetooth system service");
                    using (var BluetoothManager = activity.Call<AndroidJavaObject>("getSystemService", "bluetooth"))
                    {
                        var BluetoothAdapter = BluetoothManager.Call<AndroidJavaObject>("getAdapter");
                        if (BluetoothAdapter == null)
                        {
                            return false;
                        }

                        return BluetoothAdapter.Call<bool>("isEnabled");
                    }
                }
                catch (Exception)
                {
                    Debug.LogError("Failed to query Bluetooth sensor state");
                }
            }
        #endif
            return false;
        }

        private static bool IsWiFiEnabled()
        {
        #if UNITY_ANDROID
            using (AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
            {
                try
                {
                    Debug.Log("Requesting android WiFi system service");
                    using (var WifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi"))
                    {
                        if(WifiManager == null)
                        {
                            return false;
                        }

                        return WifiManager.Call<bool>("isWifiEnabled");
                    }
                }
                catch (Exception)
                {
                    Debug.LogError("Failed to query Wifi sensor state");
                }
            }
        #endif
            return false;
        }
    }
}
