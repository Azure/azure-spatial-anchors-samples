// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace SampleXamarin.iOS
{
    public class BasicDemoView : BasicNearbyControllerBase
    {
        public BasicDemoView()
        {
            this.UpdateMainStatusTitle("Tap to start Session");
            this.step = DemoStep.CreateAnchor;
        }

        public override void MoveToNextStepAfterCreateCloudAnchor()
        {
            this.ignoreMainButtonTaps = false;
            this.step = DemoStep.LocateAnchor;

            this.InvokeOnMainThread(() =>
            {
                this.statusLabelisHidden = true;
                this.UpdateMainStatusTitle("Tap to start next Session & look for Anchor");
            });
        }

        public override void MoveToNextStepAfterAnchorLocated()
        {
            this.step = DemoStep.DeleteLocatedANchors;

            this.InvokeOnMainThread(() =>
            {
                this.statusLabelisHidden = true;
                this.UpdateMainStatusTitle("Anchor found! Tap to delete");
            });
        }

        public override void MainButtonTap()
        {
            if (this.ignoreMainButtonTaps)
            {
                return;
            }

            switch (this.step)
            {
                case DemoStep.Start:
                    {
                        this.UpdateMainStatusTitle("Tap to start Session");
                        this.step = DemoStep.CreateAnchor;
                        break;
                    }
                case DemoStep.CreateAnchor:
                    {
                        this.ignoreMainButtonTaps = true;
                        this.currentlyPlacingAnchor = true;
                        this.saveCount = 0;

                        this.StartSession();

                        // When you tap on the screen, touchesBegan will call createLocalAnchor and create a local ARAnchor
                        // We will then put that anchor in the anchorVisuals dictionary with a special key and call CreateCloudAnchor when there is enough data for saving
                        // CreateCloudAnchor will call moveToNextStepAfterCreateCloudAnchor when its async method returns
                        this.UpdateMainStatusTitle("Tap on the screen to create an Anchor ☝️");
                        break;
                    }
                case DemoStep.LocateAnchor:
                    {
                        this.ignoreMainButtonTaps = true;
                        this.StopSession();
                        this.StartSession();

                        // We will get a call to locateAnchorsCompleted when locate operation completes, which will call moveToNextStepAfterAnchorLocated
                        this.LookForAnchor(this.targetId);
                        break;
                    }
                case DemoStep.DeleteLocatedANchors:
                    {
                        this.ignoreMainButtonTaps = true;

                        // DeleteFoundAnchors will move to the next step when its async method returns
                        this.DeleteFoundAnchors();
                        break;
                    }
                case DemoStep.StopSession:
                    {
                        this.StopSession();
                        this.NavigationController.PopViewController(true);
                        break;
                    }

                default:
                    {
                        this.ShowLogMessage("Demo has somehow entered an invalid state", SubView.ErrorView);
                        break;
                    }
            }
        }
    }
}