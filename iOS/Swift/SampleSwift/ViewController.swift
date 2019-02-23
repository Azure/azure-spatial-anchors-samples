// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
import Foundation
import UIKit
import SceneKit
import ARKit

class AnchorVisual {
    init() {
        node = nil
        identifier = ""
        cloudAnchor = nil
        localAnchor = nil
    }
    var node : SCNNode? = nil
    var identifier : String
    var cloudAnchor : ASACloudSpatialAnchor? = nil
    var localAnchor : ARAnchor? = nil
}

class ViewController: UIViewController, ARSCNViewDelegate, ASACloudSpatialAnchorSessionDelegate {
    
    // Set this to the account ID provided for the Azure Spatial Service resource.
    let SpatialAnchorsAccountId = "Set me"
    
    // Set this to the account key provided for the Azure Spatial Service resource.
    let SpatialAnchorsAccountKey = "Set me"
    
    enum DemoStep : uint {
        case CreateSession          // a session object will be created
        case ConfigSession          // the session will be configured
        case StartSession           // the session will be started
        case CreateLocalAnchor      // the session will create a local anchor
        case CreateCloudAnchor      // the session will create an unsaved cloud anchor
        case SaveCloudAnchor        // the session will save the cloud anchor
        case StopSession            // the session will stop
        case DestroySession         // the session will be destroyed
        case CreateSessionForQuery  // a session will be created to query for an achor
        case StartSessionForQuery   // the session will be started to query for an anchor
        case LookForAnchor          // the session will run the query
        case LookForNearbyAnchors   // the session will run a query for nearby anchors
        case DeleteFoundAnchor      // the session will delete the query
        case StopSessionForQuery    // the session will stop
    }
    
    let numberOfNearbyAnchors = 3; // the number of anchors we will create in the nearby demo

    let tentativeColor = UIColor.blue.withAlphaComponent(0.3)       // light blue for a local anchor
    let tentativeReadyColor = UIColor.blue.withAlphaComponent(0.8)  // dark blue when we are ready to save an anchor to the cloud
    let savedColor = UIColor.green.withAlphaComponent(0.6)          // green when the cloud anchor was saved successfully
    let foundColor = UIColor.yellow.withAlphaComponent(0.6)         // yellow when we successfully located a cloud anchor
    let deletedColor = UIColor.black.withAlphaComponent(0.6)        // grey for a deleted cloud anchor
    let failedColor = UIColor.red.withAlphaComponent(0.6)           // red when there was an error
    
    var _button : UIButton? = nil
    var _nearbyChoice : UIButton? = nil
    var _errorControl : UIButton? = nil
    var _feedbackControl : UIButton? = nil
    var _lastFeedbackMessage : String? = nil
    var _anchorVisuals = [String : AnchorVisual]()
    
    var _cloudSession : ASACloudSpatialAnchorSession? = nil
    var _cloudAnchor : ASACloudSpatialAnchor? = nil
    var _localAnchor : ARAnchor? = nil
    var _localAnchorCube : SCNBox? = nil
    
    var _basicDemo = true;              // whether we are running the basic demo (or the nearby demo)
    var _enoughDataForSaving = false    // whether we have enough data to save an anchor
    var _isAsyncOperationInProgress = false // whether an async operation is currently in progress
    var _saveCount = 0                  // the number of anchors we have saved to the cloud
    var _step = DemoStep.CreateSession  // the next step to perform
    var _targetId : String? = nil       // the cloud anchor identifier to locate

    @IBOutlet var sceneView: ARSCNView!
    
    override var prefersStatusBarHidden : Bool {
        return true;
    }
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        // Set the view's delegate
        self.sceneView.delegate = self
        
        // Show statistics such as fps and timing information
        self.sceneView.showsStatistics = false
        
        // Create a new scene
        let scene = SCNScene()
        
        // Set the scene to the view
        sceneView.scene = scene
        
        // NEW CODE BEGINS
        
        _feedbackControl = self.addButtonAt(Double(self.sceneView.bounds.size.height - 40), lines:Double(1.0))
        _feedbackControl!.backgroundColor = UIColor.clear
        _feedbackControl!.setTitleColor(UIColor.yellow, for: .normal)
        _feedbackControl!.contentHorizontalAlignment = UIControl.ContentHorizontalAlignment.left
        _feedbackControl!.isHidden = true
        
