// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#import "ViewController.h"
#import "AuthenticationHelper.h"
#import <AzureSpatialAnchors/AzureSpatialAnchors.h>

// Set this to the account ID provided for the Azure Spatial Service resource.
NSString *const SpatialAnchorsAccountId = @"Set me";

// Set this to the account key provided for the Azure Spatial Service resource.
NSString *const SpatialAnchorsAccountKey = @"Set me";

@interface ViewController () <ARSCNViewDelegate, SCNSceneRendererDelegate, ASACloudSpatialAnchorSessionDelegate, UITextFieldDelegate>

@property (nonatomic, strong) IBOutlet ARSCNView *sceneView;
@property (atomic) NSString *lastFeedbackMessage;

@end

typedef enum DemoStep_ {
    DemoStepCreateSession,          ///< a session object will be created
    DemoStepConfigSession,          ///< the session will be configured
    DemoStepStartSession,           ///< the session will be started
    DemoStepCreateLocalAnchor,      ///< the session will create a local anchor
    DemoStepCreateCloudAnchor,      ///< the session will create an unsaved cloud anchor
    DemoStepSaveCloudAnchor,        ///< the session will save the cloud anchor
    DemoStepStopSession,            ///< the session will stop
    DemoStepDestroySession,         ///< the session will be destroyed
    DemoStepCreateSessionForQuery,  ///< a session will be created to query for an achor
    DemoStepStartSessionForQuery,   ///< the session will be started to query for an anchor
    DemoStepLookForAnchor,          ///< the session will run the query
    DemoStepLookForNearbyAnchors,   ///< the session will run a query for nearby anchors
    DemoStepDeleteFoundAnchor,      ///< the session will delete the query
    DemoStepStopSessionForQuery     ///< the session will stop
} DemoStep;

@interface AnchorVisual:NSObject {}
@property(nonatomic, readwrite) SCNNode * node;
@property(nonatomic, readwrite) NSString * identifier;
@property(nonatomic, readwrite) ASACloudSpatialAnchor * cloudAnchor;
@property(nonatomic, readwrite) ARAnchor * localAnchor;
@end

@implementation AnchorVisual
@end

const uint NumberOfNearbyAnchors = 3; ///< the number of anchors we will create in the nearby demo

// MARK: - Formatting Helpers

static NSString *FeedbackToString(ASASessionUserFeedback userFeedback) {
    if (userFeedback == ASASessionUserFeedbackNone) {
        return @"";
    }
    
    NSString *result = @"";
    if (userFeedback & ASASessionUserFeedbackNotEnoughMotion) {
        result = [result stringByAppendingString:@"Not enough motion."];
    }
    if (userFeedback & ASASessionUserFeedbackMotionTooQuick) {
        result = [result stringByAppendingString:@"Motion is too quick."];
    }
    if (userFeedback & ASASessionUserFeedbackNotEnoughFeatures) {
        result = [result stringByAppendingString:@"Not enough features."];
    }
    return result;
}

static NSString *StatusToString(ASASessionStatus *status, DemoStep step) {
    NSString* feedback = FeedbackToString(status.userFeedback);
    if (step < DemoStepStopSession) {
        float progress = [status recommendedForCreateProgress];
        if ([feedback length] == 0 && progress < 1) { feedback = @"Keep moving! 🤳"; }
        return [NSString stringWithFormat:@"%.0f%% progress. %@", progress * 100.f, feedback];
    }
    else {
        if ([feedback length] == 0) { feedback = @"Keep moving! 🤳"; }
        return [NSString stringWithFormat:@"%@", feedback];
    }
}

