// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace SampleXamarin
{
    public interface SensorStatusModel
    {
        SensorStatus GeoLocationStatus { get; }
        SensorStatus WifiSignalStatus { get; }
        SensorStatus BluetoothSignalStatus { get; }
    }
}