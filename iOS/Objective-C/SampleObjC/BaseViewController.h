// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#import <ARKit/ARKit.h>
#import <AzureSpatialAnchors/AzureSpatialAnchors.h>
#import <SceneKit/SceneKit.h>
#import <UIKit/UIKit.h>

typedef enum DemoStep {
    DemoStepPrepare,                ///< prepare to start
    DemoStepCreateCloudAnchor,      ///< the session will create a cloud anchor
    DemoStepEnterAnchorNumber,      ///< in the sharing sample, we will enter the anchor number to locate
    DemoStepLookForAnchor,          ///< the session will look for an anchor
    DemoStepLookForNearbyAnchors,   ///< the session will look for nearby anchors
    DemoStepDeleteFoundAnchors,     ///< the session will delete found anchors
    DemoStepStopSession,            ///< the session will stop and be cleaned up
} DemoStep;

@interface AnchorVisual : NSObject {}
@property(nonatomic, readwrite) SCNNode * node;
@property(nonatomic, readwrite) NSString * identifier;
@property(nonatomic, readwrite) ASACloudSpatialAnchor * cloudAnchor;
@property(nonatomic, readwrite) ARAnchor * localAnchor;
@end

@interface BaseViewController : UIViewController <ARSCNViewDelegate, SCNSceneRendererDelegate, ASACloudSpatialAnchorSessionDelegate>
{
    UIButton *_feedbackControl;
    UIButton *_button;
    UIButton *_errorControl;
    UIButton *_backControl;

    NSMutableDictionary<NSString*, AnchorVisual *> *_anchorVisuals;
    
    ASACloudSpatialAnchorSession *_cloudSession;
    ASACloudSpatialAnchor *_cloudAnchor;
    ARAnchor *_localAnchor;
    SCNBox *_localAnchorCube;
    
    BOOL _enoughDataForSaving;        ///< whether we have enough data to save an anchor
    BOOL _currentlySavingAnchor;      ///< whether we are in the progress on saving an anchor
    BOOL _ignoreTaps;                 ///< whether we should ignore taps to wait for current demo step finishing
    uint _saveCount;                  ///< the number of anchors we have saved to the cloud
    DemoStep _step;                   ///< the next step to perform
    NSString *_targetId;              ///< the cloud anchor identifier to locate
    
    // Colors for the local anchor to indicate status
    UIColor *readyColor;
    UIColor *savedColor;
    UIColor *foundColor;
    UIColor *deletedColor;
    UIColor *failedColor;
}

@property (strong, nonatomic) ARSCNView *sceneView;

-(UIButton *)addButtonAt:(float)top lines:(int)lines;
-(void)showLogMessage:(NSString *)text here:(UIView *)here;
-(void)moveToMainMenu;

-(void)moveToNextStepAfterCreateCloudAnchor;
-(void)moveToNextStepAfterAnchorLocated;

-(void)startSession;
-(void)createCloudAnchor;
-(void)stopSession;
-(void)lookForAnchor;
-(void)lookForNearbyAnchors;
-(void)deleteFoundAnchors;

@end