static NSString * matrix_to_string(matrix_float4x4 value) {
    // @"[[%.3f %.3f %.3f %.3f] [%.3f %.3f %.3f %.3f] [%.3f %.3f %.3f %.3f] [%.3f %.3f %.3f %.3f]]"
    return [NSString stringWithFormat:@"[[%.2f %.2f %.2f %.2f] [%.2f %.2f %.2f %.2f] [%.2f %.2f %.2f %.2f] [%.2f %.2f %.2f %.2f]]",
            value.columns[0][0], value.columns[1][0], value.columns[2][0], value.columns[3][0],
            value.columns[0][1], value.columns[1][1], value.columns[2][1], value.columns[3][1],
            value.columns[0][2], value.columns[1][2], value.columns[2][2], value.columns[3][2],
            value.columns[0][3], value.columns[1][3], value.columns[2][3], value.columns[3][3]];
}

#pragma mark - ViewController
    
@implementation ViewController {
    UIButton *_button;
    UIButton *_nearbyChoice;
    UIButton *_errorControl;
    UIButton *_feedbackControl;
    NSMutableDictionary<NSString*, AnchorVisual *> *_anchorVisuals;
    
    ASACloudSpatialAnchorSession *_cloudSession;
    ASACloudSpatialAnchor *_cloudAnchor;
    ARAnchor *_localAnchor;
    SCNBox *_localAnchorCube;
    
    BOOL _basicDemo;            ///< whether we are running the basic demo (or the nearby demo)
    BOOL _enoughDataForSaving;  ///< whether we have enough data to save an anchor
    BOOL _isAsyncOperationInProgress; ///< whether an async operation is currently in progress
    uint _saveCount;            ///< the number of anchors we have saved to the cloud
    DemoStep _step;             ///< the next step to perform
    NSString *_targetId;        ///< the cloud anchor identifier to locate
    
    // Colors for the local anchor to indicate status
    UIColor *tentativeColor;
    UIColor *tentativeReadyColor;
    UIColor *savedColor;
    UIColor *foundColor;
    UIColor *deletedColor;
    UIColor *failedColor;
}

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

- (void)renderer:(id <SCNSceneRenderer>)renderer updateAtTime:(NSTimeInterval)time {
    // per-frame scenekit logic
    // modifications don't go through transaction model
    ASACloudSpatialAnchorSession *asession = _cloudSession;
    if (asession == NULL) {
        return;
    }
    [asession processFrame:_sceneView.session.currentFrame];
    ViewController *me = self;
    if (_step == DemoStepSaveCloudAnchor) {
        [_cloudSession getSessionStatusWithCompletionHandler:^(ASASessionStatus *value, NSError *error) {
            if (error) {
                NSLog(@"%@", error);
                [self->_errorControl setHidden:NO];
                [me showLogMessage:[error localizedDescription] here:self->_errorControl];
                return;
            }
            NSString *feedbackMessage = StatusToString(value, self->_step);
            NSString *current = me->_lastFeedbackMessage;
            if ([current isEqualToString:feedbackMessage]) {
                return;
            }
            dispatch_async(dispatch_get_main_queue(), ^{
                [me->_feedbackControl setTitle:feedbackMessage forState:UIControlStateNormal];
                me->_lastFeedbackMessage = feedbackMessage;
                
            });
            if (value.recommendedForCreateProgress >= 1.0f) {
                me->_localAnchorCube.firstMaterial.diffuse.contents = me->tentativeReadyColor;
            }
        }];
    }
}

