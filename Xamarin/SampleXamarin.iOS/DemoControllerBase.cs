// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ARKit;
using CoreGraphics;
using Foundation;
using Microsoft.Azure.SpatialAnchors;
using OpenTK;
using SceneKit;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UIKit;

namespace SampleXamarin.iOS
{
    public abstract class DemoControllerBase : UIViewController
    {
        public ARSCNView scnView;

        protected UIColor readyColor = UIColor.Blue.ColorWithAlpha((nfloat)0.6);
        protected UIColor savedColor = UIColor.Green.ColorWithAlpha((nfloat)0.6);
        protected UIColor foundColor = UIColor.Yellow.ColorWithAlpha((nfloat)0.6);
        protected UIColor deletedColor = UIColor.Black.ColorWithAlpha((nfloat)0.6);
        protected UIColor failedColor = UIColor.Red.ColorWithAlpha((nfloat)0.6);

        protected string unsavedAnchorId = "placeholder-id";

        public string statusLabelText = string.Empty;
        public string errorLabelText = string.Empty;
        public bool errorLabelisHidden = true;
        public bool statusLabelisHidden = true;
        protected UILabel statusLabel = new UILabel();
        protected UILabel errorLabel = new UILabel();

        public bool enoughDataForSaving;
        public bool currentlyPlacingAnchor;
        public bool ignoreMainButtonTaps;
        public int saveCount;
        public DemoStep step;
        protected string targetId = string.Empty;

        public ConcurrentDictionary<string, AnchorVisual> anchorVisuals = new ConcurrentDictionary<string, AnchorVisual>(StringComparer.OrdinalIgnoreCase);
        public CloudSpatialAnchorSession cloudSession;
        public CloudSpatialAnchor cloudAnchor;
        public ARAnchor localAnchor;
        public SCNBox localAnchorCube;

        public event EventHandler<AnchorLocatedEventArgs> OnAnchorLocated;

        public event EventHandler<LocateAnchorsCompletedEventArgs> OnLocateAnchorsCompleted;

        public event EventHandler<OnLogDebugEventArgs> OnLogDebug;

        public event EventHandler<SessionErrorEventArgs> OnSessionError;

        public event EventHandler<SessionUpdatedEventArgs> OnSessionUpdated;

        public virtual bool ReportProgress => this.step == DemoStep.CreateAnchor;

        protected DemoControllerBase()
        {
        }

        public abstract void MoveToNextStepAfterCreateCloudAnchor();

        public abstract void MoveToNextStepAfterAnchorLocated();

        public override bool ShouldAutorotate() => true;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.statusLabel.Text = "....";
            this.statusLabel.TextAlignment = UITextAlignment.Left;
            this.statusLabel.TextColor = UIColor.White;
            this.statusLabel.Frame = new CGRect(10, this.View.Frame.Height - 50, this.View.Frame.Width - 20, 44);
            this.statusLabel.Hidden = this.statusLabelisHidden;

            this.errorLabel.Text = this.errorLabelText;
            this.errorLabel.TextColor = UIColor.White;
            this.errorLabel.Frame = new CGRect(10, this.View.Frame.Height - 500, this.View.Frame.Width - 20, 200);
            this.errorLabel.LineBreakMode = UILineBreakMode.WordWrap;
            this.errorLabel.Lines = 10;
            this.errorLabel.Hidden = this.errorLabelisHidden;

            UIButton backButton = new UIButton
            {
                Frame = new CGRect(10, 20, 100, 44)
            };
            backButton.SetTitle("Exit Demo", UIControlState.Normal);
            backButton.SetTitleColor(UIColor.Blue, UIControlState.Normal);
            backButton.TouchUpInside += (sender, e) => this.NavigationController.PopViewController(true);

            this.scnView = new ARSCNView()
            {
                Frame = this.View.Frame,
                Delegate = new ARDelegate(this),
                DebugOptions = ARSCNDebugOptions.ShowFeaturePoints,
                UserInteractionEnabled = true,
            };

            this.View.AddSubview(this.scnView);
            this.View.AddSubview(backButton);
            this.View.AddSubview(this.errorLabel);
            this.View.AddSubview(this.statusLabel);

            if (string.IsNullOrWhiteSpace(AccountDetails.SpatialAnchorsAccountId) || AccountDetails.SpatialAnchorsAccountId == "Set me"
                    || string.IsNullOrWhiteSpace(AccountDetails.SpatialAnchorsAccountKey) || AccountDetails.SpatialAnchorsAccountKey == "Set me")
            {
                this.ShowLogMessage($"Set {nameof(AccountDetails.SpatialAnchorsAccountId)} and {nameof(AccountDetails.SpatialAnchorsAccountKey)} in {nameof(AccountDetails)}.cs.", SubView.ErrorView);
            }
        }

