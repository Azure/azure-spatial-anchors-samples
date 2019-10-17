// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#import "BaseViewController.h"

// Set this to the account ID provided for the Azure Spatial Service resource.
NSString *const SpatialAnchorsAccountId = @"Set me";

// Set this to the account key provided for the Azure Spatial Service resource.
NSString *const SpatialAnchorsAccountKey = @"Set me";

@implementation AnchorVisual
@end

#pragma mark - Formatting Helpers

static NSString *FeedbackToString(ASASessionUserFeedback userFeedback) {
    NSString *result = @"";
    
    if (userFeedback == ASASessionUserFeedbackNotEnoughMotion) {
        result = [result stringByAppendingString:@"Not enough motion."];
    }
    else if (userFeedback == ASASessionUserFeedbackMotionTooQuick) {
        result = [result stringByAppendingString:@"Motion is too quick."];
    }
    else if (userFeedback == ASASessionUserFeedbackNotEnoughFeatures) {
        result = [result stringByAppendingString:@"Not enough features."];
    }
    else {
        result = @"Keep moving! 🤳";
    }
    return result;
}

static NSString *StatusToString(ASASessionStatus *status, DemoStep step) {
    NSString* feedback = FeedbackToString(status.userFeedback);
    
    if (step == DemoStepCreateCloudAnchor) {
        float progress = [status recommendedForCreateProgress];
        return [NSString stringWithFormat:@"%.0f%% progress. %@", progress * 100.f, feedback];
    }
    else {
        return [NSString stringWithFormat:@"%@", feedback];
    }
}

static NSString *MatrixToString(matrix_float4x4 value) {
    return [NSString stringWithFormat:@"[[%.2f %.2f %.2f %.2f] [%.2f %.2f %.2f %.2f] [%.2f %.2f %.2f %.2f] [%.2f %.2f %.2f %.2f]]",
            value.columns[0][0], value.columns[1][0], value.columns[2][0], value.columns[3][0],
            value.columns[0][1], value.columns[1][1], value.columns[2][1], value.columns[3][1],
            value.columns[0][2], value.columns[1][2], value.columns[2][2], value.columns[3][2],
            value.columns[0][3], value.columns[1][3], value.columns[2][3], value.columns[3][3]];
}

#pragma mark - ViewController
    
@implementation BaseViewController

- (void)dealloc {
    [self.view removeFromSuperview];
}

#pragma mark - Azure Spatial Anchors helper functions

-(void)startSession{
    _cloudSession = [[ASACloudSpatialAnchorSession alloc] init];
    _cloudSession.session = self.sceneView.session;
    _cloudSession.logLevel = ASASessionLogLevelAll;
    _cloudSession.delegate = self;
    _cloudSession.configuration.accountId = SpatialAnchorsAccountId;
    _cloudSession.configuration.accountKey = SpatialAnchorsAccountKey;
    [_cloudSession start];
    
    [_feedbackControl setHidden:NO];
    [_errorControl setHidden:YES];
    _enoughDataForSaving = NO;
}

- (void)createLocalAnchor:(simd_float4x4)anchorLocation {
    if (_localAnchor == NULL) {
        _localAnchor = [[ARAnchor alloc] initWithTransform:anchorLocation];
        [_sceneView.session addAnchor:_localAnchor];
        
        // Put the local anchor in the anchorVisuals dictionary with a key of ""
        AnchorVisual* visual = [[AnchorVisual alloc] init];
        visual.identifier = @"";
        visual.localAnchor = _localAnchor;
        [_anchorVisuals setValue:visual forKey:visual.identifier];
        
        [_button setTitle:@"Create Cloud Anchor (once at 100%)" forState:UIControlStateNormal];
    }
}

