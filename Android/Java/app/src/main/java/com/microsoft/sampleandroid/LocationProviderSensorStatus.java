// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import com.microsoft.azure.spatialanchors.PlatformLocationProvider;

public class LocationProviderSensorStatus implements SensorStatusModel {
    private PlatformLocationProvider locationProvider;

    public LocationProviderSensorStatus(PlatformLocationProvider locationProvider) {
        this.locationProvider = locationProvider;
    }

    @Override
    public SensorStatus getGeoLocationStatus() {
        if (locationProvider == null) {
            return SensorStatus.Indeterminate;
        }
        if (!locationProvider.getSensors().getGeoLocationEnabled()) {
            return SensorStatus.Blocked;
        }

        switch (locationProvider.getGeoLocationStatus())
        {
            case Available:
                return SensorStatus.Available;
            case NoGPSData:
                return SensorStatus.Unavailable;
            case DisabledCapability:
                return SensorStatus.Blocked;
            case MissingSensorFingerprintProvider:
                return SensorStatus.Indeterminate;
        }

        return SensorStatus.Indeterminate;
    }

    @Override
    public SensorStatus getWifiSignalStatus() {
        if (locationProvider == null) {
            return SensorStatus.Indeterminate;
        }
        if (!locationProvider.getSensors().getWifiEnabled()) {
            return SensorStatus.Blocked;
        }

        switch (locationProvider.getWifiStatus())
        {
            case Available:
                return SensorStatus.Available;
            case NoAccessPointsFound:
                return SensorStatus.Unavailable;
            case DisabledCapability:
                return SensorStatus.Blocked;
            case MissingSensorFingerprintProvider:
                return SensorStatus.Indeterminate;
        }

        return SensorStatus.Indeterminate;
    }

    @Override
    public SensorStatus getBluetoothSignalStatus() {
        if (locationProvider == null) {
            return SensorStatus.Indeterminate;
        }
        if (!locationProvider.getSensors().getBluetoothEnabled()) {
            return SensorStatus.Blocked;
        }

        switch (locationProvider.getBluetoothStatus())
        {
            case Available:
                return SensorStatus.Available;
            case NoBeaconsFound:
                return SensorStatus.Unavailable;
            case DisabledCapability:
                return SensorStatus.Blocked;
            case MissingSensorFingerprintProvider:
                return SensorStatus.Indeterminate;
        }

        return SensorStatus.Indeterminate;
    }
}