        _button = self.addButtonAt(Double(self.sceneView.bounds.size.height - 80), lines:Double(1.0))
        _button!.setTitle("Start Basic Demo", for:UIControlState.normal)
        _button!.addTarget(self, action:#selector(buttonTap), for: .touchDown)
        
        _nearbyChoice = self.addButtonAt(Double(self.sceneView.bounds.size.height - 140), lines:Double(1.0))
        _nearbyChoice!.setTitle("Start Nearby Demo", for:UIControlState.normal)
        _nearbyChoice!.addTarget(self, action:#selector(nearbyTap), for: .touchDown)
        
        _errorControl = self.addButtonAt(Double(300.0), lines:Double(5.0))
        _errorControl!.isHidden = true
        
        // NEW CODE ENDS
        
        if (SpatialAnchorsAccountId == "Set me" || SpatialAnchorsAccountKey == "Set me")
        {
            _button!.isHidden = true
            _nearbyChoice!.isHidden = true
            _errorControl!.isHidden = false
            showLogMessage(text: "Set SpatialAnchorsAccountId and SpatialAnchorsAccountKey in ViewController.swift", here: _errorControl)
        }
    }
    
    @objc func nearbyTap(sender: UIButton) {
        _basicDemo = false
        buttonTap(sender: sender)
    }
    
    @objc func buttonTap(sender: UIButton) {
        switch (_step) {
        case DemoStep.CreateSession: // a session object will be created
            _nearbyChoice!.isHidden = true
            _errorControl!.isHidden = true
            _enoughDataForSaving = false
            _saveCount = 0
            _isAsyncOperationInProgress = false
            _cloudSession = ASACloudSpatialAnchorSession()
            _button!.setTitle("Configure & Start Session", for: UIControlState.normal)
            _step = DemoStep.ConfigSession
            if (!_basicDemo) {
                fallthrough
            }
        case DemoStep.ConfigSession: // the session will be configured
            _cloudSession!.session = self.sceneView.session;
            _cloudSession!.logLevel = ASASessionLogLevel.all
            _cloudSession!.delegate = self;
            _cloudSession!.configuration.accountId = SpatialAnchorsAccountId
            _cloudSession!.configuration.accountKey = SpatialAnchorsAccountKey
            _step = DemoStep.StartSession
            fallthrough
        case DemoStep.StartSession: // the session will be started
            _feedbackControl!.isHidden = false
            _cloudSession!.start()
            _button!.setTitle("Tap on the screen to create a Local Anchor", for: UIControlState.normal)
            _step = DemoStep.CreateLocalAnchor
        case DemoStep.CreateLocalAnchor: // the session will create a local anchor
            // We listen on touchesBegan and then call createLocalAnchor while in this step
            if _anchorVisuals[""] == nil {
                return
            }
        case DemoStep.CreateCloudAnchor: // the session will create an unsaved cloud anchor
            _cloudAnchor = ASACloudSpatialAnchor()
            _cloudAnchor!.localAnchor = _localAnchor
            // In this sample app we delete the cloud anchor explicitly, but you can also set it to expire automatically
            let secondsInAWeek = 60.0 * 60.0 * 24.0 * 7.0
            let oneWeekFromNow = Date(timeIntervalSinceNow: secondsInAWeek)
            _cloudAnchor!.expiration = oneWeekFromNow
            _button!.setTitle("Save Cloud Anchor (once at 100%)", for: UIControlState.normal)
            _step = DemoStep.SaveCloudAnchor
            if (!_basicDemo) {
                fallthrough
            }
        case DemoStep.SaveCloudAnchor: // the session will save the cloud anchor
            if (!_enoughDataForSaving || _isAsyncOperationInProgress) {
                return
            }
            _isAsyncOperationInProgress = true
            _cloudSession?.createAnchor(_cloudAnchor!, withCompletionHandler: { (error: Error?) in
                self._isAsyncOperationInProgress = false
                var message = ""
                if (error != nil) {
                    message = "Creation failed"
                    self._localAnchorCube?.firstMaterial?.diffuse.contents = self.failedColor
                    self._step = DemoStep.SaveCloudAnchor
                    self._errorControl?.isHidden = false
                    self._errorControl?.setTitle(error!.localizedDescription, for: UIControlState.normal)
                }
                else {
                    self._saveCount += 1;
                    self._localAnchorCube?.firstMaterial?.diffuse.contents = self.savedColor
                    self._targetId = self._cloudAnchor!.identifier
                    let visual = self._anchorVisuals[""]
                    visual?.cloudAnchor = self._cloudAnchor
                    visual?.identifier = self._cloudAnchor!.identifier
                    self._anchorVisuals.removeValue(forKey: "")
                    self._anchorVisuals[self._cloudAnchor!.identifier] = visual!
                    if (!self._basicDemo && self._saveCount < self.numberOfNearbyAnchors) {
                        message = "Tap somewhere to create the next Anchor"
                        self._step = DemoStep.CreateLocalAnchor
                    } else {
                        self._feedbackControl!.isHidden = true
                        message = "Stop & Destroy Session"
                        self._step = DemoStep.StopSession
                    }
                }
                self.showLogMessage(text: message, here: self._button)
            })
            _button!.setTitle("Cloud Anchor being saved...", for: UIControlState.normal)
        case DemoStep.StopSession: // the session will stop
            _cloudSession?.stop()
            self._step = DemoStep.DestroySession
            fallthrough
        case DemoStep.DestroySession: // the session will be destroyed
            _button!.setTitle("Configure Second Session", for: UIControlState.normal)
            _cloudAnchor = nil
            _localAnchor = nil
            _cloudSession = nil
            for (visual) in _anchorVisuals.values {
                 visual.node!.removeFromParentNode()
            }
           _anchorVisuals.removeAll()
            if (_targetId == nil) {
                _step = DemoStep.CreateSession
                return
            }
            _step = DemoStep.CreateSessionForQuery
            if (!_basicDemo) {
                fallthrough
            }
        case DemoStep.CreateSessionForQuery: // a session will be creaed to query for an achor
            _enoughDataForSaving = false
            _cloudSession = ASACloudSpatialAnchorSession()
            _cloudSession!.session = self.sceneView.session;
            _cloudSession!.logLevel = ASASessionLogLevel.all
            _cloudSession!.delegate = self;
            _cloudSession!.configuration.accountId = SpatialAnchorsAccountId
            _cloudSession!.configuration.accountKey = SpatialAnchorsAccountKey
            _button!.setTitle("Start Session & Look for Anchor", for: UIControlState.normal)
            _step = DemoStep.StartSessionForQuery
            if (!_basicDemo) {
                fallthrough
            }
        case DemoStep.StartSessionForQuery: // the session will be started to query for an anchor
            _feedbackControl!.isHidden = false
            _cloudSession?.start()
            _step = DemoStep.LookForAnchor
            fallthrough
        case DemoStep.LookForAnchor: // the session will run the query
            if (_isAsyncOperationInProgress) {
                return
            }
            _isAsyncOperationInProgress = true
            let ids = [_targetId!]
            let criteria = ASAAnchorLocateCriteria()!
            criteria.identifiers = ids
            _cloudSession!.createWatcher(criteria)
            _button!.setTitle("Locating Anchor ...", for: UIControlState.normal)
        case DemoStep.LookForNearbyAnchors: // the session will run a query for nearby anchors
            if (_anchorVisuals.count < 1) {
                _button!.setTitle("First Anchor not found yet", for: UIControlState.normal)
                return
            } else if (_isAsyncOperationInProgress) {
                return
            }
            _isAsyncOperationInProgress = true
            let criteria = ASAAnchorLocateCriteria()!
            let nearbyCriteria = ASANearAnchorCriteria()!
            nearbyCriteria.distanceInMeters = 50
            nearbyCriteria.sourceAnchor = _anchorVisuals[_targetId!]!.cloudAnchor
            criteria.nearAnchor = nearbyCriteria
            _cloudSession!.createWatcher(criteria)
            _button!.setTitle("Locating nearby Anchors ...", for: UIControlState.normal)
        case DemoStep.DeleteFoundAnchor: // the session will delete the query
            if (_anchorVisuals.count < 1) {
                _button!.setTitle("Anchor not found yet", for: UIControlState.normal)
                return
            } else if (_isAsyncOperationInProgress) {
                return
            }
            _isAsyncOperationInProgress = true
            _feedbackControl!.isHidden = true
            for (visual) in _anchorVisuals.values {
                if (visual.cloudAnchor == nil) {
                    continue
                }
                _cloudSession?.delete(visual.cloudAnchor!, withCompletionHandler: { (error: Error?) in
                    self._isAsyncOperationInProgress = false
                    var message = ""
                    var errMessage = ""
                    if (error != nil) {
                        message = "Deletion failed"
                        errMessage = error!.localizedDescription
                        visual.node?.geometry?.firstMaterial?.diffuse.contents = self.failedColor
                        self._errorControl!.isHidden = false
                        self._errorControl?.setTitle(errMessage, for: UIControlState.normal)
                    }
                    else {
                        visual.node?.geometry?.firstMaterial?.diffuse.contents = self.deletedColor
                        message = "Cloud Anchor deleted. Tap to stop Session"
                    }
                    self._step = DemoStep.StopSessionForQuery
                    self._button!.setTitle(message, for: UIControlState.normal)
                })
            }
        case DemoStep.StopSessionForQuery:
            _cloudAnchor = nil
            _localAnchor = nil
            _cloudSession = nil
            for (visual) in _anchorVisuals.values {
                visual.node!.removeFromParentNode()
            }
            _anchorVisuals.removeAll()
            _basicDemo = true
            _button!.setTitle("Start Basic Demo", for: UIControlState.normal)
            _nearbyChoice?.isHidden = false
            _step = DemoStep.CreateSession;
            return;
        }
        return
    }
    
    // MARK: - Formatting Helpers

    static func matrix_to_string(value: matrix_float4x4) -> String {
        return String.init(format: "[[%.3f %.3f %.3f %.3f] [%.3f %.3f %.3f %.3f] [%.3f %.3f %.3f %.3f] [%.3f %.3f %.3f %.3f]]",
                           value.columns.0[0], value.columns.1[0], value.columns.2[0], value.columns.3[0],
                           value.columns.0[1], value.columns.1[1], value.columns.2[1], value.columns.3[1],
                           value.columns.0[2], value.columns.1[2], value.columns.2[2], value.columns.3[2],
                           value.columns.0[3], value.columns.1[3], value.columns.2[3], value.columns.3[3])
    }
    
    func StatusToString(status:ASASessionStatus, step:DemoStep) -> String {
        var feedback = FeedbackToString(userFeedback:status.userFeedback)
        if (step.rawValue < DemoStep.StopSession.rawValue) {
            let progress = status.recommendedForCreateProgress
            if (feedback.isEmpty && progress < 1.0) { feedback = "Keep moving! 🤳"}
            return "\(String(format: "%.0f", progress * 100))% progress. \(feedback)"
        }
        else {
            if (feedback.isEmpty) { feedback = "Keep moving! 🤳"}
            return "\(feedback)"
        }
    }
    
    func FeedbackToString(userFeedback:ASASessionUserFeedback) -> String {
        if (userFeedback.isEmpty) {
            return ""
        }
        var result = ""
        if (userFeedback.contains(ASASessionUserFeedback.notEnoughMotion)) {
            result.append("Not enough motion.")
        }
        if (userFeedback.contains(ASASessionUserFeedback.motionTooQuick)) {
            result.append("Motion is too quick.")
        }
        if (userFeedback.contains(ASASessionUserFeedback.notEnoughFeatures)) {
            result.append("Not enough features.")
        }
        return result;
    }
    
    // MARK: - ASACloudSpatialAnchorSessionDelegate
    
    internal func onLogDebug(_ cloudSpatialAnchorSession: ASACloudSpatialAnchorSession!, _ args: ASAOnLogDebugEventArgs!) {
//        // If you want, you can print logs to the console
//        if let message = args.message {
//            print(message)
//        }
    }
    
    //  // You can use this helper method to get an authentication token via Azure Active Directory.
    //    internal func tokenRequired(_ cloudSpatialAnchorSession: ASACloudSpatialAnchorSession!, _ args: ASATokenRequiredEventArgs!) {
    //        let deferral = args.getDeferral()
    //        // AAD user token scenario to get an authentication token
    //        AuthenticationHelper.acquireAuthenticationToken { (token: String?, error: Error?) in
    //            if error != nil {
    //                let errMessage = error!.localizedDescription
    //                self._errorControl!.isHidden = false
    //                self._errorControl?.setTitle(errMessage, for: UIControlState.normal)
    //            }
    //            if token != nil {
    //                args.authenticationToken = token
    //            }
    //            deferral?.complete()
    //        }
    //    }
    
    internal func sessionUpdated(_ cloudSpatialAnchorSession:ASACloudSpatialAnchorSession!, _ args:ASASessionUpdatedEventArgs!) {
        let status = args.status!
        let message = StatusToString(status: status, step: _step)
        _enoughDataForSaving = status.recommendedForCreateProgress >= 1.0
        self.showLogMessage(text: message, here: self._feedbackControl)
    }
    
    internal func error (_ cloudSpatialAnchorSession: ASACloudSpatialAnchorSession!, _ args:ASASessionErrorEventArgs!) {
        _errorControl!.isHidden = false
        self.showLogMessage(text: args.errorMessage, here: self._errorControl)
        print(args.errorMessage)
    }
    
    internal func anchorLocated(_ cloudSpatialAnchorSession: ASACloudSpatialAnchorSession!, _ args: ASAAnchorLocatedEventArgs!) {
        _isAsyncOperationInProgress = false
        let status = args.status
        switch (status) {
        case ASALocateAnchorStatus.alreadyTracked:
            // Ignore if we were already handling this.
            break
        case ASALocateAnchorStatus.located:
            let anchor = args.anchor
            print("Cloud Anchor found! Identifier: \(anchor!.identifier ?? "Null"). Location: \(ViewController.matrix_to_string(value: anchor!.localAnchor.transform))")
            let visual = AnchorVisual()
            visual.cloudAnchor = anchor
            visual.identifier = anchor!.identifier
            visual.localAnchor = anchor!.localAnchor
            _anchorVisuals[visual.identifier] = visual
            self.sceneView.session.add(anchor: anchor!.localAnchor)
            var message: String?
            if (_basicDemo || _anchorVisuals.count >= numberOfNearbyAnchors) {
                // In the basic demo we found our anchor, or in the nearby demo we found all our anchors
                _feedbackControl?.isHidden = true
                message = "Anchor(s) found! Tap to delete"
                _step = DemoStep.DeleteFoundAnchor
            }
            else {
                message = "Anchor found! Tap to locate nearby"
                _step = DemoStep.LookForNearbyAnchors
            }
            self._button!.setTitle(message, for: UIControlState.normal)
        case ASALocateAnchorStatus.notLocatedAnchorDoesNotExist:
            break
        case ASALocateAnchorStatus.notLocated:
            break
        }
    }
    
    internal func locateAnchorsCompleted(_ cloudSpatialAnchorSession: ASACloudSpatialAnchorSession!, _ args: ASALocateAnchorsCompletedEventArgs!) {
        print("Anchor locate operation completed completed for watcher with identifier: \(args.watcher!.identifier)")
    }
    
    // MARK: - ARSCNViewDelegate
    
    // Override to create and configure nodes for anchors added to the view's session.
    func renderer(_ renderer: SCNSceneRenderer, nodeFor anchor: ARAnchor) -> SCNNode? {
        for (visual) in _anchorVisuals.values {
            if (visual.localAnchor == anchor) {
                print("renderer:nodeForAnchor with local anchor \(anchor) at \(ViewController.matrix_to_string(value: anchor.transform))")
                let cube = SCNBox(width:0.2, height:0.2, length:0.2, chamferRadius:0.0)
                if (visual.identifier.count > 0) {
                    cube.firstMaterial?.diffuse.contents = foundColor
                } else {
                    cube.firstMaterial?.diffuse.contents = tentativeColor
                }
                visual.node = SCNNode(geometry:cube)
                _localAnchorCube = cube
                return visual.node;
            }
        }
        return nil;
    }
    
    func session(_ session: ARSession, didFailWithError error: Error) {
        // Present an error message to the user
    }
    
    func sessionWasInterrupted(_ session: ARSession) {
        // Inform the user that the session has been interrupted, for example, by presenting an overlay
    }
    
    func sessionInterruptionEnded(_ session: ARSession) {
        // Reset tracking and/or remove existing anchors if consistent tracking is required
    }
    
    // MARK: - ViewController

    func renderer(_ renderer: SCNSceneRenderer, updateAtTime time: TimeInterval) {
        // per-frame scenekit logic
        // modifications don't go through transaction model
        let asession = _cloudSession
        if (asession == nil) {
            return
        }
        asession?.processFrame(self.sceneView.session.currentFrame)
        
        if (_step == DemoStep.SaveCloudAnchor) {
            _cloudSession?.getStatusWithCompletionHandler( { (value, error:Error?) -> (Void) in
                if (error != nil) {
                    self._errorControl!.isHidden = false
                    self.showLogMessage(text: (error?.localizedDescription)!, here: self._errorControl)
                    return
                }
                
                let feedbackMessage = self.StatusToString(status: value!, step: self._step)
                let current = self._lastFeedbackMessage
                if (current == feedbackMessage) {
                    return
                }

                DispatchQueue.main.async {
                    self._feedbackControl?.setTitle(feedbackMessage, for: UIControlState.normal)
                    self._lastFeedbackMessage = feedbackMessage
                }
                
                if (value!.recommendedForCreateProgress > 1.0) {
                    self._localAnchorCube?.firstMaterial?.diffuse.contents = self.tentativeReadyColor
                }
            })
        }
    }
    
    func showLogMessage (text: String, here: UIView!) {
        DispatchQueue.main.async {
            let button = here as? UIButton
            button?.setTitle(text, for: UIControlState.normal)
            let textField = here as? UITextField
            textField?.text = text
        }
    }
    
    // MARK: - View Management
    
    override func touchesBegan(_ touches: Set<UITouch>, with event: UIEvent?) {
        if (_step != DemoStep.CreateLocalAnchor) {
            return
        }
        var anchorLocation = simd_float4x4()
        let touchLocation = touches.first!.location(in: self.sceneView)
        let hitResultsFeaturePoints: [ARHitTestResult] = self.sceneView.hitTest(touchLocation, types: .featurePoint)
        if let hit = hitResultsFeaturePoints.first {
            // If we have a feature point create the local anchor there
            anchorLocation = hit.worldTransform
        } else {
            // Otherwise create the local anchor using the camera's current position
            if let currentFrame = sceneView.session.currentFrame {
                var translation = matrix_identity_float4x4
                translation.columns.3.z = -0.5 // Put it 0.5 meters in front of the camera
                let transform = simd_mul(currentFrame.camera.transform, translation)
                anchorLocation = transform
            }
        }
        if (anchorLocation == simd_float4x4()){
            _button!.setTitle("Trouble placing anchor. Tap to try again", for: UIControlState.normal)
            return
        } else {
            createLocalAnchor(anchorLocation: anchorLocation)
        }
    }
    
    func createLocalAnchor(anchorLocation:simd_float4x4!){
        _localAnchor = ARAnchor(transform:anchorLocation)
        self.sceneView.session.add(anchor: _localAnchor!)
        let visual = AnchorVisual()
        visual.identifier = ""
        visual.localAnchor = _localAnchor
        _anchorVisuals[visual.identifier] = visual
        _button!.setTitle("Create Cloud Anchor", for: UIControlState.normal)
        _step = DemoStep.CreateCloudAnchor;
    }
    
    func addButtonAt(_ top: Double,   lines: Double) -> UIButton {
        let wideSize = self.sceneView.bounds.size.width - 20.0
        let result = UIButton()
        result.frame = CGRect(x: 10.0, y: top, width: Double(wideSize), height: lines * 40)
        result.titleLabel?.textColor = UIColor.black
        result.titleLabel?.shadowColor = UIColor.white
        result.backgroundColor = UIColor.lightGray.withAlphaComponent(0.6)
        if (lines > 1) {
            result.titleLabel?.lineBreakMode = NSLineBreakMode.byWordWrapping
        }
        self.sceneView.addSubview(result)
        return result
    }
    
    override func viewWillAppear(_ animated: Bool) {
        super.viewWillAppear(animated)
        
        // Create a session configuration
        let configuration = ARWorldTrackingConfiguration()
        sceneView.debugOptions = ARSCNDebugOptions.showFeaturePoints;
        // Run the view's session
        sceneView.session.run(configuration)
    }
    
    override func viewWillDisappear(_ animated: Bool) {
        super.viewWillDisappear(animated)
        
        // Pause the view's session
        sceneView.session.pause()
    }
    
    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Release any cached data, images, etc that aren't in use.
    }
}
