// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#import "SensorStatusView.h"
#import "SensorStatusModel.h"

@implementation SensorStatusView
{
    UIImageView IBOutlet *geoLocationStatusIcon;
    UIImageView IBOutlet *wifiStatusIcon;
    UIImageView IBOutlet *bluetoothStatusIcon;

    NSObject <SensorStatusModel> *model;
}

- (instancetype)initWithFrame:(CGRect)frame {
    self = [super initWithFrame: frame];
    if (self) {
        [self commonInit];
    }
    return self;
}

- (instancetype)initWithCoder:(NSCoder *)coder {
    self = [super initWithCoder:coder];
    if (self) {
        [self commonInit];
    }
    return self;
}

- (void)commonInit {
    Class ownType = [SensorStatusView class];
    NSBundle *bundle = [NSBundle bundleForClass:ownType];
    UINib *nib = [[bundle loadNibNamed:NSStringFromClass(ownType) owner:self options:nil] firstObject];
    UIView *contentView = (UIView *)nib;
    CGRect adjustedFrame = self.frame;
    adjustedFrame.size = contentView.frame.size;
    [self setFrame: adjustedFrame];
    [self addSubview:contentView];

}

- (void)setModel:(NSObject<SensorStatusModel> *)model {
    self->model = model;
}

- (void)update {
    const enum SensorStatus geoLocationStatus =
        model == nil ? SensorStatusIndeterminate : model.geoLocationStatus;
    const enum SensorStatus wifiSignalStatus =
        model == nil ? SensorStatusIndeterminate : model.wifiSignalStatus;
    const enum SensorStatus bluetoothSignalStatus =
        model == nil ? SensorStatusIndeterminate : model.bluetoothSignalStatus;

    geoLocationStatusIcon.image = [self getIconForStatus:geoLocationStatus];
    wifiStatusIcon.image = [self getIconForStatus:wifiSignalStatus];
    bluetoothStatusIcon.image = [self getIconForStatus:bluetoothSignalStatus];
}

- (UIImage*)getIconForStatus:(enum SensorStatus)status {
    switch (status) {
        case SensorStatusIndeterminate:
            return [UIImage imageNamed:@"gray-circle"];
        case SensorStatusBlocked:
            return [UIImage imageNamed:@"red-circle"];
        case SensorStatusUnavailable:
            return [UIImage imageNamed:@"orange-circle"];
        case SensorStatusAvailable:
            return [UIImage imageNamed:@"green-circle"];
    }
}

@end