-(void)updatePlaneMaterialDimensions:(SCNNode *)sceneNode {
    float width = ((SCNPlane *)sceneNode.geometry).width;
    float height = ((SCNPlane *)sceneNode.geometry).height;
    sceneNode.geometry.firstMaterial.diffuse.contentsTransform = SCNMatrix4MakeScale(width, height, 1.0);
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
    _isAsyncOperationInProgress = NO;
    ASALocateAnchorStatus status = [args status];
    switch (status) {
    case ASALocateAnchorStatusAlreadyTracked:
        // Ignore if we were already handling this.
        break;
    case ASALocateAnchorStatusLocated: {
        ASACloudSpatialAnchor *anchor = [args anchor];
        NSLog(@"Cloud Anchor found! Identifier: %@. Location: %@", anchor.identifier, matrix_to_string(anchor.localAnchor.transform));
        AnchorVisual* visual = [[AnchorVisual alloc] init];
        visual.cloudAnchor = anchor;
        visual.identifier = anchor.identifier;
        visual.localAnchor = anchor.localAnchor;
        [_anchorVisuals setValue:visual forKey:visual.identifier];
        [_sceneView.session addAnchor:anchor.localAnchor];
        NSString *message;
        ViewController *me = self;
        if (me->_basicDemo || [_anchorVisuals count] >= NumberOfNearbyAnchors) {
            // In the basic demo we found our anchor, or in the nearby demo found all our anchors
            [_feedbackControl setHidden:YES];
            message = @"Anchor(s) found! Tap to delete";
            me->_step = DemoStepDeleteFoundAnchor;
        }
        else {
            message = @"Anchor found! Tap to locate nearby";
            me->_step = DemoStepLookForNearbyAnchors;
        }
        dispatch_async(dispatch_get_main_queue(), ^{
            [me->_button setTitle:message forState:UIControlStateNormal];
        });
    }
        break;
    case ASALocateAnchorStatusNotLocatedAnchorDoesNotExist:
        break;
    case ASALocateAnchorStatusNotLocated:
        break;
    }
}

