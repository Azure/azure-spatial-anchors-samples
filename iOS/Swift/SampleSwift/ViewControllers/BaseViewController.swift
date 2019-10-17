// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

import ARKit
import Foundation
import SceneKit
import UIKit

// Colors for the local anchors to indicate status
let readyColor = UIColor.blue.withAlphaComponent(0.3)           // light blue for a local anchor
let savedColor = UIColor.green.withAlphaComponent(0.6)          // green when the cloud anchor was saved successfully
let foundColor = UIColor.yellow.withAlphaComponent(0.6)         // yellow when we successfully located a cloud anchor
let deletedColor = UIColor.black.withAlphaComponent(0.6)        // grey for a deleted cloud anchor
let failedColor = UIColor.red.withAlphaComponent(0.6)           // red when there was an error

// Special dictionary key used to track an unsaved anchor
let unsavedAnchorId = "placeholder-id"

class BaseViewController: UIViewController, ARSCNViewDelegate, ASACloudSpatialAnchorSessionDelegate {
    
    // Set this to the account ID provided for the Azure Spatial Service resource.
    let spatialAnchorsAccountId = "Set me"
    
    // Set this to the account key provided for the Azure Spatial Service resource.
    let spatialAnchorsAccountKey = "Set me"
    
    @IBOutlet var sceneView: ARSCNView!
    
    var mainButton: UIButton!
    var backButton: UIButton!
    var feedbackControl: UIButton!
    var errorControl: UIButton!
    
    var anchorVisuals = [String : AnchorVisual]()
    var cloudSession: ASACloudSpatialAnchorSession? = nil
    var cloudAnchor: ASACloudSpatialAnchor? = nil
    var localAnchor: ARAnchor? = nil
    var localAnchorCube: SCNBox? = nil
    
    var enoughDataForSaving = false     // whether we have enough data to save an anchor
    var currentlyPlacingAnchor = false  // whether we are currently placing an anchor
    var ignoreMainButtonTaps = false    // whether we should ignore taps to wait for current demo step finishing
    var saveCount = 0                   // the number of anchors we have saved to the cloud
    var step = DemoStep.prepare         // the next step to perform
    var targetId : String? = nil        // the cloud anchor identifier to locate
    
    func moveToNextStepAfterCreateCloudAnchor() { assertionFailure("Must be implemented in subclass") }
    
    func moveToNextStepAfterAnchorLocated() { assertionFailure("Must be implemented in subclass") }
    
    // MARK: - View Management
    
    @objc func mainButtonTap(sender: UIButton) { assertionFailure("Must be implemented in subclass") }
    
    @objc func backButtonTap(sender: UIButton) {
        moveToMainMenu()
    }
    
    func moveToMainMenu() {
        self.dismiss(animated: false, completion: nil)
    }
    
    override var prefersStatusBarHidden: Bool {
        return true
    }
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        sceneView.delegate = self              // Set the view's delegate
        sceneView.showsStatistics = false      // Show statistics such as fps and timing information
        sceneView.scene = SCNScene()           // Create a new scene and set it on the view
        
