// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#import "SensorStatus.h"

@protocol SensorStatusModel <NSObject>
@property (readonly) enum SensorStatus geoLocationStatus;
@property (readonly) enum SensorStatus wifiSignalStatus;
@property (readonly) enum SensorStatus bluetoothSignalStatus;
@end
