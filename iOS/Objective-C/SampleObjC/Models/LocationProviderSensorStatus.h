// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#import <AzureSpatialAnchors/AzureSpatialAnchors.h>
#import "SensorStatusModel.h"

@interface LocationProviderSensorStatus: NSObject <SensorStatusModel>
- (id)initForLocationProvider:(ASAPlatformLocationProvider *)locationProvider;
@end
