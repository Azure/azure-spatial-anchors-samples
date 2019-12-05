// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#import <UIKit/UIKit.h>
#import "Models/SensorStatusModel.h"

@interface SensorStatusView : UIView
- (void)setModel:(NSObject <SensorStatusModel> *)model;
- (void)update;
@end
