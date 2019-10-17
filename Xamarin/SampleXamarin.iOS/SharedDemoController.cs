// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using CoreGraphics;
using Microsoft.Azure.SpatialAnchors;
using SampleXamarin.AnchorSharing;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UIKit;

namespace SampleXamarin.iOS
{
    public class ShareDemoController : DemoControllerBase
    {
        private readonly string anchorId = string.Empty; //for searching for cloudAnchor
        private readonly AnchorSharingServiceClient anchorSharingServiceClient; //cloud saving and retreiving client

        public string mainLabelText = string.Empty;
        private readonly UILabel mainLabel = new UILabel();
        private readonly UIButton createButton = new UIButton(UIButtonType.System);
        private readonly UIButton locateButton = new UIButton(UIButtonType.System);
        private readonly UILabel anchorIdLabel = new UILabel();
        private readonly UITextField anchorIdEntry = new UITextField();

        public ShareDemoController()
        {
            this.anchorSharingServiceClient = new AnchorSharingServiceClient(AccountDetails.AnchorSharingServiceUrl);
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.createButton.SetTitle("Create", UIControlState.Normal);
            this.createButton.Frame = new CGRect(10, this.View.Frame.Height - 90, (this.View.Frame.Width - 20) / 2, 44);
            this.createButton.BackgroundColor = UIColor.LightGray.ColorWithAlpha((nfloat)0.6);
            this.createButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            this.createButton.TouchUpInside += (sender, e) => this.CreateButtonTap();

            this.locateButton.SetTitle("Locate", UIControlState.Normal);
            this.locateButton.Frame = new CGRect((10 + this.View.Frame.Width / 2), this.View.Frame.Height - 90, (this.View.Frame.Width - 20) / 2 - 10, 44);
            this.locateButton.BackgroundColor = UIColor.LightGray.ColorWithAlpha((nfloat)0.6);
            this.locateButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            this.locateButton.Hidden = false;
            this.locateButton.TouchUpInside += (sender, e) => this.LocateButtonTapAsync();

            this.statusLabel.Text = "....";
            this.statusLabel.TextAlignment = UITextAlignment.Left;
            this.statusLabel.TextColor = UIColor.White;
            this.statusLabel.Frame = new CGRect(10, this.View.Frame.Height - 50, this.View.Frame.Width - 20, 44);
            this.statusLabel.Hidden = this.statusLabelisHidden;

            this.mainLabel.Text = "Welcome to Azure Spatial Anchors Shard Anchors Demo \n Choose Create or Locate";
            this.mainLabel.TextAlignment = UITextAlignment.Center;
            this.mainLabel.LineBreakMode = UILineBreakMode.WordWrap;
            this.mainLabel.Lines = 3;
            this.mainLabel.TextColor = UIColor.Yellow;
            this.mainLabel.Frame = new CGRect(10, 150, this.View.Frame.Width - 20, 40);

            this.anchorIdLabel.Text = "Enter Anchor ID Value:";
            this.anchorIdLabel.BackgroundColor = UIColor.LightGray;
            this.anchorIdLabel.TextAlignment = UITextAlignment.Left;
            this.anchorIdLabel.TextColor = UIColor.White;
            this.anchorIdLabel.Frame = new CGRect(10, 150, this.View.Frame.Width - 20, 40);
            this.anchorIdLabel.Hidden = true;

            this.anchorIdEntry.Frame = new CGRect(10, 200, this.View.Frame.Width - 20, 44);
            this.anchorIdEntry.TextAlignment = UITextAlignment.Left;
            this.anchorIdEntry.MinimumFontSize = 17f;
            this.anchorIdEntry.AdjustsFontSizeToFitWidth = true;
            this.anchorIdEntry.ReturnKeyType = UIReturnKeyType.Done;
            this.anchorIdEntry.BackgroundColor = UIColor.White;
            this.anchorIdEntry.KeyboardType = UIKeyboardType.NumberPad;
            this.anchorIdEntry.Hidden = true;

            this.View.AddSubview(this.mainLabel);
            this.View.AddSubview(this.createButton);
            this.View.AddSubview(this.locateButton);
            this.View.AddSubview(this.anchorIdLabel);
            this.View.AddSubview(this.anchorIdEntry);
        }