-(void)createCloudAnchor{
    if (_localAnchor == NULL) {
        return;
    }
    
    _cloudAnchor = [[ASACloudSpatialAnchor alloc] init];
    _cloudAnchor.localAnchor = _localAnchor;
    // In this sample app we delete the cloud anchor explicitly, but you can also set it to expire automatically
    int secondsInAWeek = 60 * 60 * 24 * 7;
    NSDate *oneWeekFromNow = [[NSDate alloc] initWithTimeIntervalSinceNow: (NSTimeInterval) secondsInAWeek];
    _cloudAnchor.expiration = oneWeekFromNow;
    
    [_cloudSession createAnchor:_cloudAnchor withCompletionHandler:^(NSError *error) {
        if (error) {
            [self->_button setTitle:@"Creation failed" forState:UIControlStateNormal];
            [self->_errorControl setHidden:NO];
            [self->_errorControl setTitle:error.localizedDescription forState:UIControlStateNormal];
            self->_localAnchorCube.firstMaterial.diffuse.contents = self->failedColor;
        } else {
            self->_saveCount++;
            self->_localAnchorCube.firstMaterial.diffuse.contents = self->savedColor;
            self->_targetId = self->_cloudAnchor.identifier;
            AnchorVisual* visual = self->_anchorVisuals[@""];
            visual.cloudAnchor = self->_cloudAnchor;
            visual.identifier = self->_cloudAnchor.identifier;
            [self->_anchorVisuals setValue:visual forKey:visual.identifier];
            [self->_anchorVisuals removeObjectForKey:@""];
            self->_localAnchor = NULL;
            
            [self moveToNextStepAfterCreateCloudAnchor];
        }
    }];
}

-(void)stopSession{
    if (_cloudSession){
        [_cloudSession stop];
        [_cloudSession dispose];
    }
    _cloudAnchor = NULL;
    _localAnchor = NULL;
    _cloudSession = NULL;
    
    for (AnchorVisual* visual in [_anchorVisuals allValues]) {
        [visual.node removeFromParentNode];
    }
    [_anchorVisuals removeAllObjects];
}

-(void)lookForAnchor{
    NSArray *ids = @[_targetId];
    ASAAnchorLocateCriteria *criteria = [ASAAnchorLocateCriteria new];
    criteria.identifiers = ids;
    [_cloudSession createWatcher:criteria];
    
    [_button setTitle:@"Locating Anchor ..." forState:UIControlStateNormal];
}

-(void)lookForNearbyAnchors{
    ASAAnchorLocateCriteria *criteria = [ASAAnchorLocateCriteria new];
    ASANearAnchorCriteria *nearCriteria = [ASANearAnchorCriteria new];
    [nearCriteria setDistanceInMeters:10];
    [nearCriteria setSourceAnchor:_anchorVisuals[_targetId].cloudAnchor];
    [criteria setNearAnchor:nearCriteria];
    [_cloudSession createWatcher:criteria];
    
    [_button setTitle:@"Locating nearby Anchors ..." forState:UIControlStateNormal];
}

-(void)deleteFoundAnchors {
    if ([_anchorVisuals count] == 0) {
        [_button setTitle:@"Anchor not found yet" forState:UIControlStateNormal];
        return;
    }
    
    [_button setTitle:@"Deleting found Anchor(s) ..." forState:UIControlStateNormal];
    
    for (AnchorVisual *visual in [_anchorVisuals allValues]) {
        if (visual.cloudAnchor == nil) {
            continue;
        }
        [_cloudSession deleteAnchor:visual.cloudAnchor withCompletionHandler:^(NSError *error) {
            self->_ignoreTaps = NO;
            self->_saveCount--;
            NSString *message;
            if (error) {
                [self->_errorControl setHidden:NO];
                message = @"Deletion failed";
                [self->_errorControl setTitle:error.localizedDescription forState:UIControlStateNormal];
                visual.node.geometry.firstMaterial.diffuse.contents = self->failedColor;
            } else {
                visual.node.geometry.firstMaterial.diffuse.contents = self->deletedColor;
            }
            
            if (self->_saveCount == 0) {
                message = [NSString stringWithFormat:@"Cloud Anchor(s) deleted. Tap to stop Session"];
                [self->_button setTitle:message forState:UIControlStateNormal];
                self->_step = DemoStepStopSession;
            }
        }];
    }
}

#pragma mark - ASACloudSpatialAnchorSessionDelegate

- (void)onLogDebug:(ASACloudSpatialAnchorSession *)cloudSpatialAnchorSession :(ASAOnLogDebugEventArgs *)args {
    NSLog(@"%@", [args message]);
}

