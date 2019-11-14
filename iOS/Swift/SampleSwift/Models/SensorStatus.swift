// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

enum SensorStatus {
    /// The sensor's status is unknown
    case Indeterminate

    /// Access has not been granted by the user
    case Blocked

    /// The sensor is active but has not provided any observations
    case Unavailable

    /// The sensor is providing observations
    case Available
}
