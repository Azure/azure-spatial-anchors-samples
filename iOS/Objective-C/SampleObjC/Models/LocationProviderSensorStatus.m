// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#import "LocationProviderSensorStatus.h"

@implementation LocationProviderSensorStatus
{
    ASAPlatformLocationProvider *locationProvider;
}

- (enum SensorStatus)geoLocationStatus {
    if (locationProvider == nil) {
        return SensorStatusIndeterminate;
    }

    if (!locationProvider.sensors.geoLocationEnabled) {
        return SensorStatusBlocked;
    }

    switch (locationProvider.geoLocationStatus)
    {
        case ASAGeoLocationStatusResultAvailable:
            return SensorStatusAvailable;
        case ASAGeoLocationStatusResultDisabledCapability:
            return SensorStatusBlocked;
        case ASAGeoLocationStatusResultMissingSensorFingerprintProvider:
            return SensorStatusIndeterminate;
        case ASAGeoLocationStatusResultNoGPSData:
            return SensorStatusUnavailable;
    }

    return SensorStatusIndeterminate;
}
- (enum SensorStatus)wifiSignalStatus {
    if (locationProvider == nil) {
        return SensorStatusIndeterminate;
    }

    if (!locationProvider.sensors.wifiEnabled) {
        return SensorStatusBlocked;
    }

    switch (locationProvider.wifiStatus)
    {
        case ASAWifiStatusResultAvailable:
            return SensorStatusAvailable;
        case ASAWifiStatusResultDisabledCapability:
            return SensorStatusBlocked;
        case ASAWifiStatusResultMissingSensorFingerprintProvider:
            return SensorStatusIndeterminate;
        case ASAWifiStatusResultNoAccessPointsFound:
            return SensorStatusUnavailable;
    }

    return SensorStatusIndeterminate;
}
- (enum SensorStatus)bluetoothSignalStatus {
    if (locationProvider == nil) {
        return SensorStatusIndeterminate;
    }

    if (!locationProvider.sensors.bluetoothEnabled) {
        return SensorStatusBlocked;
    }

    switch (locationProvider.bluetoothStatus)
    {
        case ASABluetoothStatusResultAvailable:
            return SensorStatusAvailable;
        case ASABluetoothStatusResultDisabledCapability:
            return SensorStatusBlocked;
        case ASABluetoothStatusResultMissingSensorFingerprintProvider:
            return SensorStatusIndeterminate;
        case ASABluetoothStatusResultNoBeaconsFound:
            return SensorStatusUnavailable;
    }

    return SensorStatusIndeterminate;
}

- (id)initForLocationProvider:(ASAPlatformLocationProvider*)locationProvider {
    self = [super init];
    if (self != nil) {
        self->locationProvider = locationProvider;
    }
    return self;
}

@end