//  // You can use this helper method to get an authentication token via Azure Active Directory.
//- (void)tokenRequired:(SSCCloudSpatialAnchorSession *)cloudSpatialAnchorSession :(ASATokenRequiredEventArgs *)args {
//    ASACloudSpatialAnchorSessionDeferral *deferral = [args getDeferral];
//    [AuthenticationHelper acquireAuthenticationToken:^(NSString *token, NSError *err) {
//        if (err)
//        {
//            [self->_errorControl setHidden:NO];
//            NSString *errMessage = err.localizedDescription;
//            [self->_errorControl setTitle:errMessage forState:UIControlStateNormal];
//        }
//        if (token) args.authenticationToken = token;
//        [deferral complete];
//    }];
//}

- (void)anchorLocated:(ASACloudSpatialAnchorSession *)cloudSpatialAnchorSession :(ASAAnchorLocatedEventArgs *)args {
    ASALocateAnchorStatus status = [args status];
    switch (status) {
        case ASALocateAnchorStatusAlreadyTracked:
            // Ignore if we were already handling this.
            NSLog(@"Cloud Anchor was located - we were already tracking it. Identifier: %@", [args anchor].identifier);
            break;
        case ASALocateAnchorStatusLocated: {
            ASACloudSpatialAnchor *anchor = [args anchor];
            NSLog(@"Cloud Anchor found! Identifier: %@. Location: %@", anchor.identifier, MatrixToString(anchor.localAnchor.transform));
            AnchorVisual* visual = [[AnchorVisual alloc] init];
            visual.cloudAnchor = anchor;
            visual.identifier = anchor.identifier;
            visual.localAnchor = anchor.localAnchor;
            [_anchorVisuals setValue:visual forKey:visual.identifier];
            [_sceneView.session addAnchor:anchor.localAnchor];
            break;
        }
        case ASALocateAnchorStatusNotLocatedAnchorDoesNotExist:
            break;
        case ASALocateAnchorStatusNotLocated:
            break;
    }
}

- (void)locateAnchorsCompleted:(ASACloudSpatialAnchorSession *)cloudSpatialAnchorSession :(ASALocateAnchorsCompletedEventArgs *)args {
    NSLog(@"Locate operation completed for watcher with identifier: %i", args.watcher.identifier);
    _ignoreTaps = NO;
    [self moveToNextStepAfterAnchorLocated];
}

- (void)sessionUpdated:(ASACloudSpatialAnchorSession *)cloudSpatialAnchorSession :(ASASessionUpdatedEventArgs *)args {
    ASASessionStatus *status = [args status];
    NSString *message = StatusToString(status, _step);
    _enoughDataForSaving = status.recommendedForCreateProgress >= 1.0f;
    [self showLogMessage:message here:_feedbackControl];
}

- (void)error:(ASACloudSpatialAnchorSession *)cloudSpatialAnchorSession :(ASASessionErrorEventArgs *)args {
    [_errorControl setHidden:NO];
    NSString *errorMessage = [args errorMessage];
    [self showLogMessage:errorMessage here:_errorControl];
}

#pragma mark - Handle taps to create an anchor

- (void)touchesBegan:(NSSet *)touches withEvent:(UIEvent *)event{
    [self.view endEditing:YES];
    [super touchesBegan:touches withEvent:event];
    
    if (!_currentlySavingAnchor) {
        return;
    }
    
    simd_float4x4 anchorLocation;
    CGPoint touchLocation = [[[event allTouches] anyObject] locationInView: _sceneView];
    NSArray<ARHitTestResult*> *hitResultsFeaturePoints = [_sceneView hitTest:touchLocation types:ARHitTestResultTypeFeaturePoint];
    ARHitTestResult *hit = [hitResultsFeaturePoints firstObject];
    ARFrame *currentFrame = [[_sceneView session] currentFrame];
    if (hit != nil) {
        // If we have a feature point create the local anchor there
        anchorLocation = [hit worldTransform];
    }
    else if (currentFrame != nil) {
        // Otherwise create the local anchor using the camera's current position
        simd_float4x4 translation = matrix_identity_float4x4;
        translation.columns[3].z = -0.5; // Put it 0.5 meters in front of the camera
        simd_float4x4 transform = simd_mul(currentFrame.camera.transform, translation);
        anchorLocation = transform;
    }
    else {
        [_button setTitle:@"Trouble placing anchor. Tap to try again" forState:UIControlStateNormal];
        return;
    }
    
    [self createLocalAnchor: anchorLocation];
}

