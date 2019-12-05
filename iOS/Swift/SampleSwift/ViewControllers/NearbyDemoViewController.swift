// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

// This demo creates and saves three anchors. It then locates the third one with its identifier.
// It then looks for anchors near the found anchor (to find the other two).
class NearbyDemoViewController: BaseViewController {

    let numberOfNearbyAnchors = 3    // the number of anchors we will create in the nearby demo
    
    override func onCloudAnchorCreated() {
        if (saveCount < numberOfNearbyAnchors) {
            currentlyPlacingAnchor = true
            
            DispatchQueue.main.async {
                self.mainButton.setTitle("Tap on the screen to create the next Anchor ☝️", for: .normal)
            }
        }
        else {
            ignoreMainButtonTaps = false
            step = .lookForAnchor
            
            DispatchQueue.main.async {
                self.feedbackControl.isHidden = true
                self.mainButton.setTitle("Tap to start next Session & look for Anchor", for: .normal)
            }
        }
    }
    
    override func onLocateAnchorsCompleted() {
        ignoreMainButtonTaps = false
        if (step == .lookForAnchor) {
            step = .lookForNearbyAnchors
            
            DispatchQueue.main.async {
                self.mainButton.setTitle("Anchor found! Tap to locate nearby", for: .normal)
            }
            
        }
        else {
            step = .deleteFoundAnchors
            
            DispatchQueue.main.async {
                self.feedbackControl.isHidden = true
                self.mainButton.setTitle("Anchors found! Tap to delete", for: .normal)
            }
        }
    }
    
    @objc override func mainButtonTap(sender: UIButton) {
        if (ignoreMainButtonTaps) {
            return
        }
        
        switch (step) {
        case .prepare:
            mainButton.setTitle("Tap to start Session", for: .normal)
            step = .createCloudAnchor
        case .createCloudAnchor:
            ignoreMainButtonTaps = true
            currentlyPlacingAnchor = true
            saveCount = 0
            
            startSession()
            
            // When you tap on the screen, touchesBegan will call createLocalAnchor and create a local ARAnchor.
            // We will then put that anchor in the anchorVisuals dictionary with a special key and call CreateCloudAnchor when there is enough data for saving.
            // CreateCloudAnchor will call onCloudAnchorCreated when its async method returns to move to the next step.
            mainButton.setTitle("Tap on the screen to create an Anchor ☝️", for: .normal)
        case .lookForAnchor:
            ignoreMainButtonTaps = true
            stopSession()
            startSession()
            
            // We will get a call to onLocateAnchorsCompleted which will move to the next step when the locate operation completes.
            lookForAnchor()
        case .lookForNearbyAnchors:
            if (anchorVisuals.count == 0) {
                mainButton.setTitle("First Anchor not found yet", for: .normal)
                return
            }
            
            ignoreMainButtonTaps = true

            // We will get a call to onLocateAnchorsCompleted which will move to the next step when the locate operation completes.
            lookForNearbyAnchors()
        case .deleteFoundAnchors:
            ignoreMainButtonTaps = true
            
            // DeleteFoundAnchors will move to the next step when its async method returns
            deleteFoundAnchors()
        case .stopSession:
            stopSession()
            moveToMainMenu()
        default:
            assertionFailure("Demo has somehow entered an invalid state")
        }
    }
}
