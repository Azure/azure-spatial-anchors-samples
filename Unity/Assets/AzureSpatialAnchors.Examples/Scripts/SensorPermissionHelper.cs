// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
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

        public static void RequestSensorPermissions()
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Permission.RequestUserPermission(Permission.FineLocation);
            }
            // Fine location implies coarse location
#endif
        }

        public static bool HasGeoLocationPermission()
        {
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(Permission.FineLocation);
#else
            return true;
#endif
        }

        public static bool HasWifiPermission()
        {
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(Permission.CoarseLocation);
#elif UNITY_IOS
            return HaveAccessWifiInformationEntitlement;
#else
            return true;
#endif
        }

        public static bool HasBluetoothPermission()
        {
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(Permission.CoarseLocation);
#else
            return true;
#endif
        }
    }
}