#pragma mark - View Management

- (void)buttonTap:(UIButton *)sender {}

- (void)secondaryButtonTap:(UIButton *)sender {}

- (void)backButtonTap:(UIButton *)sender {
    [self moveToMainMenu];
}

-(void)moveToMainMenu {
    [self dismissViewControllerAnimated:NO completion:nil];
}

-(void)moveToNextStepAfterCreateCloudAnchor{}

-(void)moveToNextStepAfterAnchorLocated{}

-(void)showLogMessage:(NSString *)text here:(UIView *)here{
    dispatch_async(dispatch_get_main_queue(), ^{
        if ([here isKindOfClass:[UIButton class]]) {
            [(UIButton *)here setTitle:text forState:UIControlStateNormal];
        }
        else if ([here isKindOfClass:[UITextField class]]) {
            [(UITextField *)here setText:text];
        }
    });
}

- (UIButton *)addButtonAt:(float)top lines:(int)lines {
    float wideSize = _sceneView.bounds.size.width - 20;
    UIButton *result = [UIButton buttonWithType:UIButtonTypeSystem];
    result.frame = CGRectMake(10, top, wideSize, lines * 40);
    [result setTitleColor:UIColor.blackColor forState:UIControlStateNormal];
    [result setTitleShadowColor:UIColor.whiteColor forState:UIControlStateNormal];
    [result setBackgroundColor:[UIColor.lightGrayColor colorWithAlphaComponent:0.6]];
    if (lines > 1) {
        result.titleLabel.lineBreakMode = NSLineBreakByWordWrapping;
    }
    [self.sceneView addSubview:result];
    return result;
}

- (void)viewDidLoad {
    [super viewDidLoad];
    
    [self performSelector:@selector(setNeedsStatusBarAppearanceUpdate)];

    // Initialize ARSCNView
    self.sceneView = [[ARSCNView alloc] initWithFrame:self.view.bounds];
    
    // Set the view's session
    self.sceneView.session = [[ARSession alloc] init];
    
    // Set the view's delegate
    self.sceneView.delegate = self;
    
    // Show statistics such as fps and timing information
    self.sceneView.showsStatistics = NO;
    
    // Create a new scene
    SCNScene *scene = [SCNScene new];
    
    // Set the scene to the view
    self.sceneView.scene = scene;
    
    // Add sceneView to the view
    [self.view addSubview:self.sceneView];
    
    _anchorVisuals = [[NSMutableDictionary<NSString*, AnchorVisual *> alloc] init];

    readyColor = [UIColor.blueColor colorWithAlphaComponent:0.6];           // blue for a local anchor
    savedColor = [UIColor.greenColor colorWithAlphaComponent:0.6];          // green when the cloud anchor was saved successfully
    foundColor = [UIColor.yellowColor colorWithAlphaComponent:0.6];         // yellow when we successfully located a cloud anchor
    deletedColor = [UIColor.blackColor colorWithAlphaComponent:0.6];        // grey for a deleted cloud anchor
    failedColor = [UIColor.redColor colorWithAlphaComponent:0.6];           // red when there was an error
    
    // Control to indicate when we can create an anchor
    _feedbackControl  = [self addButtonAt:_sceneView.bounds.size.height - 40 lines:1];
    [_feedbackControl setBackgroundColor: [UIColor clearColor]];
    [_feedbackControl setTitleColor:[UIColor yellowColor] forState:UIControlStateNormal];
    [_feedbackControl setContentHorizontalAlignment:UIControlContentHorizontalAlignmentLeft];
    [_feedbackControl setHidden:YES];

    // Main button
    _button = [self addButtonAt:_sceneView.bounds.size.height - 80 lines:1];
    [_button addTarget:self action:@selector(buttonTap:) forControlEvents:UIControlEventTouchDown];
    
    // Control to show errors and verbose text
    _errorControl  = [self addButtonAt:_sceneView.bounds.size.height - 400 lines:5];
    [_errorControl setHidden:YES];
    
    // Control to go back to the menu screen
    _backControl = [self addButtonAt: 20 lines:1];
    [_backControl addTarget:self action:@selector(backButtonTap:) forControlEvents:UIControlEventTouchDown];
    [_backControl setBackgroundColor: [UIColor clearColor]];
    [_backControl setTitleColor:[UIColor blueColor] forState:UIControlStateNormal];
    [_backControl setContentHorizontalAlignment:UIControlContentHorizontalAlignmentLeft];
    [_backControl setTitle:@"Exit Demo" forState:UIControlStateNormal];
    
    if ([SpatialAnchorsAccountId isEqual: @"Set me"] || [SpatialAnchorsAccountKey isEqual: @"Set me"]) {
        [_button setHidden:YES];
        [_errorControl setHidden:NO];
        [self showLogMessage:@"Set SpatialAnchorsAccountId and SpatialAnchorsAccountKey in BaseViewController.m" here:_errorControl];
    } else {
        // Move to DemoStepPrepare to start all demos
        _step = DemoStepPrepare;
        [self buttonTap:nil];
    }
}