        public void HideStatusLabel(bool isHidden)
        {
            this.InvokeOnMainThread(() => this.statusLabel.Hidden = isHidden);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            //ADD BACK BUTTON THEN UNCOMMENT THIS
            this.NavigationController.SetNavigationBarHidden(true, true);

            // Configure ARKit
            ARWorldTrackingConfiguration config = new ARWorldTrackingConfiguration
            {
                PlaneDetection = ARPlaneDetection.Horizontal
            };

            // This method is called subsequent to `ViewDidLoad` so we know `scnView` is instantiated
            this.scnView.Session.Run(config, ARSessionRunOptions.RemoveExistingAnchors);
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            this.scnView.Session.Pause();
        }

        public abstract void UpdateMainStatusTitle(string title);

        public abstract void HideMainStatus();

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            this.View.EndEditing(true);
            base.TouchesBegan(touches, evt);

            if (!this.currentlyPlacingAnchor)
            {
                return;
            }

            UITouch touch = touches.AnyObject as UITouch;
            if (touch != null)
            {
                CGPoint touchLocation = touch.LocationInView(this.scnView);
                if (this.TryHitTestFromTouchPoint(touchLocation, out NMatrix4 worldTransform))
                {
                    this.CreateLocalAnchor(ref worldTransform);
                }
                else
                {
                    this.UpdateMainStatusTitle("Trouble placing anchor. Please try again");
                }
            }
            else
            {
                this.UpdateMainStatusTitle("Trouble placing anchor. Please try again");
            }
        }

        private bool TryHitTestFromTouchPoint(CGPoint pt, out NMatrix4 worldTransform)
        {
            //Hit test against existing anchors
            ARHitTestResult[] hits = this.scnView.HitTest(pt, ARHitTestResultType.FeaturePoint);
            if (hits != null && hits.Length > 0)
            {
                ARHitTestResult hit = hits.FirstOrDefault();

                if (hit != null)
                {
                    worldTransform = hit.WorldTransform;
                    return true;
                }
            }

            worldTransform = default;

            return false;
        }

        #region Azure Spatial Anchors Helper Functions

        public void StartSession()
        {
            this.cloudSession = new CloudSpatialAnchorSession
            {
                Session = this.scnView.Session,
                LogLevel = SessionLogLevel.Information
            };
            this.cloudSession.Configuration.AccountId = AccountDetails.SpatialAnchorsAccountId;
            this.cloudSession.Configuration.AccountKey = AccountDetails.SpatialAnchorsAccountKey;

            //Delegate events hook here
            this.cloudSession.OnLogDebug += this.SpatialCloudSession_LogDebug;
            this.cloudSession.Error += this.SpatialAnchorsSession_Error;
            this.cloudSession.AnchorLocated += this.SpatialAnchorsSession_AnchorLocated;
            this.cloudSession.LocateAnchorsCompleted += this.SpatialAnchorsSession_LocateAnchorsCompleted;
            this.cloudSession.SessionUpdated += this.SpatialAnchorsSession_SessionUpdated;

            this.cloudSession.Start();
            this.statusLabelisHidden = false;
            this.errorLabelisHidden = true;
            this.enoughDataForSaving = false;
        }

        private SCNNode PlaceCube(ARAnchor anchor)
        {
            AnchorVisual visual = this.anchorVisuals.Values.FirstOrDefault(a => a.localAnchor == anchor);

            if (visual is null)
            {
                return null;
            }

            Debug.WriteLine($"renderer:nodeForAnchor with local anchor {anchor} at {anchor.Transform}");
            SCNBox box = new SCNBox { Width = 0.2f, Height = 0.2f, Length = 0.2f };

            if (visual.identifier != this.unsavedAnchorId)
            {
                box.FirstMaterial.Diffuse.Contents = this.foundColor;
            }
            else
            {
                box.FirstMaterial.Diffuse.Contents = this.readyColor;
            }

            this.localAnchorCube = box;
            SCNNode cubeNode = new SCNNode
            {
                Position = visual.localAnchor.Transform.ToPosition(),
                Geometry = box
            };
            this.scnView.Scene.RootNode.AddChildNode(cubeNode);
            visual.node = cubeNode;

            return visual.node;
        }

        public void CreateLocalAnchor(ref NMatrix4 transform)
        {
            if (this.localAnchor != null)
            {
                return;
            }

            this.localAnchor = new ARAnchor(transform);
            this.scnView.Session.AddAnchor(this.localAnchor);

            // Put the local anchor in the anchorVisuals list with a special key
            AnchorVisual visual = new AnchorVisual
            {
                identifier = this.unsavedAnchorId,
                localAnchor = this.localAnchor
            };
            this.anchorVisuals[visual.identifier] = visual;

            this.PlaceCube(this.localAnchor);

            this.UpdateMainStatusTitle("Create Cloud Anchor (once at 100%)");
        }

        public async Task<CloudSpatialAnchor> CreateCloudAnchorAsync(CloudSpatialAnchor newCloudAnchor)
        {
            if (newCloudAnchor == null)
            {
                throw new ArgumentNullException(nameof(newCloudAnchor));
            }

            if (newCloudAnchor.LocalAnchor == null || !string.IsNullOrEmpty(newCloudAnchor.Identifier))
            {
                throw new ArgumentException("The specified cloud anchor cannot be saved.", nameof(newCloudAnchor));
            }

            try
            {
                await this.cloudSession.CreateAnchorAsync(newCloudAnchor);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return newCloudAnchor;
        }

        public void CreateCloudAnchor()
        {
            this.currentlyPlacingAnchor = false;
            this.UpdateMainStatusTitle("Cloud Anchor being saved...");

            this.cloudAnchor = new CloudSpatialAnchor
            {
                LocalAnchor = this.localAnchor
            };

            // In this sample app we delete the cloud anchor explicitly, but you can also set it to expire automatically
            DateTime now = DateTime.Today;
            DateTimeOffset oneWeekFromNow = now.AddDays(7);
            this.cloudAnchor.Expiration = oneWeekFromNow;

            Task.Run(async () =>
            {
                try
                {
                    CloudSpatialAnchor result = await this.CreateCloudAnchorAsync(this.cloudAnchor);
                    this.AnchorSaveSuccess(result);
                }
                catch (CloudSpatialException ex)
                {
                    this.AnchorSaveFailed($"{ex.Message}, {ex.ErrorCode}");
                }
                catch (Exception ex)
                {
                    this.AnchorSaveFailed(ex.Message);
                }
            });
        }

        protected virtual void AnchorSaveSuccess(CloudSpatialAnchor result)
        {
            this.saveCount += 1;
            this.localAnchorCube.FirstMaterial.Diffuse.Contents = this.savedColor;
            this.targetId = result.Identifier;
            Console.WriteLine("ASADemo: created anchor: " + this.targetId);

            AnchorVisual visual = this.anchorVisuals[this.unsavedAnchorId];
            visual.cloudAnchor = this.cloudAnchor;
            visual.identifier = this.cloudAnchor.Identifier;
            this.anchorVisuals[visual.identifier] = visual;

            this.anchorVisuals.TryRemove(this.unsavedAnchorId, out _);
            this.localAnchor = null;
        }

        protected void AnchorSaveFailed(string message)
        {
            this.UpdateMainStatusTitle("Creation Failed");
            this.InvokeOnMainThread(() =>
            {
                this.errorLabelisHidden = false;
                this.errorLabelText = message;
                Console.WriteLine("Cloud save Failed : " + message);
            });
            this.localAnchorCube.FirstMaterial.Diffuse.Contents = this.failedColor;
        }

        public void LookForAnchor(string searchId)
        {
            if (string.IsNullOrWhiteSpace(searchId))
            {
                throw new ArgumentException("The value cannot be null, empty, or whitespace.", nameof(searchId));
            }

            AnchorLocateCriteria criteria = new AnchorLocateCriteria
            {
                Identifiers = new[] { searchId }
            };

            // Cannot run more than one watcher concurrently
            this.StopLocating();

            this.cloudSession.CreateWatcher(criteria);

            this.UpdateMainStatusTitle("Locating Anchor ....");
        }

        public void StopSession()
        {
            this.cloudSession.Stop();
            this.cloudAnchor = null;
            this.localAnchor = null;
            this.cloudSession = null;

            foreach (AnchorVisual visual in this.anchorVisuals.Values)
            {
                if (visual.node != null)
                {
                    visual.node.RemoveFromParentNode();
            }
            }

            this.anchorVisuals.Clear();
        }

        public void StopLocating()
        {
            CloudSpatialAnchorWatcher watcher = this.cloudSession.GetActiveWatchers().FirstOrDefault();

            // Only 1 active watcher at a time is permitted.
            watcher?.Stop();
        }

        #endregion Azure Spatial Anchors Helper Functions

        #region CloudSpatialAnchorSession Delegates

        private void SpatialCloudSession_LogDebug(object sender, OnLogDebugEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Message))
            {
                return;
            }

            Debug.WriteLine(e.Message);

            this.OnLogDebug?.Invoke(sender, e);
        }

        private void SpatialAnchorsSession_Error(object sender, SessionErrorEventArgs e)
        {
            string errorMessage = e.ErrorMessage;

            this.ShowLogMessage(errorMessage, SubView.ErrorView);
            Console.WriteLine("Error Code : " + e.ErrorCode + ", Message : " + e.ErrorMessage);

            this.OnSessionError?.Invoke(sender, e);
        }

        private void SpatialAnchorsSession_AnchorLocated(object sender, AnchorLocatedEventArgs e)
        {
            LocateAnchorStatus status = e.Status;
            switch (status)
            {
                case LocateAnchorStatus.AlreadyTracked:
                    break;

                case LocateAnchorStatus.Located:
                    {
                        CloudSpatialAnchor anchor = e.Anchor;
                        Debug.WriteLine("Cloud Anchor found! Identifier : " + anchor.Identifier);
                        AnchorVisual visual = new AnchorVisual
                        {
                            cloudAnchor = anchor,
                            identifier = anchor.Identifier,
                            localAnchor = anchor.LocalAnchor
                        };
                        this.anchorVisuals[visual.identifier] = visual;
                        this.scnView.Session.AddAnchor(anchor.LocalAnchor);
                        this.PlaceCube(visual.localAnchor);
                        break;
                    }

                case LocateAnchorStatus.NotLocated:
                case LocateAnchorStatus.NotLocatedAnchorDoesNotExist:
                    break;
            }

            this.OnAnchorLocated?.Invoke(sender, e);
        }

        private void SpatialAnchorsSession_LocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs e)
        {
            Console.WriteLine("Anchor locate operation completed, completed for watcher with identifier : " + e.Watcher.Identifier);
            this.ignoreMainButtonTaps = false;

            this.MoveToNextStepAfterAnchorLocated();

            this.OnLocateAnchorsCompleted?.Invoke(sender, e);
        }

        private void SpatialAnchorsSession_SessionUpdated(object sender, SessionUpdatedEventArgs e)
        {
            SessionStatus status = e.Status;
            string message = this.StatusToString(status);
            this.enoughDataForSaving = status.RecommendedForCreateProgress >= 1.0;
            if (this.step == DemoStep.DeleteLocatedANchors | this.step == DemoStep.LocateAnchor | this.step == DemoStep.LocateNearbyAnchors)
            {
                this.HideStatusLabel(true);
            }
            else
            {
                this.ShowLogMessage(message, SubView.StatusView);
            }

            Debug.WriteLine("SessionUpdated :: " + message);
            this.OnSessionUpdated?.Invoke(sender, e);
        }

        #endregion CloudSpatialAnchorSession Delegates

        #region String Helper Methods

        public void ShowLogMessage(string message, SubView sv)
        {
            switch (sv)
            {
                case SubView.ErrorView:
                    {
                        this.HideMainStatus();
                        this.InvokeOnMainThread(() =>
                        {
                            this.errorLabel.Text = message;
                            this.errorLabel.Hidden = false;
                        });
                        break;
                    }
                case SubView.StatusView:
                    {
                        this.InvokeOnMainThread(() =>
                        {
                            this.statusLabel.Text = message;
                            this.statusLabel.Hidden = false;
                        });
                        break;
                    }
            }
        }

        public string StatusToString(SessionStatus status)
        {
            string feedback = this.FeedbackToString(status.UserFeedback);

            if (this.ReportProgress)
            {
                float progress = status.RecommendedForCreateProgress;
                string str = string.Format("{0:p0} progress. ", progress);
                str += $"{feedback}";
                return str;
            }
            else
            {
                return feedback;
            }
        }

        public string FeedbackToString(SessionUserFeedback userFeedback)
        {
            if (userFeedback == SessionUserFeedback.NotEnoughMotion)
            {
                return "Not enough motion.";
            }
            else if (userFeedback == SessionUserFeedback.MotionTooQuick)
            {
                return "Motion is too quick.";
            }
            else if (userFeedback == SessionUserFeedback.NotEnoughFeatures)
            {
                return "Not enough features.";
            }
            else
            {
                return "Keep moving! 🤳";
        }
        }

        #endregion String Helper Methods
    }
}