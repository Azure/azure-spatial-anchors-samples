// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Google.AR.Core;
using Google.AR.Sceneform;
using Google.AR.Sceneform.UX;
using Java.Util;
using Microsoft.Azure.SpatialAnchors;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Color = Android.Graphics.Color;

namespace SampleXamarin
{
    [Activity(Label = "AzureSpatialAnchorsActivity")]
    public class AzureSpatialAnchorsActivity : AppCompatActivity
    {
        private const int numberOfNearbyAnchors = 3;

        private readonly ConcurrentDictionary<string, AnchorVisual> anchorVisuals = new ConcurrentDictionary<string, AnchorVisual>();

        private readonly object progressLock = new object();

        private Button actionButton;

        private string anchorID;

        private int anchorsToLocate = 0;

        private ArFragment arFragment;

        private Button backButton;

        private bool basicDemo = true;

        private AzureSpatialAnchorsManager cloudAnchorManager;

        private DemoStep currentDemoStep = DemoStep.Start;

        private bool enoughDataForSaving;

        private int saveCount = 0;

        private TextView scanProgressText;

        private ArSceneView sceneView;

        private TextView statusText;

        public void OnExitDemoClicked(object sender, EventArgs e)
        {
            this.DestroySession();
            this.Finish();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.SetContentView(Resource.Layout.activity_anchors);

            this.basicDemo = this.Intent.GetBooleanExtra("BasicDemo", true);

            this.arFragment = (ArFragment)this.SupportFragmentManager.FindFragmentById(Resource.Id.ar_fragment);
            this.arFragment.TapArPlane += (_, e) => this.OnTapArPlaneListener(e.HitResult, e.Plane, e.MotionEvent);

            this.sceneView = this.arFragment.ArSceneView;

            Scene scene = this.sceneView.Scene;
            scene.Update += (_, args) =>
            {
                // Pass frames to Spatial Anchors for processing.
                this.cloudAnchorManager?.Update(this.sceneView.ArFrame);
            };

            this.backButton = (Button)this.FindViewById(Resource.Id.backButton);
            this.backButton.Click += this.OnExitDemoClicked;
            this.statusText = (TextView)this.FindViewById(Resource.Id.statusText);
            this.scanProgressText = (TextView)this.FindViewById(Resource.Id.scanProgressText);
            this.actionButton = (Button)this.FindViewById(Resource.Id.actionButton);
            this.actionButton.Click += (_, args) => this.AdvanceDemo();
        }

        protected override void OnResume()
        {
            base.OnResume();

            // ArFragment of Sceneform automatically requests the camera permission before creating the AR session,
            // so we don't need to request the camera permission explicitly.
            // This will cause onResume to be called again after the user responds to the permission request.
            if (!SceneformHelper.HasCameraPermission(this))
            {
                return;
            }

            if (this.sceneView?.Session is null && !SceneformHelper.TrySetupSessionForSceneView(this, this.sceneView))
            {
                // Exception will be logged and SceneForm will handle any ARCore specific issues.
                this.Finish();
                return;
            }

            if (string.IsNullOrWhiteSpace(AccountDetails.SpatialAnchorsAccountId) || AccountDetails.SpatialAnchorsAccountId == "Set me"
                || string.IsNullOrWhiteSpace(AccountDetails.SpatialAnchorsAccountKey) || AccountDetails.SpatialAnchorsAccountKey == "Set me"
                || string.IsNullOrWhiteSpace(AccountDetails.SpatialAnchorsAccountDomain) || AccountDetails.SpatialAnchorsAccountDomain == "Set me")
            {
                Toast.MakeText(this, $"\"Set {AccountDetails.SpatialAnchorsAccountId}, {AccountDetails.SpatialAnchorsAccountKey}, and {AccountDetails.SpatialAnchorsAccountDomain} in {nameof(AccountDetails)}.cs\"", ToastLength.Long)
                        .Show();

                this.Finish();
                return;
            }

            if (this.currentDemoStep == DemoStep.Start)
            {
                this.StartDemo();
            }
        }