- (void)viewWillAppear:(BOOL)animated {
    [super viewWillAppear:animated];
    // Create a session configuration
    ARWorldTrackingConfiguration *configuration = [ARWorldTrackingConfiguration new];
    configuration.planeDetection = ARPlaneDetectionHorizontal;

    // Run the view's session
    self.sceneView.debugOptions = ARSCNDebugOptionShowFeaturePoints;
    [self.sceneView.session runWithConfiguration:configuration];
}


- (void)viewWillDisappear:(BOOL)animated {
    [super viewWillDisappear:animated];

    [self stopSession];
}

- (void)viewDidDisappear:(BOOL)animated {
    // Pause the view's session
    [self.sceneView.session pause];
}

- (void)didReceiveMemoryWarning {
    [super didReceiveMemoryWarning];
    // Release any cached data, images, etc that aren't in use.
}

#pragma mark - ARSCNViewDelegate

- (void)renderer:(id <SCNSceneRenderer>)renderer updateAtTime:(NSTimeInterval)time {
    // per-frame scenekit logic
    // modifications don't go through transaction model
    ASACloudSpatialAnchorSession *asession = _cloudSession;
    if (asession == NULL) {
        return;
    }
    [asession processFrame:_sceneView.session.currentFrame];
    
    if (_currentlySavingAnchor && _enoughDataForSaving && self->_anchorVisuals[@""] != NULL) {
        _currentlySavingAnchor = NO;
        dispatch_async(dispatch_get_main_queue(), ^{
            [self->_button setTitle:@"Cloud Anchor being saved..." forState:UIControlStateNormal];
        });
        
        [self createCloudAnchor];
    }
}

// Override to create and configure nodes for anchors added to the view's session.
- (SCNNode *)renderer:(id<SCNSceneRenderer>)renderer nodeForAnchor:(ARAnchor *)anchor {
    for (AnchorVisual* visual in [_anchorVisuals allValues]) {
        if (visual.localAnchor == anchor) {
            NSLog(@"renderer:nodeForAnchor with local Anchor %p at %@", anchor, MatrixToString(anchor.transform));
            SCNBox *cube = [SCNBox boxWithWidth:0.2f height:0.2f length:0.2f chamferRadius:0.0f];
            if ([visual.identifier length] > 0) {
                cube.firstMaterial.diffuse.contents = foundColor;
            } else {
                cube.firstMaterial.diffuse.contents = readyColor;
                 _localAnchorCube = cube;
            }
            SCNNode *node = [SCNNode nodeWithGeometry:cube];
            visual.node = node;
            return node;
        }
    }
    return nil;
}

- (void)session:(ARSession *)session didFailWithError:(NSError *)error {
    // Present an error message to the user

}

- (void)sessionWasInterrupted:(ARSession *)session {
    // Inform the user that the session has been interrupted, for example, by presenting an overlay

}

- (void)sessionInterruptionEnded:(ARSession *)session {
    // Reset tracking and/or remove existing anchors if consistent tracking is required
    if (_cloudSession) {
        [_cloudSession reset];
    }
}

@end
