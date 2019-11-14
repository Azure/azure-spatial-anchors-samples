// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

public enum SensorStatus {
    /// The sensor's status is unknown
    Indeterminate,

    /// Access has not been granted by the user
    Blocked,

    /// The sensor is active but has not provided any observations
    Unavailable,

    /// The sensor is providing observations
    Available
}