        private void AdvanceDemo()
        {
            switch (this.currentDemoStep)
            {
                case DemoStep.SaveAnchor:
                    if (!this.anchorVisuals.TryGetValue(string.Empty, out AnchorVisual visual))
                    {
                        throw new InvalidOperationException("Expected a visual with empty key to be available, but found none.");
                    }

                    if (visual == null)
                    {
                        return;
                    }

                    if (!this.enoughDataForSaving)
                    {
                        return;
                    }

                    // Hide the back button until we're done
                    this.RunOnUiThread(() => this.backButton.Visibility = ViewStates.Gone);

                    this.SetupLocalCloudAnchor(visual);

                    Task.Run(async () =>
                    {
                        try
                        {
                            CloudSpatialAnchor result = await this.cloudAnchorManager.CreateAnchorAsync(visual.CloudAnchor);
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

                    lock (this.progressLock)
                    {
                        this.RunOnUiThread(() =>
                        {
                            this.scanProgressText.Visibility = ViewStates.Gone;
                            this.scanProgressText.Text = string.Empty;
                            this.actionButton.Visibility = ViewStates.Invisible;
                            this.statusText.Text = "Saving cloud anchor...";
                        });
                        this.currentDemoStep = DemoStep.SavingAnchor;
                    }

                    break;

                case DemoStep.StopSession:
                    this.cloudAnchorManager.ResetSession(resumeIfRunning: false);
                    this.ClearVisuals();

                    this.RunOnUiThread(() =>
                    {
                        this.statusText.Text = string.Empty;
                        this.actionButton.Text = "Locate anchor";
                    });

                    this.currentDemoStep = DemoStep.LocateAnchor;

                    break;

                case DemoStep.LocateAnchor:
                    // We need to restart the session to find anchors we created.
                    this.StartNewSession();

                    AnchorLocateCriteria criteria = new AnchorLocateCriteria();
                    criteria.SetIdentifiers(new string[] { this.anchorID });

                    // Cannot run more than one watcher concurrently
                    this.StopWatcher();

                    this.anchorsToLocate = 1;
                    this.cloudAnchorManager.StartLocating(criteria);

                    this.RunOnUiThread(() =>
                    {
                        this.actionButton.Visibility = ViewStates.Invisible;
                        this.statusText.Text = "Look for anchor";
                    });

                    break;

                case DemoStep.LocateNearbyAnchors:
                    if (this.anchorVisuals.Count == 0 || !this.anchorVisuals.ContainsKey(this.anchorID))
                    {
                        this.RunOnUiThread(() => this.statusText.Text = "Cannot locate nearby. Previous anchor not yet located.");

                        break;
                    }

                    AnchorLocateCriteria nearbyLocateCriteria = new AnchorLocateCriteria();
                    NearAnchorCriteria nearAnchorCriteria = new NearAnchorCriteria
                    {
                        DistanceInMeters = 10,
                        SourceAnchor = this.anchorVisuals[this.anchorID].CloudAnchor
                    };
                    nearbyLocateCriteria.NearAnchor = nearAnchorCriteria;
                    // Cannot run more than one watcher concurrently
                    this.StopWatcher();
                    this.anchorsToLocate = this.saveCount;
                    this.cloudAnchorManager.StartLocating(nearbyLocateCriteria);
                    this.RunOnUiThread(() =>
                    {
                        this.actionButton.Visibility = ViewStates.Invisible;
                        this.statusText.Text = "Locating...";
                    });

                    break;

                case DemoStep.End:
                    foreach (AnchorVisual toDeleteVisual in this.anchorVisuals.Values)
                    {
                        this.cloudAnchorManager.DeleteAnchorAsync(toDeleteVisual.CloudAnchor);
                    }

                    this.DestroySession();

                    this.RunOnUiThread(() =>
                    {
                        this.actionButton.Text = "Restart";
                        this.statusText.Text = string.Empty;
                        this.backButton.Visibility = ViewStates.Visible;
                    });

                    this.currentDemoStep = DemoStep.Restart;

                    break;

                case DemoStep.Restart:
                    this.StartDemo();
                    break;
            }
        }

        private void AnchorSaveFailed(string message)
        {
            this.RunOnUiThread(() => this.statusText.Text = message);
            AnchorVisual visual = this.anchorVisuals[string.Empty];
            visual.SetColor(this, Color.Red);
        }

        private void AnchorSaveSuccess(CloudSpatialAnchor result)
        {
            this.saveCount++;

            this.anchorID = result.Identifier;
            Log.Debug("ASADemo:", "created anchor: " + this.anchorID);

            AnchorVisual visual = this.anchorVisuals[string.Empty];
            visual.SetColor(this, Color.Green);
            this.anchorVisuals[this.anchorID] = visual;
            this.anchorVisuals.TryRemove(string.Empty, out _);

            if (this.basicDemo || this.saveCount == numberOfNearbyAnchors)
            {
                this.RunOnUiThread(() =>
                {
                    this.statusText.Text = string.Empty;
                    this.actionButton.Visibility = ViewStates.Visible;
                });

                this.currentDemoStep = DemoStep.StopSession;
                this.AdvanceDemo();
            }
            else
            {
                // Need to create more anchors for nearby demo
                this.RunOnUiThread(() =>
                {
                    this.statusText.Text = "Tap a surface to create next anchor";
                    this.actionButton.Visibility = ViewStates.Invisible;
                });

                this.currentDemoStep = DemoStep.CreateAnchor;
            }
        }

        private void ClearVisuals()
        {
            foreach (AnchorVisual visual in this.anchorVisuals.Values)
            {
                visual.Destroy();
            }

            this.anchorVisuals.Clear();
        }

        private Anchor CreateAnchor(HitResult hitResult)
        {
            AnchorVisual visual = new AnchorVisual(arFragment, hitResult.CreateAnchor());
            visual.SetColor(this, Color.Yellow);
            visual.AddToScene(this.arFragment);
            this.anchorVisuals[string.Empty] = visual;

            this.RunOnUiThread(() =>
            {
                this.scanProgressText.Visibility = ViewStates.Visible;
                if (this.enoughDataForSaving)
                {
                    this.statusText.Text = "Ready to save";
                    this.actionButton.Text = "Save cloud anchor";
                    this.actionButton.Visibility = ViewStates.Visible;
                }
                else
                {
                    this.statusText.Text = "Move around the anchor";
                }
            });

            this.currentDemoStep = DemoStep.SaveAnchor;

            return visual.LocalAnchor;
        }

        private void DestroySession()
        {
            if (this.cloudAnchorManager != null)
            {
                this.cloudAnchorManager.StopSession();
                this.cloudAnchorManager = null;
            }

            this.ClearVisuals();
        }

        private void OnAnchorLocated(object sender, AnchorLocatedEvent eventArgs)
        {
            LocateAnchorStatus status = eventArgs.Status;

            if (status == LocateAnchorStatus.AlreadyTracked
                    || status == LocateAnchorStatus.Located)
            {
                this.anchorsToLocate--;
            }

            this.RunOnUiThread(() =>
            {
                if (status == LocateAnchorStatus.AlreadyTracked)
                {
                    // Nothing to do since we've already rendered any anchors we've located.
                }
                else if (status == LocateAnchorStatus.Located)
                {
                    this.RenderLocatedAnchor(eventArgs.Anchor);
                }
                else if (status == LocateAnchorStatus.NotLocatedAnchorDoesNotExist)
                {
                    this.statusText.Text = "Anchor does not exist";
                }
            });
        }

        private void OnLocateAnchorsCompleted(object sender, LocateAnchorsCompletedEvent _)
        {
            if (this.anchorsToLocate > 0)
            {
                // We didn't find all of the anchors.
                this.StopWatcher();
                this.RunOnUiThread(() =>
                {
                    this.statusText.Text = "Not all anchors were located. Check the logs for errors and\\or try again.";
                    this.actionButton.Visibility = ViewStates.Visible;
                    this.actionButton.Text = "Cleanup anchors";
                });
                this.currentDemoStep = DemoStep.End;

                return;
            }

            this.anchorsToLocate = 0;

            this.RunOnUiThread(() => this.statusText.Text = "Anchor located!");

            if (!this.basicDemo && this.currentDemoStep == DemoStep.LocateAnchor)
            {
                this.RunOnUiThread(() =>
                {
                    this.actionButton.Visibility = ViewStates.Visible;
                    this.actionButton.Text = "Look for anchors nearby";
                });
                this.currentDemoStep = DemoStep.LocateNearbyAnchors;
            }
            else
            {
                this.StopWatcher();
                this.RunOnUiThread(() =>
                {
                    this.actionButton.Visibility = ViewStates.Visible;
                    this.actionButton.Text = "Cleanup anchors";
                });
                this.currentDemoStep = DemoStep.End;
            }
        }

        private void OnSessionUpdated(object sender, SessionUpdatedEvent eventArgs)
        {
            float progress = eventArgs.Status.RecommendedForCreateProgress;
            this.enoughDataForSaving = progress >= 1.0;
            lock (this.progressLock)
            {
                if (this.currentDemoStep == DemoStep.SaveAnchor)
                {
                    this.RunOnUiThread(() =>
                    {
                        this.scanProgressText.Text = $"Scan progress is {Math.Min(1.0f, progress):0%}";
                    });

                    if (this.enoughDataForSaving && this.actionButton.Visibility != ViewStates.Visible)
                    {
                        // Enable the save button
                        this.RunOnUiThread(() =>
                        {
                            this.statusText.Text = "Ready to save";
                            this.actionButton.Text = "Save cloud anchor";
                            this.actionButton.Visibility = ViewStates.Visible;
                        });
                        this.currentDemoStep = DemoStep.SaveAnchor;
                    }
                }
            }
        }

        private void OnTapArPlaneListener(HitResult hitResult, Plane plane, MotionEvent motionEvent)
        {
            if (this.currentDemoStep == DemoStep.CreateAnchor)
            {
                this.CreateAnchor(hitResult);
            }
        }

        private void RenderLocatedAnchor(CloudSpatialAnchor anchor)
        {
            AnchorVisual foundVisual = new AnchorVisual(arFragment, anchor.LocalAnchor)
            {
                CloudAnchor = anchor
            };
            foundVisual.AnchorNode.SetParent(this.arFragment.ArSceneView.Scene);
            string cloudAnchorIdentifier = foundVisual.CloudAnchor.Identifier;
            foundVisual.SetColor(this, Color.Red);
            foundVisual.AddToScene(this.arFragment);
            this.anchorVisuals[cloudAnchorIdentifier] = foundVisual;
        }

        private void SetupLocalCloudAnchor(AnchorVisual visual)
        {
            CloudSpatialAnchor cloudAnchor = new CloudSpatialAnchor
            {
                LocalAnchor = visual.LocalAnchor
            };
            visual.CloudAnchor = cloudAnchor;

            // In this sample app we delete the cloud anchor explicitly, but you can also set it to expire automatically
            Date now = new Date();
            Calendar cal = Calendar.Instance;
            cal.Time = now;
            cal.Add(CalendarField.Date, 7);
            Date oneWeekFromNow = cal.Time;
            cloudAnchor.Expiration = oneWeekFromNow;
        }

        private void StartDemo()
        {
            this.saveCount = 0;
            this.StartNewSession();
            this.RunOnUiThread(() =>
            {
                this.scanProgressText.Visibility = ViewStates.Gone;
                this.statusText.Text = "Tap a surface to create an anchor";
                this.actionButton.Visibility = ViewStates.Invisible;
            });
            this.currentDemoStep = DemoStep.CreateAnchor;
        }

        private void StartNewSession()
        {
            this.DestroySession();

            this.cloudAnchorManager = new AzureSpatialAnchorsManager(this.sceneView.Session);
            this.cloudAnchorManager.OnAnchorLocated += this.OnAnchorLocated;
            this.cloudAnchorManager.OnLocateAnchorsCompleted += this.OnLocateAnchorsCompleted;
            this.cloudAnchorManager.OnSessionUpdated += this.OnSessionUpdated;
            this.cloudAnchorManager.StartSession();
        }

        private void StopWatcher()
        {
            if (this.cloudAnchorManager != null)
            {
                this.cloudAnchorManager.StopLocating();
            }
            this.anchorsToLocate = 0;
        }
    }
}