        // Main button
        mainButton = addButtonAt(Double(sceneView.bounds.size.height - 80), lines: Double(1.0))
        mainButton.addTarget(self, action:#selector(mainButtonTap), for: .touchDown)
        
        // Control to go back to the menu screen
        backButton = addButtonAt(20, lines: 1)
        backButton.addTarget(self, action:#selector(backButtonTap), for: .touchDown)
        backButton.backgroundColor = .clear
        backButton.setTitleColor(.blue, for: .normal)
        backButton.contentHorizontalAlignment = .left
        backButton.setTitle("Exit Demo", for: .normal)
        
         // Control to indicate when we can create an anchor
        feedbackControl = addButtonAt(Double(sceneView.bounds.size.height - 40), lines: Double(1.0))
        feedbackControl.backgroundColor = .clear
        feedbackControl.setTitleColor(.yellow, for: .normal)
        feedbackControl.contentHorizontalAlignment = .left
        feedbackControl.isHidden = true
        
        // Control to show errors and verbose text
        errorControl = addButtonAt(Double(sceneView.bounds.size.height - 400), lines: Double(5.0))
        errorControl.isHidden = true
        
        if (spatialAnchorsAccountId == "Set me" || spatialAnchorsAccountKey == "Set me") {
            mainButton.isHidden = true
            errorControl.isHidden = false
            showLogMessage(text: "Set spatialAnchorsAccountId and spatialAnchorsAccountKey in BaseViewController.swift", here: errorControl)
        }
        else {
            // Start the demo
            mainButtonTap(sender: mainButton)
        }
    }
    
    override func viewWillAppear(_ animated: Bool) {
        super.viewWillAppear(animated)
        
        // Create a session configuration
        let configuration = ARWorldTrackingConfiguration()
        sceneView.debugOptions = .showFeaturePoints
        // Run the view's session
        sceneView.session.run(configuration)
    }
    
    override func viewWillDisappear(_ animated: Bool) {
        super.viewWillDisappear(animated)
        
        // Pause the view's session
        sceneView.session.pause()
    }
    
    override func touchesBegan(_ touches: Set<UITouch>, with event: UIEvent?) {
        self.view.endEditing(true)
        super.touchesBegan(touches, with: event)
        
        if (!currentlyPlacingAnchor) {
            return
        }
        
        var anchorLocation = simd_float4x4()
        let touchLocation = touches.first!.location(in: sceneView)
        let hitResultsFeaturePoints: [ARHitTestResult] = sceneView.hitTest(touchLocation, types: .featurePoint)
        if let hit = hitResultsFeaturePoints.first {
            // If we have a feature point create the local anchor there
            anchorLocation = hit.worldTransform
        }
        else if let currentFrame = sceneView.session.currentFrame {
            // Otherwise create the local anchor using the camera's current position
            var translation = matrix_identity_float4x4
            translation.columns.3.z = -0.5 // Put it 0.5 meters in front of the camera
            let transform = simd_mul(currentFrame.camera.transform, translation)
            anchorLocation = transform
        }
        else {
            mainButton.setTitle("Trouble placing anchor. Tap to try again", for: .normal)
            return
        }
    
        createLocalAnchor(anchorLocation: anchorLocation)
    }
    
    // MARK: - ARKit Delegates
    
    // Override to create and configure nodes for anchors added to the view's session.
    func renderer(_ renderer: SCNSceneRenderer, nodeFor anchor: ARAnchor) -> SCNNode? {
        for visual in anchorVisuals.values {
            if (visual.localAnchor == anchor) {
                print("renderer:nodeForAnchor with local anchor \(anchor) at \(BaseViewController.matrixToString(value: anchor.transform))")
                let cube = SCNBox(width: 0.2, height: 0.2, length: 0.2, chamferRadius: 0.0)
                if (visual.identifier != unsavedAnchorId) {
                    cube.firstMaterial?.diffuse.contents = foundColor
                }
                else {
                    cube.firstMaterial?.diffuse.contents = readyColor
                    localAnchorCube = cube
                }
                visual.node = SCNNode(geometry: cube)
                return visual.node
            }
        }
        return nil
    }
    
    func session(_ session: ARSession, didFailWithError error: Error) {
        // Present an error message to the user
        print(error)
    }
    
    func sessionWasInterrupted(_ session: ARSession) {
        // Inform the user that the session has been interrupted, for example, by presenting an overlay
    }
    
    func sessionInterruptionEnded(_ session: ARSession) {
        // Reset tracking and/or remove existing anchors if consistent tracking is required
        if let cloudSession = cloudSession {
            cloudSession.reset()
        }
    }
    
    // MARK: - SceneKit Delegates
    
    func renderer(_ renderer: SCNSceneRenderer, updateAtTime time: TimeInterval) {
        // per-frame scenekit logic
        // modifications don't go through transaction model
        if let cloudSession = cloudSession {
            cloudSession.processFrame(sceneView.session.currentFrame)
            
            if (currentlyPlacingAnchor && enoughDataForSaving && localAnchor != nil) {
                createCloudAnchor()
            }
        }
    }
    
    // MARK: - Azure Spatial Anchors Helper Functions
    
    func startSession() {
        cloudSession = ASACloudSpatialAnchorSession()
        cloudSession!.session = sceneView.session
        cloudSession!.logLevel = .information
        cloudSession!.delegate = self
        cloudSession!.configuration.accountId = spatialAnchorsAccountId
        cloudSession!.configuration.accountKey = spatialAnchorsAccountKey
        cloudSession!.start()
        
        feedbackControl.isHidden = false
        errorControl.isHidden = true
        enoughDataForSaving = false
    }
    
    func createLocalAnchor(anchorLocation: simd_float4x4) {
        if (localAnchor == nil) {
            localAnchor = ARAnchor(transform: anchorLocation)
            sceneView.session.add(anchor: localAnchor!)
            
            // Put the local anchor in the anchorVisuals dictionary with a special key
            let visual = AnchorVisual()
            visual.identifier = unsavedAnchorId
            visual.localAnchor = localAnchor
            anchorVisuals[visual.identifier] = visual
            
            mainButton.setTitle("Create Cloud Anchor (once at 100%)", for: .normal)
        }
    }
    
    func createCloudAnchor() {
        currentlyPlacingAnchor = false
        DispatchQueue.main.async {
            self.mainButton.setTitle("Cloud Anchor being saved...", for: .normal)
        }
        
        cloudAnchor = ASACloudSpatialAnchor()
        cloudAnchor!.localAnchor = localAnchor!
        
        // In this sample app we delete the cloud anchor explicitly, but you can also set it to expire automatically
        let secondsInAWeek = 60 * 60 * 24 * 7
        let oneWeekFromNow = Date(timeIntervalSinceNow: TimeInterval(secondsInAWeek))
        cloudAnchor!.expiration = oneWeekFromNow
        
        cloudSession!.createAnchor(cloudAnchor, withCompletionHandler: { (error: Error?) in
            if let error = error {
                DispatchQueue.main.async {
                    self.mainButton.setTitle("Creation failed", for: .normal)
                    self.errorControl.isHidden = false
                    self.errorControl.setTitle(error.localizedDescription, for: .normal)
                }
                self.localAnchorCube?.firstMaterial?.diffuse.contents = failedColor
            }
            else {
                self.saveCount += 1
                self.localAnchorCube?.firstMaterial?.diffuse.contents = savedColor
                self.targetId = self.cloudAnchor!.identifier
                let visual = self.anchorVisuals[unsavedAnchorId]
                visual?.cloudAnchor = self.cloudAnchor
                visual?.identifier = self.cloudAnchor!.identifier
                self.anchorVisuals[visual!.identifier] = visual
                self.anchorVisuals.removeValue(forKey: unsavedAnchorId)
                self.localAnchor = nil
                
                self.moveToNextStepAfterCreateCloudAnchor()
            }
        })
    }
    
    func stopSession() {
        if let cloudSession = cloudSession {
            cloudSession.stop()
            cloudSession.dispose()
        }
        
        cloudAnchor = nil
        localAnchor = nil
        cloudSession = nil
        
        for visual in anchorVisuals.values {
            visual.node!.removeFromParentNode()
        }
        
        anchorVisuals.removeAll()
    }
    
    func lookForAnchor() {
        let ids = [targetId!]
        let criteria = ASAAnchorLocateCriteria()!
        criteria.identifiers = ids
        cloudSession!.createWatcher(criteria)
        mainButton.setTitle("Locating Anchor ...", for: .normal)
    }
    
    func lookForNearbyAnchors() {
        let criteria = ASAAnchorLocateCriteria()!
        let nearCriteria = ASANearAnchorCriteria()!
        nearCriteria.distanceInMeters = 10
        nearCriteria.sourceAnchor = anchorVisuals[targetId!]!.cloudAnchor
        criteria.nearAnchor = nearCriteria
        cloudSession!.createWatcher(criteria)
        mainButton.setTitle("Locating nearby Anchors ...", for: .normal)
    }
    
    func deleteFoundAnchors() {
        if (anchorVisuals.count == 0) {
            mainButton.setTitle("Anchor not found yet", for: .normal)
            return
        }
        
        mainButton.setTitle("Deleting found Anchor(s) ...", for: .normal)

        for visual in anchorVisuals.values {
            if let visualCloudAnchor = visual.cloudAnchor {
                cloudSession!.delete(visualCloudAnchor, withCompletionHandler: { (error: Error?) in
                    self.ignoreMainButtonTaps = false
                    self.saveCount -= 1
                    
                    if let error = error {
                        visual.node?.geometry?.firstMaterial?.diffuse.contents = failedColor
                        DispatchQueue.main.async {
                            self.errorControl.isHidden = false
                            self.errorControl.setTitle(error.localizedDescription, for: .normal)
                        }
                    }
                    else {
                        visual.node?.geometry?.firstMaterial?.diffuse.contents = deletedColor
                    }
                    
                    if (self.saveCount == 0) {
                        self.step = .stopSession
                        DispatchQueue.main.async {
                            self.mainButton.setTitle("Cloud Anchor(s) deleted. Tap to stop Session", for: .normal)
                        }
                    }
                })
            }
        }
    }
    
    // MARK: - ASACloudSpatialAnchorSession Delegates
    
    internal func onLogDebug(_ cloudSpatialAnchorSession: ASACloudSpatialAnchorSession!, _ args: ASAOnLogDebugEventArgs!) {
        if let message = args.message {
            print(message)
        }
    }
    
//    // You can use this helper method to get an authentication token via Azure Active Directory.
//    internal func tokenRequired(_ cloudSpatialAnchorSession: ASACloudSpatialAnchorSession!, _ args: ASATokenRequiredEventArgs!) {
//        let deferral = args.getDeferral()
//        // AAD user token scenario to get an authentication token
//        AuthenticationHelper.acquireAuthenticationToken { (token: String?, error: Error?) in
//            if error != nil {
//                let errMessage = error!.localizedDescription
//                DispatchQueue.main.async {
//                    self.errorControl.isHidden = false
//                    self.errorControl.setTitle(errMessage, for: .normal)
//                }
//            }
//            if token != nil {
//                args.authenticationToken = token
//            }
//            deferral?.complete()
//        }
//    }
    
    internal func anchorLocated(_ cloudSpatialAnchorSession: ASACloudSpatialAnchorSession!, _ args: ASAAnchorLocatedEventArgs!) {
        let status = args.status
        switch (status) {
        case .alreadyTracked:
            // Ignore if we were already handling this.
            break
        case .located:
            let anchor = args.anchor
            print("Cloud Anchor found! Identifier: \(anchor!.identifier ?? "nil"). Location: \(BaseViewController.matrixToString(value: anchor!.localAnchor.transform))")
            let visual = AnchorVisual()
            visual.cloudAnchor = anchor
            visual.identifier = anchor!.identifier
            visual.localAnchor = anchor!.localAnchor
            anchorVisuals[visual.identifier] = visual
            sceneView.session.add(anchor: anchor!.localAnchor)
        case .notLocatedAnchorDoesNotExist:
            break
        case .notLocated:
            break
        }
    }
    
    internal func locateAnchorsCompleted(_ cloudSpatialAnchorSession: ASACloudSpatialAnchorSession!, _ args: ASALocateAnchorsCompletedEventArgs!) {
        print("Anchor locate operation completed completed for watcher with identifier: \(args.watcher!.identifier)")
        ignoreMainButtonTaps = false
        moveToNextStepAfterAnchorLocated()
    }
    
    internal func sessionUpdated(_ cloudSpatialAnchorSession: ASACloudSpatialAnchorSession!, _ args: ASASessionUpdatedEventArgs!) {
        let status = args.status!
        let message = BaseViewController.statusToString(status: status, step: step)
        enoughDataForSaving = status.recommendedForCreateProgress >= 1.0
        showLogMessage(text: message, here: feedbackControl)
    }
    
    internal func error (_ cloudSpatialAnchorSession: ASACloudSpatialAnchorSession!, _ args: ASASessionErrorEventArgs!) {
        if let errorMessage = args.errorMessage {
            DispatchQueue.main.async {
                self.errorControl.isHidden = false
            }
            showLogMessage(text: errorMessage, here: errorControl)
            print("Error code: \(args.errorCode), message: \(errorMessage)")
        }
    }
    
    // MARK: - UI Helpers
    
    func addButtonAt(_ top: Double, lines: Double) -> UIButton {
        let wideSize = sceneView.bounds.size.width - 20.0
        let result = UIButton(type: .system)
        result.frame = CGRect(x: 10.0, y: top, width: Double(wideSize), height: lines * 40)
        result.setTitleColor(.black, for: .normal)
        result.setTitleShadowColor(.white, for: .normal)
        result.backgroundColor = UIColor.lightGray.withAlphaComponent(0.6)
        if (lines > 1) {
            result.titleLabel?.lineBreakMode = NSLineBreakMode.byWordWrapping
        }
        sceneView.addSubview(result)
        return result
    }
    
    func showLogMessage(text: String, here: UIView!) {
        let button = here as? UIButton
        let textField = here as? UITextField
        
        DispatchQueue.main.async {
            button?.setTitle(text, for: .normal)
            textField?.text = text
        }
    }

    // MARK: - Formatting Helpers
    
    static func matrixToString(value: matrix_float4x4) -> String {
        return String.init(format: "[[%.3f %.3f %.3f %.3f] [%.3f %.3f %.3f %.3f] [%.3f %.3f %.3f %.3f] [%.3f %.3f %.3f %.3f]]",
                           value.columns.0[0], value.columns.1[0], value.columns.2[0], value.columns.3[0],
                           value.columns.0[1], value.columns.1[1], value.columns.2[1], value.columns.3[1],
                           value.columns.0[2], value.columns.1[2], value.columns.2[2], value.columns.3[2],
                           value.columns.0[3], value.columns.1[3], value.columns.2[3], value.columns.3[3])
    }
    
    static func statusToString(status: ASASessionStatus, step: DemoStep) -> String {
        let feedback = feedbackToString(userFeedback: status.userFeedback)
        
        if (step == .createCloudAnchor) {
            let progress = status.recommendedForCreateProgress
            return String.init(format: "%.0f%% progress. %@", progress * 100, feedback)
        }
        else {
            return feedback
        }
    }
    
    static func feedbackToString(userFeedback: ASASessionUserFeedback) -> String {
        if (userFeedback == .notEnoughMotion) {
            return ("Not enough motion.")
        }
        else if (userFeedback == .motionTooQuick) {
            return ("Motion is too quick.")
        }
        else if (userFeedback == .notEnoughFeatures) {
            return ("Not enough features.")
        }
        else {
            return "Keep moving! 🤳"
        }
    }
}