        private async Task LocateButtonTapAsync()
        {
            Debug.WriteLine("CURRENT STEP VALUE :: " + this.step);
            if (this.step == DemoStep.Start)
            {
                this.step = DemoStep.EnterAnchorNumber;
                this.InvokeOnMainThread(() =>
                {
                    this.ignoreMainButtonTaps = true;
                    this.createButton.Hidden = true;
                    this.locateButton.Hidden = false;
                    this.anchorIdEntry.Hidden = false;
                    this.anchorIdLabel.Hidden = false;
                });
            }
            else
            {
                string inputVal = this.anchorIdEntry.Text;
                this.anchorIdEntry.Hidden = true;
                this.anchorIdLabel.Hidden = true;
                this.locateButton.Hidden = true;
                this.StartSession();
                if (!string.IsNullOrEmpty(inputVal))
                {
                    RetrieveAnchorResponse response = await this.anchorSharingServiceClient.RetrieveAnchorIdAsync(inputVal);

                    Debug.WriteLine("RESPONSE VALUE :: " + response.AnchorId + " ANCHOR Found :: " + response.AnchorFound);

                    if (response.AnchorFound)
                    {
                        this.LookForAnchor(response.AnchorId);
                    }
                    else
                    {
                        this.step = DemoStep.Start;
                        this.anchorIdEntry.Hidden = true;
                        this.anchorIdLabel.Hidden = true;
                        this.UpdateMainStatusTitle("Anchor number not found or has expired.");
                    }

                    this.step = DemoStep.LocateAnchor;
                    this.UpdateMainStatusTitle("Locating Anchor..");
                }
            }
        }

        private void CreateButtonTap()
        {
            this.step = DemoStep.CreateAnchor;
            this.ignoreMainButtonTaps = true;
            this.currentlyPlacingAnchor = true;
            this.saveCount = 0;
            this.createButton.Hidden = true;
            this.locateButton.Hidden = true;
            this.StartSession();

            this.UpdateMainStatusTitle("Tap on the screen to Create an Anchor");
        }

        public override void MoveToNextStepAfterCreateCloudAnchor()
        {
            this.ignoreMainButtonTaps = false;

            this.InvokeOnMainThread(() =>
            {
                this.step = DemoStep.Start;
                this.statusLabel.Text = "Create Success!!";
                this.StopSession();
            });
        }

        public override void MoveToNextStepAfterAnchorLocated()
        {
            this.InvokeOnMainThread(() =>
            {
                this.mainLabel.Text = "Anchor Found!";
                this.statusLabel.Hidden = true;
            });
        }

        public override void UpdateMainStatusTitle(string title)
        {
            this.InvokeOnMainThread(() => this.mainLabel.Text = title);
        }

        public override void HideMainStatus()
        {
            this.mainLabel.Hidden = true;
        }

        protected override void AnchorSaveSuccess(CloudSpatialAnchor result)
        {
            base.AnchorSaveSuccess(result);

            Debug.WriteLine("ASADemo", "recording anchor with web service");
            Debug.WriteLine("ASADemo", "anchorId: " + this.anchorId);

            Task.Run(async () =>
            {
                try
                {
                    SendAnchorResponse sendResult = await this.SendtoSharingServiceAsync(this.cloudAnchor.Identifier);
                    this.UpdateMainStatusTitle("Anchor Number: " + sendResult.AnchorNumber);
                    this.MoveToNextStepAfterCreateCloudAnchor();
                }
                catch (Exception ex)
                {
                    this.AnchorSaveFailed(ex.Message);
                }
            });
        }

        public async Task<SendAnchorResponse> SendtoSharingServiceAsync(string anchorId)
        {
            SendAnchorResponse response = null;

            if (anchorId == null)
            {
                throw new ArgumentException("The anchorId was null");
            }

            try
            {
                response = await this.anchorSharingServiceClient.SendAnchorIdAsync(anchorId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return response;
        }
    }
}
