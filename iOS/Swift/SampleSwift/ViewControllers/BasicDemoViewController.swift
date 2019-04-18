// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

// This demo creates and saves an anchor. It then locates it with its identifier.
class BasicDemoViewController: BaseViewController {
    
    override func moveToNextStepAfterCreateCloudAnchor() {
        ignoreMainButtonTaps = false
        step = .lookForAnchor
        
        DispatchQueue.main.async {
            self.feedbackControl.isHidden = true
            self.mainButton.setTitle("Tap to start next Session & look for Anchor", for: .normal)
        }
    }
    
    override func moveToNextStepAfterAnchorLocated() {
        step = .deleteFoundAnchors
        
        DispatchQueue.main.async {
            self.feedbackControl.isHidden = true
            self.mainButton.setTitle("Anchor found! Tap to delete", for: .normal)
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
            
            // When you tap on the screen, touchesBegan will call createLocalAnchor and create a local ARAnchor
            // We will then put that anchor in the anchorVisuals dictionary with a special key and call CreateCloudAnchor when there is enough data for saving
            // CreateCloudAnchor will call moveToNextStepAfterCreateCloudAnchor when its async method returns
            mainButton.setTitle("Tap on the screen to create an Anchor ☝️", for: .normal)
        case .lookForAnchor:
            ignoreMainButtonTaps = true
            stopSession()
            startSession()
            
            // We will get a call to locateAnchorsCompleted when locate operation completes, which will call moveToNextStepAfterAnchorLocated
            lookForAnchor()
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
