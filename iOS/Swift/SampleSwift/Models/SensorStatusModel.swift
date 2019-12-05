// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

import Foundation

protocol SensorStatusModel {
    var geoLocationStatus: SensorStatus { get }
    var wifiSignalStatus: SensorStatus { get }
    var bluetoothSignalStatus: SensorStatus { get }
}
