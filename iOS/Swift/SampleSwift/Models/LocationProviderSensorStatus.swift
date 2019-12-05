// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

class LocationProviderSensorStatus: SensorStatusModel {
    private weak var locationProvider: ASAPlatformLocationProvider?

    var geoLocationStatus: SensorStatus {
        get {
            if let locationProvider = locationProvider {
                if !locationProvider.sensors.geoLocationEnabled {
                    return .Blocked
                }
                switch (locationProvider.geoLocationStatus){
                case ASAGeoLocationStatusResult.available :
                    return .Available
                case ASAGeoLocationStatusResult.disabledCapability:
                    return .Blocked
                case ASAGeoLocationStatusResult.missingSensorFingerprintProvider:
                    return .Indeterminate
                case ASAGeoLocationStatusResult.noGPSData:
                    return .Unavailable
                }

            }
            return .Indeterminate
        }
    }

    var wifiSignalStatus: SensorStatus {
        get {
            if let locationProvider = locationProvider {
                if !locationProvider.sensors.wifiEnabled {
                    return .Blocked
                }
                switch (locationProvider.wifiStatus){
                case ASAWifiStatusResult.available :
                    return .Available
                case ASAWifiStatusResult.disabledCapability:
                    return .Blocked
                case ASAWifiStatusResult.missingSensorFingerprintProvider:
                    return .Indeterminate
                case ASAWifiStatusResult.noAccessPointsFound:
                    return .Unavailable
                }
            }
            return .Indeterminate
        }
    }

    var bluetoothSignalStatus: SensorStatus {
        get {
            if let locationProvider = locationProvider {
                if !locationProvider.sensors.bluetoothEnabled {
                    return .Blocked
                }
                switch (locationProvider.bluetoothStatus){
                case ASABluetoothStatusResult.available :
                    return .Available
                case ASABluetoothStatusResult.disabledCapability:
                    return .Blocked
                case ASABluetoothStatusResult.missingSensorFingerprintProvider:
                    return .Indeterminate
                case ASABluetoothStatusResult.noBeaconsFound:
                    return .Unavailable
                }
            }
            return .Indeterminate
        }
    }

    init(for locationProvider: ASAPlatformLocationProvider?) {
        self.locationProvider = locationProvider
    }
}
