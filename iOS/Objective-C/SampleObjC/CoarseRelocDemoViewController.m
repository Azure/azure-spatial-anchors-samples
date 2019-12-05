// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#import "CoarseRelocDemoViewController.h"
#import "Views/SensorStatusView.h"
#import "Models/LocationProviderSensorStatus.h"

/// Whether the "Access WiFi Information" capability is enabled.
/// If available, the MAC address of the connected Wi-Fi access point can be used
/// to help find nearby anchors.
/// Note: This entitlement requires a paid Apple Developer account.
const bool haveAccessWifiInformationEntitlement = false;

// Whitelist of Bluetooth-LE beacons used to find anchors and improve the locatability
// of existing anchors.
// Add the UUIDs for your own Bluetooth beacons here to use them with Azure Spatial Anchors.
#define KNOWN_BLUETOOTH_PROXIMITY_UUIDS @[ \
          @"61687109-905f-4436-91f8-e602f514c96d", \
          @"e1f54e02-1e23-44e0-9c3d-512eb56adec9", \
          @"01234567-8901-2345-6789-012345678903", \
      ]

// This demo creates and saves an anchor. It then locates all anchors near the device based on sensor data.
@implementation CoarseRelocDemoViewController
{
    IBOutlet SensorStatusView *sensorStatusView;
    ASAPlatformLocationProvider *locationProvider;
    ASACloudSpatialAnchorWatcher *nearDeviceWatcher;
    int numAnchorsFound;
}

-(void)onCloudAnchorCreated {
    _ignoreTaps = NO;
    [_feedbackControl setHidden:YES];
    dispatch_async(dispatch_get_main_queue(), ^(void){
        [self->sensorStatusView update];
    });
    [_button setTitle:@"Tap to start next Session & look for anchors near device" forState:UIControlStateNormal];
    _step = DemoStepLookForNearbyAnchors;
}

-(void)onNewAnchorLocated:(ASACloudSpatialAnchor*)cloudAnchor {
    _ignoreTaps = NO;
    [_feedbackControl setHidden:YES];
    NSString *message = [NSString stringWithFormat:@"%d anchor(s) found. Tap to stop watcher", ++self->numAnchorsFound];
    [_button setTitle:message forState:UIControlStateNormal];
    _step = DemoStepStopWatcher;
}

- (void)renderer:(id<SCNSceneRenderer>)renderer updateAtTime:(NSTimeInterval)time {
    [super renderer:renderer updateAtTime:time];
    dispatch_async(dispatch_get_main_queue(), ^(void){
        if (self->sensorStatusView != nil) {
            [self->sensorStatusView update];
        }
    });
}

- (void)buttonTap:(UIButton *)sender {
    if (_ignoreTaps){
        return;
    }
    switch (_step) {
        case DemoStepPrepare:
            [_button setTitle:@"Tap to start Session" forState:UIControlStateNormal];
            _step = DemoStepCreateCloudAnchor;
            [self createLocationProvider];
            break;
        case DemoStepCreateCloudAnchor:
            _ignoreTaps = YES;
            _currentlySavingAnchor = YES;
            _saveCount = 0;

            [self startSession];
            [self attachLocationProviderToSession];

            // When you tap on the screen, touchesBegan will call createLocalAnchor and create a local ARAnchor.
            // We will then put that anchor in the anchorVisuals dictionary with a key of "" and call CreateCloudAnchor when there is enough data for saving.
            // CreateCloudAnchor will call onCloudAnchorCreated when its async method returns to move to the next step.
            [_button setTitle:@"Tap on the screen to create an Anchor ☝️" forState:UIControlStateNormal];
            break;
        case DemoStepLookForNearbyAnchors:
            _ignoreTaps = YES;
            [self stopSession];
            [self startSession];
            [self attachLocationProviderToSession];

            // We will get a call to onLocateAnchorsCompleted which will move to the next step when the locate operation completes.
            [self lookForAnchorsNearDevice];
            break;
        case DemoStepStopWatcher:
            _step = DemoStepStopSession;
            [nearDeviceWatcher stop];
            nearDeviceWatcher = nil;
            [_button setTitle: @"Tap to stop Session and return to the main menu" forState:UIControlStateNormal];
            break;
        case DemoStepStopSession:
            [self stopSession];
            locationProvider = nil;
            [sensorStatusView setModel:nil];
            [self moveToMainMenu];
            return;
        default:
            assert(false);
            _step = 0;
            return;
    }
}

- (void)createLocationProvider {
    locationProvider = [[ASAPlatformLocationProvider alloc] init];

    // Register known Bluetooth beacons
    locationProvider.sensors.knownBeaconProximityUuids = KNOWN_BLUETOOTH_PROXIMITY_UUIDS;

    // Display the sensor status
    NSObject <SensorStatusModel> *sensorStatus = [[LocationProviderSensorStatus alloc] initForLocationProvider:locationProvider];
    [sensorStatusView setModel:sensorStatus];

    [self enableAllowedSensors];
}

- (void)enableAllowedSensors {
    if (locationProvider != nil) {
        ASASensorCapabilities *sensors = locationProvider.sensors;
        sensors.bluetoothEnabled = true;
        sensors.wifiEnabled = haveAccessWifiInformationEntitlement;
        sensors.geoLocationEnabled = true;
    }
}

- (void)attachLocationProviderToSession {
    _cloudSession.locationProvider = locationProvider;
}

- (void)lookForAnchorsNearDevice {
    ASANearDeviceCriteria *nearDevice = [[ASANearDeviceCriteria alloc] init];
    nearDevice.distanceInMeters = 8.0f;
    nearDevice.maxResultCount = 25;

    ASAAnchorLocateCriteria *criteria = [[ASAAnchorLocateCriteria alloc] init];
    criteria.nearDevice = nearDevice;
    nearDeviceWatcher = [_cloudSession createWatcher:criteria];

    [_button setTitle: @"Looking for anchors near device" forState:UIControlStateNormal];
}

@end
