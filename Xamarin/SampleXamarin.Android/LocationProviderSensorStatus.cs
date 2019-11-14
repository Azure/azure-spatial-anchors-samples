// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Azure.SpatialAnchors;

namespace SampleXamarin
{
    public class LocationProviderSensorStatus : SensorStatusModel
    {
        private PlatformLocationProvider locationProvider;

        public LocationProviderSensorStatus(PlatformLocationProvider locationProvider)
        {
            this.locationProvider = locationProvider;
        }

        public SensorStatus GeoLocationStatus
        {
            get
            {
                if (locationProvider == null)
                {
                    return SensorStatus.Indeterminate;
                }
                if (!locationProvider.Sensors.GeoLocationEnabled)
                {
                    return SensorStatus.Blocked;
                }

                var geoLocationStatus = locationProvider.GeoLocationStatus;
                if (geoLocationStatus == GeoLocationStatusResult.Available)
                {
                    return SensorStatus.Available;
                }
                if (geoLocationStatus == GeoLocationStatusResult.DisabledCapability)
                {
                    return SensorStatus.Blocked;
                }
                if (geoLocationStatus == GeoLocationStatusResult.NoGPSData)
                {
                    return SensorStatus.Unavailable;
                }
                return SensorStatus.Indeterminate;
            }
        }

        public SensorStatus WifiSignalStatus
        {
            get
            {
                if (locationProvider == null)
                {
                    return SensorStatus.Indeterminate;
                }
                if (!locationProvider.Sensors.WifiEnabled)
                {
                    return SensorStatus.Blocked;
                }

                var wifiStatus = locationProvider.WifiStatus;
                if (wifiStatus == WifiStatusResult.Available)
                {
                    return SensorStatus.Available;
                }
                if (wifiStatus == WifiStatusResult.DisabledCapability)
                {
                    return SensorStatus.Blocked;
                }
                if (wifiStatus == WifiStatusResult.NoAccessPointsFound)
                {
                    return SensorStatus.Unavailable;
                }
                return SensorStatus.Indeterminate;
            }
        }

        public SensorStatus BluetoothSignalStatus
        {
            get
            {
                if (locationProvider == null)
                {
                    return SensorStatus.Indeterminate;
                }
                if (!locationProvider.Sensors.BluetoothEnabled)
                {
                    return SensorStatus.Blocked;
                }

                var bluetoothStatus = locationProvider.BluetoothStatus;
                if (bluetoothStatus == BluetoothStatusResult.Available)
                {
                    return SensorStatus.Available;
                }
                if (bluetoothStatus == BluetoothStatusResult.DisabledCapability)
                {
                    return SensorStatus.Blocked;
                }
                if (bluetoothStatus == BluetoothStatusResult.NoBeaconsFound)
                {
                    return SensorStatus.Unavailable;
                }
                return SensorStatus.Indeterminate;
            }
        }
    }
}
