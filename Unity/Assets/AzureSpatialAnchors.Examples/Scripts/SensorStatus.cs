// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

public enum SensorStatus
{
    /// <summary>
    /// GeoLocation data is available.
    /// </summary>
    Available,
    /// <summary>
    /// GeoLocation was disabled in the SensorCapabilities.
    /// </summary>
    DisabledCapability,
    /// <summary>
    /// No sensor fingerprint provider has been created.
    /// </summary>
    MissingSensorFingerprintProvider,
    /// <summary>
    /// No GPS data has been received.
    /// </summary>
    NoData
}