- (void)locateAnchorsCompleted:(ASACloudSpatialAnchorSession *)cloudSpatialAnchorSession :(ASALocateAnchorsCompletedEventArgs *)args {
    NSLog(@"Locate operation completed for watcher with identifier: %i", args.watcher.identifier);
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

- (void)nearbyTap:(UIButton *)sender {
    _basicDemo = NO;
    [self buttonTap:sender];
}
    
- (void)buttonTap:(UIButton *)sender {
    ViewController *me = self;
    int secondsInAWeek = 60 * 60 * 24 * 7;
    NSDate *oneWeekFromNow = [[NSDate alloc] initWithTimeIntervalSinceNow: (NSTimeInterval) secondsInAWeek];
    switch (_step) {
        case DemoStepCreateSession: {
            [_nearbyChoice setHidden:YES];
            [_errorControl setHidden:YES];
            _enoughDataForSaving = NO;
            _isAsyncOperationInProgress = NO;
            _saveCount = 0;
            _cloudSession = [[ASACloudSpatialAnchorSession alloc] init];
            [_button setTitle:@"Configure & Start Session" forState:UIControlStateNormal];
            _step = DemoStepConfigSession;
        }
        if (_basicDemo) {
            break;
        }
        case DemoStepConfigSession:
            _cloudSession.session = self.sceneView.session;
            _cloudSession.logLevel = ASASessionLogLevelAll;
            _cloudSession.delegate = self;
            _cloudSession.configuration.accountId = SpatialAnchorsAccountId;
            _cloudSession.configuration.accountKey = SpatialAnchorsAccountKey;
            _step = DemoStepStartSession;
        case DemoStepStartSession:
            [_feedbackControl setHidden:NO];
            [_cloudSession start];
            [_button setTitle:@"Tap on the screen to create a Local Anchor" forState:UIControlStateNormal];
            _step = DemoStepCreateLocalAnchor;
            break;
        case DemoStepCreateLocalAnchor:
            // We listen on touchesBegan and then call createLocalAnchor while in this step
            if (![_anchorVisuals objectForKey:@""]) {
                return;
            }
        case DemoStepCreateCloudAnchor:
            _cloudAnchor = [[ASACloudSpatialAnchor alloc] init];
            _cloudAnchor.localAnchor = _localAnchor;
            // In this sample app we delete the cloud anchor explicitly, but you can also set it to expire automatically
            _cloudAnchor.expiration = oneWeekFromNow;
            [_button setTitle:@"Save Cloud Anchor (once at 100%)" forState:UIControlStateNormal];
            _step = DemoStepSaveCloudAnchor;
            if (_basicDemo) {
                break;
            }
        case DemoStepSaveCloudAnchor: {
            if (!_enoughDataForSaving || _isAsyncOperationInProgress) {
                return;
            }
            _isAsyncOperationInProgress = YES;
            [_cloudSession createAnchor:_cloudAnchor withCompletionHandler:^(NSError *error) {
                self->_isAsyncOperationInProgress = NO;
                NSString *message;
                if (error) {
                    message = @"Creation failed";
                    [me->_errorControl setHidden:NO];
                    [me->_errorControl setTitle:error.localizedDescription forState:UIControlStateNormal];
                    me->_localAnchorCube.firstMaterial.diffuse.contents = me->failedColor;
                    me->_step = DemoStepSaveCloudAnchor;
                } else {
                    me->_saveCount++;
                    me->_localAnchorCube.firstMaterial.diffuse.contents = me->savedColor;
                    me->_targetId = me->_cloudAnchor.identifier;
                    AnchorVisual* visual = me->_anchorVisuals[@""];
                    visual.cloudAnchor = me->_cloudAnchor;
                    visual.identifier = me->_cloudAnchor.identifier;
                    [me->_anchorVisuals setValue:visual forKey:visual.identifier];
                    [me->_anchorVisuals removeObjectForKey:@""];
                    if (!me->_basicDemo && me->_saveCount < NumberOfNearbyAnchors) {
                        message = @"Tap somewhere to create the next Anchor";
                        me->_step = DemoStepCreateLocalAnchor;
                    } else {
                        [self->_feedbackControl setHidden:YES];
                        message = @"Stop & Destroy Session";
                        me->_step = DemoStepStopSession;
                    }
                }
                [me->_button setTitle:message forState:UIControlStateNormal];
            }];
            [_button setTitle:@"Cloud Anchor being saved..." forState:UIControlStateNormal];
        }
            break;
        case DemoStepStopSession:
            [_cloudSession stop];
            _step = DemoStepDestroySession;
        case DemoStepDestroySession:
            [_button setTitle:@"Configure second Session" forState:UIControlStateNormal];
            _cloudAnchor = NULL;
            _localAnchor = NULL;
            _cloudSession = NULL;
            for (AnchorVisual* visual in [_anchorVisuals allValues]) {
                [visual.node removeFromParentNode];
            }
            [_anchorVisuals removeAllObjects];
            if (_targetId == nil) {
                _step = 0;
                return;
            }
            _step = DemoStepCreateSessionForQuery;
            if (_basicDemo) {
                break;
            }
        case DemoStepCreateSessionForQuery:
            _enoughDataForSaving = NO;
            _cloudSession = [[ASACloudSpatialAnchorSession alloc] init];
            _cloudSession.session = self.sceneView.session;
            _cloudSession.logLevel = ASASessionLogLevelAll;
            _cloudSession.delegate = self;
            _cloudSession.configuration.accountId = SpatialAnchorsAccountId;
            _cloudSession.configuration.accountKey = SpatialAnchorsAccountKey;
            [_button setTitle:@"Start Session & Look for Anchor" forState:UIControlStateNormal];
            _step = DemoStepStartSessionForQuery;
            break;
        case DemoStepStartSessionForQuery:
            [_feedbackControl setHidden:NO];
            [_cloudSession start];
            _step = DemoStepLookForAnchor;
        case DemoStepLookForAnchor: {
            if (_isAsyncOperationInProgress) {
                return;
            }
            _isAsyncOperationInProgress = YES;
            NSArray *ids = @[_targetId];
            ASAAnchorLocateCriteria *criteria = [ASAAnchorLocateCriteria new];
            criteria.identifiers = ids;
            [_cloudSession createWatcher:criteria];
            [_button setTitle:@"Locating Anchor ..." forState:UIControlStateNormal];
        }
        break;
        case DemoStepLookForNearbyAnchors: {
            if ([_anchorVisuals count] < 1) {
                [_button setTitle:@"First Anchor not found yet" forState:UIControlStateNormal];
                return;
            }
            ASAAnchorLocateCriteria *criteria = [ASAAnchorLocateCriteria new];
            ASANearAnchorCriteria *nearCriteria = [ASANearAnchorCriteria new];
            [nearCriteria setDistanceInMeters:50];
            [nearCriteria setSourceAnchor:_anchorVisuals[_targetId].cloudAnchor];
            [criteria setNearAnchor:nearCriteria];
            [_cloudSession createWatcher:criteria];
            [_button setTitle:@"Locating nearby Anchors ..." forState:UIControlStateNormal];
        }
        break;
        case DemoStepDeleteFoundAnchor: {
            if ([_anchorVisuals count] < 1) {
                [_button setTitle:@"Anchor not found yet" forState:UIControlStateNormal];
                return;
            } else if (_isAsyncOperationInProgress){
                return;
            }
            _isAsyncOperationInProgress = YES;
            [_button setTitle:@"Deleting found Anchors ..." forState:UIControlStateNormal];
            for (AnchorVisual *visual in [_anchorVisuals allValues]) {
                if (visual.cloudAnchor == nil) {
                    continue;
                }
                [_cloudSession deleteAnchor:visual.cloudAnchor withCompletionHandler:^(NSError *error) {
                    self->_isAsyncOperationInProgress = NO;
                    NSString *message;
                    if (error) {
                        [self->_errorControl setHidden:NO];
                        message = @"Deletion failed";
                        [me->_errorControl setTitle:error.localizedDescription forState:UIControlStateNormal];
                        visual.node.geometry.firstMaterial.diffuse.contents = me->failedColor;
                    } else {
                        visual.node.geometry.firstMaterial.diffuse.contents = me->deletedColor;
                        message = [NSString stringWithFormat:@"Cloud Anchor deleted. Tap to stop Session"];
                    }
                    me->_step = DemoStepStopSessionForQuery;
                    [me->_button setTitle:message forState:UIControlStateNormal];
                }];
            }
        }
            break;
        case DemoStepStopSessionForQuery: {
            _cloudAnchor = NULL;
            _localAnchor = NULL;
            _cloudSession = NULL;
            [me->_button setTitle:@"Start Basic Demo" forState:UIControlStateNormal];
            [_nearbyChoice setHidden:NO];
            _basicDemo = YES;
            for (AnchorVisual* visual in [_anchorVisuals allValues]) {
                [visual.node removeFromParentNode];
            }
            [_anchorVisuals removeAllObjects];
            _step = DemoStepCreateSession;
            return;
        }
            break;
        default:
            assert(false);
            _step = 0;
            return;
    }
}

#pragma mark - UITextFieldDelegate implementation

- (void)touchesBegan:(NSSet *)touches withEvent:(UIEvent *)event{
    [self.view endEditing:YES];
    [super touchesBegan:touches withEvent:event];
    
    // Place local anchor
    if (_step != DemoStepCreateLocalAnchor) {
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

- (void)createLocalAnchor:(simd_float4x4)anchorLocation {
    _localAnchor = [[ARAnchor alloc] initWithTransform:anchorLocation];
    [_sceneView.session addAnchor:_localAnchor];
    AnchorVisual* visual = [[AnchorVisual alloc] init];
    visual.identifier = @"";
    visual.localAnchor = _localAnchor;
    [_anchorVisuals setValue:visual forKey:visual.identifier];
    [_button setTitle:@"Create Cloud Anchor" forState:UIControlStateNormal];
    _step = DemoStepCreateCloudAnchor;
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

- (BOOL)prefersStatusBarHidden {
    return YES;
}
    
- (void)viewDidLoad {
    [super viewDidLoad];
    
    [self performSelector:@selector(setNeedsStatusBarAppearanceUpdate)];
    
    // Set the view's delegate
    self.sceneView.delegate = self;
    
    // Show statistics such as fps and timing information
    self.sceneView.showsStatistics = NO;
    
    // Create a new scene
    // SCNScene *scene = [SCNScene sceneNamed:@"art.scnassets/ship.scn"]; // part of default project template
    SCNScene *scene = [SCNScene new];
    
    // Set the scene to the view
    self.sceneView.scene = scene;
    
    _step = DemoStepCreateSession;

    tentativeColor = [UIColor.blueColor colorWithAlphaComponent:0.3];       // light blue for a local anchor
    tentativeReadyColor = [UIColor.blueColor colorWithAlphaComponent:0.8];  // dark blue when we are ready to save an anchor to the cloud
    savedColor = [UIColor.greenColor colorWithAlphaComponent:0.6];          // green when the cloud anchor was saved successfully
    foundColor = [UIColor.yellowColor colorWithAlphaComponent:0.6];         // yellow when we successfully located a cloud anchor
    deletedColor = [UIColor.blackColor colorWithAlphaComponent:0.6];        // grey for a deleted cloud anchor
    failedColor = [UIColor.redColor colorWithAlphaComponent:0.6];           // red when there was an error
    
    _anchorVisuals = [[NSMutableDictionary<NSString*, AnchorVisual *> alloc] init];
    
    _feedbackControl  = [self addButtonAt:_sceneView.bounds.size.height - 40 lines:1];
    [_feedbackControl setBackgroundColor: [UIColor clearColor]];
    [_feedbackControl setTitleColor:[UIColor yellowColor] forState:UIControlStateNormal];
    [_feedbackControl setContentHorizontalAlignment:UIControlContentHorizontalAlignmentLeft];
    [_feedbackControl setHidden:YES];

    _button = [self addButtonAt:_sceneView.bounds.size.height - 80 lines:1];
    [_button setTitle:@"Start Basic Demo" forState:UIControlStateNormal];
    [_button addTarget:self action:@selector(buttonTap:) forControlEvents:UIControlEventTouchDown];
    _basicDemo = YES;
    
    _nearbyChoice = [self addButtonAt:_sceneView.bounds.size.height - 140 lines:1];
    [_nearbyChoice setTitle:@"Start Nearby Demo" forState:UIControlStateNormal];
    [_nearbyChoice addTarget:self action:@selector(nearbyTap:) forControlEvents:UIControlEventTouchDown];
    
    _errorControl  = [self addButtonAt:300 lines:5];
    [_errorControl setHidden:YES];
    
    if ([SpatialAnchorsAccountId  isEqual: @"Set me"] || [SpatialAnchorsAccountKey  isEqual: @"Set me"])
    {
        [_button setHidden:YES];
        [_nearbyChoice setHidden:YES];
        [_errorControl setHidden:NO];
        [self showLogMessage:@"Set SpatialAnchorsAccountId and SpatialAnchorsAccountKey in ViewController.m" here:_errorControl];
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
    
    // Pause the view's session
    [self.sceneView.session pause];
}

- (void)didReceiveMemoryWarning {
    [super didReceiveMemoryWarning];
    // Release any cached data, images, etc that aren't in use.
}

#pragma mark - ARSCNViewDelegate

// Override to create and configure nodes for anchors added to the view's session.
- (SCNNode *)renderer:(id<SCNSceneRenderer>)renderer nodeForAnchor:(ARAnchor *)anchor {
    for (AnchorVisual* visual in [_anchorVisuals allValues]) {
        if (visual.localAnchor == anchor) {
            NSLog(@"renderer:nodeForAnchor with local Anchor %p at %@", anchor, matrix_to_string(anchor.transform));
            SCNBox *cube = [SCNBox boxWithWidth:0.2f height:0.2f length:0.2f chamferRadius:0.0f];
            if ([visual.identifier length] > 0) {
                cube.firstMaterial.diffuse.contents = foundColor;
            } else {
                cube.firstMaterial.diffuse.contents = tentativeColor;
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
