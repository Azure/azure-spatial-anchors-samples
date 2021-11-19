﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using CoreGraphics;
using Microsoft.Azure.SpatialAnchors;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UIKit;

namespace SampleXamarin.iOS
{
    public abstract class StepByStepDemoViewControllerBase : DemoViewControllerBase
    {
        protected bool ignoreMainButtonTaps;
        public bool mainButtonisHidden = false;
        private readonly UIButton mainButton = new UIButton(UIButtonType.System);

        protected StepByStepDemoViewControllerBase(IntPtr handle)
            : base(handle)
        {
        }

        public abstract void MainButtonTap();

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();


            int buttonHeight = (int)(0.05f * Math.Max(this.View.Frame.Width, this.View.Frame.Height)); // 5% of larger screen dimension
            int mainButtonYValue = (int)(this.View.Frame.Height) - borderSize - buttonHeight;
            this.mainButton.SetTitle("Tap to start Session", UIControlState.Normal);
            this.mainButton.Frame = new CGRect(borderSize, mainButtonYValue, this.View.Frame.Width - 2 * borderSize, buttonHeight);
            this.mainButton.BackgroundColor = UIColor.LightGray.ColorWithAlpha((nfloat)0.6);
            this.mainButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            this.mainButton.Hidden = this.mainButtonisHidden;
            this.mainButton.TouchUpInside += (sender, e) => this.MainButtonTap();

            this.View.AddSubview(this.mainButton);

            // Start the demo
            this.MainButtonTap();
        }

        public override void ViewWillLayoutSubviews()
        {
            base.ViewWillLayoutSubviews();

            int buttonHeight = (int)(this.mainButton.Frame.Height);
            this.mainButton.Frame = new CGRect(borderSize, this.View.Frame.Height - borderSize - buttonHeight, this.View.Frame.Width - 2 * borderSize, buttonHeight);
        }

        public override void UpdateMainStatusTitle(string title)
        {
            this.InvokeOnMainThread(() => this.mainButton.SetTitle(title, UIControlState.Normal));
        }

        public override void HideMainStatus()
        {
            this.mainButton.Hidden = true;
        }

        public void LookForNearbyAnchors()
        {
            AnchorLocateCriteria criteria = new AnchorLocateCriteria();
            NearAnchorCriteria nearCriteria = new NearAnchorCriteria
            {
                DistanceInMeters = 10,
                SourceAnchor = this.anchorVisuals[this.targetId].cloudAnchor
            };
            criteria.NearAnchor = nearCriteria;

            // Cannot run more than one watcher concurrently
            this.StopLocating();

            this.cloudSession.CreateWatcher(criteria);

            this.UpdateMainStatusTitle("Locating Nearby Anchors ....");
        }

        public void DeleteFoundAnchors()
        {
            if (this.anchorVisuals.Count == 0)
            {
                this.UpdateMainStatusTitle("Anchors Not Found Yet ....");
                return;
            }

            this.UpdateMainStatusTitle("Deleting Found Anchors ....");

            int test = 0;
            foreach (AnchorVisual visual in this.anchorVisuals.Values)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        Debug.WriteLine("anchorVisuals CHECK :: " + visual.identifier + "index :: " + (test + 1));
                        await this.cloudSession.DeleteAnchorAsync(visual.cloudAnchor);
                        this.AnchorDeleteSuccess(visual);

                        if (this.saveCount == 0)
                        {
                            this.step = DemoStep.StopSession;
                            this.UpdateMainStatusTitle("Cloud Anchor(s) deleted.Tap to stop Session");
                        }
                    }
                    catch (CloudSpatialException ex)
                    {
                        this.AnchorDeleteFailed($"{ex.Message}, {ex.ErrorCode}");
                    }
                    catch (Exception ex)
                    {
                        this.AnchorDeleteFailed(ex.Message);
                    }
                });
            }
        }

        private void AnchorDeleteSuccess(AnchorVisual visual)
        {
            this.saveCount -= 1;
            this.ignoreMainButtonTaps = false;
            Debug.WriteLine("LOCAL ANCHOR:: " + visual.identifier);
            visual.node.Geometry.FirstMaterial.Diffuse.Contents = this.deletedColor;
        }

        private void AnchorDeleteFailed(string message)
        {
            this.InvokeOnMainThread(() =>
            {
                this.mainButton.SetTitle("Deletion Failed", UIControlState.Normal);
                this.errorLabel.Hidden = false;
                this.errorLabel.Text = message;
            });
            this.localAnchorCube.FirstMaterial.Diffuse.Contents = this.failedColor;
        }
    }
}