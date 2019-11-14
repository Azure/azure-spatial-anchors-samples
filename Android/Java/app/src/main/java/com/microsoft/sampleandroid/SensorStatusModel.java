// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

public interface SensorStatusModel {
    SensorStatus getGeoLocationStatus();
    SensorStatus getWifiSignalStatus();
    SensorStatus getBluetoothSignalStatus();
}
