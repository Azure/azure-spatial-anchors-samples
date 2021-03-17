// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Foundation;
using System;

namespace SampleXamarin.iOS
{
    [Register(nameof(BasicDemoViewController))]
    public class BasicDemoViewController : StepByStepDemoViewControllerBase
    {
        public BasicDemoViewController(IntPtr handle)
            : base(handle)
        {
            this.OnLocateAnchorsCompleted +=
                (sender, args) => MoveToNextStepAfterLocateAnchorsCompleted();
        }

        public override void OnCloudAnchorCreated()
        {
            this.ignoreMainButtonTaps = false;
            this.step = DemoStep.LocateAnchor;

            this.InvokeOnMainThread(() =>
            {
                this.statusLabel.Hidden = true;
                this.UpdateMainStatusTitle("Tap to start next Session & look for Anchor");
            });
        }

        public void MoveToNextStepAfterLocateAnchorsCompleted()
        {
            this.step = DemoStep.DeleteLocatedAnchors;

            this.InvokeOnMainThread(() =>
            {
                this.ignoreMainButtonTaps = false;
                this.statusLabel.Hidden = true;
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

                        // When you tap on the screen, TouchesBegan will call createLocalAnchor and create a local ARAnchor
                        // We will then put that anchor in the anchorVisuals dictionary with a special key and call CreateCloudAnchor when there is enough data for saving
                        // CreateCloudAnchor will call MoveToNextStepAfterCreateCloudAnchor when its async method returns
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
                case DemoStep.DeleteLocatedAnchors:
                    {
                        this.ignoreMainButtonTaps = true;

                        // DeleteFoundAnchors will move to the next step when its async method returns
                        this.DeleteFoundAnchors();
                        break;
                    }
                case DemoStep.StopSession:
                    {
                        this.StopSession();
                        this.MoveToMainMenu();
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