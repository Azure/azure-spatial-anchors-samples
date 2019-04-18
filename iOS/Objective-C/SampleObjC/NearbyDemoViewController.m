// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#import "NearbyDemoViewController.h"

// This demo creates and saves three anchors. It then locates one with its identifier.
// It then looks for anchors near the found anchor (to find the other two).

uint const NumberOfNearbyAnchors = 3; ///< the number of anchors we will create in the nearby demo

@implementation NearbyDemoViewController

-(void)moveToNextStepAfterCreateCloudAnchor{
    if (_saveCount < NumberOfNearbyAnchors) {
        [_button setTitle:@"Tap on the screen to create the next Anchor ☝️" forState:UIControlStateNormal];
        _currentlySavingAnchor = YES;
    } else {
        _ignoreTaps = NO;
        [_feedbackControl setHidden:YES];
        [_button setTitle:@"Tap to start next Session & look for Anchor" forState:UIControlStateNormal];
        _step = DemoStepLookForAnchor;
    }
}

-(void)moveToNextStepAfterAnchorLocated{
    if (_step == DemoStepLookForAnchor) {
        [_button setTitle:@"Anchor found! Tap to locate nearby" forState:UIControlStateNormal];
        _step = DemoStepLookForNearbyAnchors;
    }
    else {
        [_feedbackControl setHidden:YES];
        [_button setTitle:@"Anchors found! Tap to delete" forState:UIControlStateNormal];
        _step = DemoStepDeleteFoundAnchors;
    }
}

- (void)buttonTap:(UIButton *)sender {
    if (_ignoreTaps) {
        return;
    }
    switch (_step) {
        case DemoStepPrepare:
            [_button setTitle:@"Tap to start Session" forState:UIControlStateNormal];
            _step = DemoStepCreateCloudAnchor;
            break;
        case DemoStepCreateCloudAnchor:
            _ignoreTaps = YES;
            _currentlySavingAnchor = YES;
            _saveCount = 0;
            
            [self startSession];
            
            // When you tap on the screen, touchesBegan will call createLocalAnchor and create a local ARAnchor
            // We will then put that anchor in the anchorVisuals dictionary with a key of "" and call CreateCloudAnchor when there is enough data for saving
            // CreateCloudAnchor will call moveToNextStepAfterCreateCloudAnchor when its async method returns
            [_button setTitle:@"Tap on the screen to create an Anchor ☝️" forState:UIControlStateNormal];
            break;
        case DemoStepLookForAnchor:
            _ignoreTaps = YES;
            [self stopSession];
            [self startSession];
            
            // We will get a call to locateAnchorsCompleted when locate operation completes, which will call moveToNextStepAfterAnchorLocated
            [self lookForAnchor];
            break;
        case DemoStepLookForNearbyAnchors:
            if ([_anchorVisuals count] == 0) {
                [_button setTitle:@"First Anchor not found yet" forState:UIControlStateNormal];
                return;
            }
            _ignoreTaps = YES;
            
            // We will get a call to locateAnchorsCompleted when locate operation completes, which will call moveToNextStepAfterAnchorLocated
            [self lookForNearbyAnchors];
            break;
        case DemoStepDeleteFoundAnchors:
            _ignoreTaps = YES;
            
            // DeleteFoundAnchors will move to the next step when its async method returns
            [self deleteFoundAnchors];
            break;
        case DemoStepStopSession:
            [self stopSession];
            [self moveToMainMenu];
            return;
        default:
            assert(false);
            _step = 0;
            return;
    }
}
@end
