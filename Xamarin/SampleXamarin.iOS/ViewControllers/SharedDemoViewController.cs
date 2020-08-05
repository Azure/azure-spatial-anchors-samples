// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using CoreGraphics;
using Foundation;
using Microsoft.Azure.SpatialAnchors;
using SampleXamarin.AnchorSharing;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UIKit;

namespace SampleXamarin.iOS
{
    [Register(nameof(SharedDemoViewController))]
    public class SharedDemoViewController : DemoViewControllerBase
    {
        private readonly string anchorId = string.Empty; //for searching for cloudAnchor
        private readonly AnchorSharingServiceClient anchorSharingServiceClient; //cloud saving and retrieving client

        public string mainLabelText = string.Empty;
        private readonly UILabel mainLabel = new UILabel();
        private readonly UIButton createButton = new UIButton(UIButtonType.System);
        private readonly UIButton locateButton = new UIButton(UIButtonType.System);
        private readonly UILabel anchorIdLabel = new UILabel();
        private readonly UITextField anchorIdEntry = new UITextField();

        public SharedDemoViewController(IntPtr handle)
            : base(handle)
        {
            this.anchorSharingServiceClient = new AnchorSharingServiceClient(AccountDetails.AnchorSharingServiceUrl);

            this.OnLocateAnchorsCompleted +=
                (sender, args) => MoveToNextStepAfterLocateAnchorsCompleted();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.createButton.SetTitle("Create", UIControlState.Normal);
            this.createButton.Frame = new CGRect(10, this.View.Frame.Height*0.87, (this.View.Frame.Width - 20) / 2, 44);
            this.createButton.BackgroundColor = UIColor.LightGray.ColorWithAlpha((nfloat)0.6);
            this.createButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            this.createButton.TouchUpInside += (sender, e) => this.CreateButtonTap();

            this.locateButton.SetTitle("Locate", UIControlState.Normal);
            this.locateButton.Frame = new CGRect((10 + this.View.Frame.Width / 2), this.View.Frame.Height*0.87, (this.View.Frame.Width - 20) / 2 - 10, 44);
            this.locateButton.BackgroundColor = UIColor.LightGray.ColorWithAlpha((nfloat)0.6);
            this.locateButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            this.locateButton.Hidden = false;
            this.locateButton.TouchUpInside += (sender, e) => this.LocateButtonTapAsync();

            this.statusLabel.Text = "....";
            this.statusLabel.TextAlignment = UITextAlignment.Left;
            this.statusLabel.TextColor = UIColor.White;
            this.statusLabel.Frame = new CGRect(10, this.View.Frame.Height - 50, this.View.Frame.Width - 20, 44);
            this.statusLabel.Hidden = this.statusLabelIsHidden;

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

            if (string.IsNullOrWhiteSpace(AccountDetails.AnchorSharingServiceUrl) || AccountDetails.AnchorSharingServiceUrl == "Set me")
            {
                this.ShowLogMessage($"Set {nameof(AccountDetails.AnchorSharingServiceUrl)} in {nameof(AccountDetails)}.cs.", SubView.ErrorView);
            }

            this.ShowCreateOrLocateMenu();
        }

        private void ShowCreateOrLocateMenu()
        {
            this.step = DemoStep.Start;
            this.InvokeOnMainThread(() =>
            {
                this.mainLabel.Hidden = false;
                this.createButton.Hidden = false;
                this.locateButton.Hidden = false;
            });
        }

        private async Task LocateButtonTapAsync()
        {
            Debug.WriteLine("CURRENT STEP VALUE :: " + this.step);
            if (this.step == DemoStep.Start)
            {
                this.step = DemoStep.EnterAnchorNumber;
                this.InvokeOnMainThread(() =>
                {
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
            this.currentlyPlacingAnchor = true;
            this.saveCount = 0;
            this.createButton.Hidden = true;
            this.locateButton.Hidden = true;
            this.StartSession();

            this.UpdateMainStatusTitle("Tap on the screen to Create an Anchor");
        }

        public override void OnCloudAnchorCreated()
        {
            this.StopSession();
        }

        public void MoveToNextStepAfterLocateAnchorsCompleted()
        {
            this.UpdateMainStatusTitle("Anchor Found!");
            this.ShowCreateOrLocateMenu();
        }

        public override void UpdateMainStatusTitle(string title)
        {
            this.InvokeOnMainThread(() => {
                this.mainLabel.Text = title;
                this.mainLabel.Hidden = false;
            });
        }

        public void UpdateStatus(string status)
        {
            this.InvokeOnMainThread(() => {
                this.statusLabel.Text = status;
                this.statusLabel.Hidden = false;
            });
        }

        public override void HideMainStatus()
        {
            this.mainLabel.Hidden = true;
        }

        protected override void AnchorSaveSuccess(CloudSpatialAnchor result)
        {
            base.AnchorSaveSuccess(result);

            Debug.WriteLine("ASADemo", "recording anchor with web service");
            Debug.WriteLine("ASADemo", "anchorId: " + result.Identifier);

            Task.Run(async () =>
            {
                try
                {
                    SendAnchorResponse sendResult = await this.SendtoSharingServiceAsync(result.Identifier);
                    this.UpdateStatus("Create Success!!");
                    this.UpdateMainStatusTitle("Anchor Number: " + sendResult.AnchorNumber);
                    this.ShowCreateOrLocateMenu();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to save the anchor to the sharing service: '{ex.Message}'.");
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
