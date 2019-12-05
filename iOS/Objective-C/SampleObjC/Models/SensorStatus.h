// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

typedef NS_ENUM(NSInteger, SensorStatus) {
    /// The sensor's status is unknown
    SensorStatusIndeterminate,

    /// Access has not been granted by the user
    SensorStatusBlocked,

    /// The sensor is active but has not provided any observations
    SensorStatusUnavailable,

    /// The sensor is providing observations
    SensorStatusAvailable
};
