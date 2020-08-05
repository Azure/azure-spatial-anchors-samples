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
using Microsoft.Azure.SpatialAnchors;
using SampleXamarin.AnchorSharing;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Color = Android.Graphics.Color;

namespace SampleXamarin
{
    [Activity(Label = "AzureSpatialAnchorsSharedActivity")]
    public class AzureSpatialAnchorsSharedActivity : AppCompatActivity
    {
        private readonly ConcurrentDictionary<string, AnchorVisual> anchorVisuals = new ConcurrentDictionary<string, AnchorVisual>();

        private readonly object renderLock = new object();

        private AnchorSharingServiceClient anchorSharingServiceClient;

        private string anchorId = string.Empty;

        private EditText anchorNumInput;

        private ArFragment arFragment;

        private AzureSpatialAnchorsManager cloudAnchorManager;

        private Button createButton;

        private DemoStep currentStep = DemoStep.Start;

        private TextView editTextInfo;

        private Button exitButton;

        private string feedbackText;

        private Button locateButton;

        private ArSceneView sceneView;

        private TextView textView;

        public void OnCreateButtonClicked(object sender, EventArgs args)
        {
            this.textView.Text = "Scan your environment and place an anchor";
            this.DestroySession();

            this.cloudAnchorManager = new AzureSpatialAnchorsManager(this.sceneView.Session);

            this.cloudAnchorManager.OnSessionUpdated += (_, sessionUpdateArgs) =>
            {
                SessionStatus status = sessionUpdateArgs.Status;

                if (this.currentStep == DemoStep.CreateAnchor)
                {
                    float progress = status.RecommendedForCreateProgress;
                    if (progress >= 1.0)
                    {
                        if (this.anchorVisuals.TryGetValue(string.Empty, out AnchorVisual visual))
                        {
                            //Transition to saving...
                            this.TransitionToSaving(visual);
                        }
                        else
                        {
                            this.feedbackText = "Tap somewhere to place an anchor.";
                        }
                    }
                    else
                    {
                        this.feedbackText = $"Progress is {progress:0%}";
                    }
                }
            };

            this.cloudAnchorManager.StartSession();
            this.currentStep = DemoStep.CreateAnchor;
            this.EnableCorrectUIControls();
        }

        public void OnExitDemoClicked(object sender, EventArgs args)
        {
            lock (this.renderLock)
            {
                this.DestroySession();

                this.Finish();
            }
        }

        public void OnLocateButtonClicked(object sender, EventArgs args)
        {
            if (this.currentStep == DemoStep.Start)
            {
                this.currentStep = DemoStep.EnterAnchorNumber;
                this.textView.Text = "Enter an anchor number and press locate";
                this.EnableCorrectUIControls();
            }
            else
            {
                string inputVal = this.anchorNumInput.Text;
                if (!string.IsNullOrEmpty(inputVal))
                {
                    Task.Run(async () =>
                    {
                        RetrieveAnchorResponse response = await this.anchorSharingServiceClient.RetrieveAnchorIdAsync(inputVal);

                        if (response.AnchorFound)
                        {
                            this.AnchorLookedUp(response.AnchorId);
                        }
                        else
                        {
                            this.RunOnUiThread(() => {
                                this.currentStep = DemoStep.Start;
                                this.EnableCorrectUIControls();
                                this.textView.Text = "Anchor number not found or has expired.";
                            });
                        }
                    });

                    this.currentStep = DemoStep.LocateAnchor;
                    this.EnableCorrectUIControls();
                }
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            this.SetContentView(Resource.Layout.activity_shared);

            this.arFragment = (ArFragment)this.SupportFragmentManager.FindFragmentById(Resource.Id.ar_fragment);
            this.arFragment.TapArPlane += (sender, args) => this.OnTapArPlaneListener(args.HitResult, args.Plane, args.MotionEvent);

            this.sceneView = this.arFragment.ArSceneView;

            this.exitButton = (Button)this.FindViewById(Resource.Id.mainMenu);
            this.exitButton.Click += this.OnExitDemoClicked;
            this.textView = (TextView)this.FindViewById(Resource.Id.textView);
            this.textView.Visibility = ViewStates.Visible;
            this.locateButton = (Button)this.FindViewById(Resource.Id.locateButton);
            this.locateButton.Click += this.OnLocateButtonClicked;
            this.createButton = (Button)this.FindViewById(Resource.Id.createButton);
            this.createButton.Click += this.OnCreateButtonClicked;
            this.anchorNumInput = (EditText)this.FindViewById(Resource.Id.anchorNumText);
            this.editTextInfo = (TextView)this.FindViewById(Resource.Id.editTextInfo);
            this.EnableCorrectUIControls();

            Scene scene = this.sceneView.Scene;
            scene.Update += (_, args) =>
            {
                // Pass frames to Spatial Anchors for processing.
                this.cloudAnchorManager?.Update(this.sceneView.ArFrame);
            };
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            this.DestroySession();
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

            if (string.IsNullOrEmpty(AccountDetails.AnchorSharingServiceUrl) || AccountDetails.AnchorSharingServiceUrl == "Set me")
            {
                Toast.MakeText(this, $"Set the {AccountDetails.AnchorSharingServiceUrl} in {nameof(AccountDetails)}.cs", ToastLength.Long)
                        .Show();

                this.Finish();
                return;
            }

            this.anchorSharingServiceClient = new AnchorSharingServiceClient(AccountDetails.AnchorSharingServiceUrl);

            this.UpdateStatic();
        }

        private void AnchorLookedUp(string anchorId)
        {
            Log.Debug("ASADemo", "anchor " + anchorId);
            this.anchorId = anchorId;
            this.DestroySession();

            bool anchorLocated = false;

            this.cloudAnchorManager = new AzureSpatialAnchorsManager(this.sceneView.Session);
            this.cloudAnchorManager.OnAnchorLocated += (sender, args) =>
                this.RunOnUiThread(() =>
                {
                    CloudSpatialAnchor anchor = args.Anchor;
                    LocateAnchorStatus status = args.Status;

                    if (status == LocateAnchorStatus.AlreadyTracked || status == LocateAnchorStatus.Located)
                    {
                        anchorLocated = true;

                        AnchorVisual foundVisual = new AnchorVisual(arFragment, anchor.LocalAnchor)
                        {
                            CloudAnchor = anchor
                        };
                        foundVisual.AnchorNode.SetParent(this.arFragment.ArSceneView.Scene);
                        string cloudAnchorIdentifier = foundVisual.CloudAnchor.Identifier;
                        foundVisual.SetColor(this, Color.Yellow);
                        foundVisual.AddToScene(this.arFragment);
                        this.anchorVisuals[cloudAnchorIdentifier] = foundVisual;
                    }
                });

            this.cloudAnchorManager.OnLocateAnchorsCompleted += (sender, args) =>
            {
                this.currentStep = DemoStep.Start;

                this.RunOnUiThread(() =>
                {
                    if (anchorLocated)
                    {
                        this.textView.Text = "Anchor located!";
                    }
                    else
                    {
                        this.textView.Text = "Anchor was not located. Check the logs for errors and\\or create a new anchor and try again.";
                    }

                    this.EnableCorrectUIControls();
                });
            };
            this.cloudAnchorManager.StartSession();
            AnchorLocateCriteria criteria = new AnchorLocateCriteria();
            criteria.SetIdentifiers(new string[] { anchorId });
            this.cloudAnchorManager.StartLocating(criteria);
        }

        private void AnchorPosted(string anchorNumber)
        {
            this.RunOnUiThread(() =>
            {
                this.textView.Text = "Anchor Number: " + anchorNumber;
                this.currentStep = DemoStep.Start;
                this.cloudAnchorManager.StopSession();
                this.cloudAnchorManager = null;
                this.ClearVisuals();
                this.EnableCorrectUIControls();
            });
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

            return visual.LocalAnchor;
        }

        private void CreateAnchorExceptionCompletion(string message)
        {
            this.textView.Text = message;
            this.currentStep = DemoStep.Start;
            this.cloudAnchorManager.StopSession();
            this.cloudAnchorManager = null;
            this.EnableCorrectUIControls();
        }

        private void DestroySession()
        {
            if (this.cloudAnchorManager != null)
            {
                this.cloudAnchorManager.StopSession();
                this.cloudAnchorManager = null;
            }

            this.StopWatcher();

            this.ClearVisuals();
        }

        private void EnableCorrectUIControls()
        {
            switch (this.currentStep)
            {
                case DemoStep.Start:
                    this.textView.Visibility = ViewStates.Visible;
                    this.locateButton.Visibility = ViewStates.Visible;
                    this.createButton.Visibility = ViewStates.Visible;
                    this.anchorNumInput.Visibility = ViewStates.Gone;
                    this.editTextInfo.Visibility = ViewStates.Gone;
                    this.SupportActionBar.Hide();
                    break;

                case DemoStep.CreateAnchor:
                    this.textView.Visibility = ViewStates.Visible;
                    this.locateButton.Visibility = ViewStates.Gone;
                    this.createButton.Visibility = ViewStates.Gone;
                    this.anchorNumInput.Visibility = ViewStates.Gone;
                    this.editTextInfo.Visibility = ViewStates.Gone;
                    break;

                case DemoStep.LocateAnchor:
                    this.textView.Visibility = ViewStates.Visible;
                    this.locateButton.Visibility = ViewStates.Gone;
                    this.createButton.Visibility = ViewStates.Gone;
                    this.anchorNumInput.Visibility = ViewStates.Gone;
                    this.editTextInfo.Visibility = ViewStates.Gone;
                    break;

                case DemoStep.SavingAnchor:
                    this.textView.Visibility = ViewStates.Visible;
                    this.locateButton.Visibility = ViewStates.Gone;
                    this.createButton.Visibility = ViewStates.Gone;
                    this.anchorNumInput.Visibility = ViewStates.Gone;
                    this.editTextInfo.Visibility = ViewStates.Gone;
                    break;

                case DemoStep.EnterAnchorNumber:
                    this.textView.Visibility = ViewStates.Visible;
                    this.locateButton.Visibility = ViewStates.Visible;
                    this.createButton.Visibility = ViewStates.Gone;
                    this.anchorNumInput.Visibility = ViewStates.Visible;
                    this.editTextInfo.Visibility = ViewStates.Visible;
                    break;
            }
        }

        private void OnTapArPlaneListener(HitResult hitResult, Plane plane, MotionEvent motionEvent)
        {
            if (this.currentStep == DemoStep.CreateAnchor)
            {
                if (!this.anchorVisuals.ContainsKey(string.Empty))
                {
                    this.CreateAnchor(hitResult);
                }
            }
        }

        private void StopWatcher()
        {
            if (this.cloudAnchorManager != null)
            {
                this.cloudAnchorManager.StopLocating();
            }
        }

        private void TransitionToSaving(AnchorVisual visual)
        {
            Log.Debug("ASADemo:", "transition to saving");
            this.currentStep = DemoStep.SavingAnchor;
            this.EnableCorrectUIControls();
            Log.Debug("ASADemo", "creating anchor");
            CloudSpatialAnchor cloudAnchor = new CloudSpatialAnchor();
            visual.CloudAnchor = cloudAnchor;
            cloudAnchor.LocalAnchor = visual.LocalAnchor;

            this.cloudAnchorManager.CreateAnchorAsync(cloudAnchor)
                .ContinueWith(async cloudAnchorTask =>
                {
                    try
                    {
                        CloudSpatialAnchor anchor = await cloudAnchorTask;

                        string anchorId = anchor.Identifier;
                        Log.Debug("ASADemo:", "created anchor: " + anchorId);
                        visual.SetColor(this, Color.Green);
                        this.anchorVisuals[anchorId] = visual;
                        this.anchorVisuals.TryRemove(string.Empty, out _);

                        Log.Debug("ASADemo", "recording anchor with web service");
                        Log.Debug("ASADemo", "anchorId: " + anchorId);
                        SendAnchorResponse response = await this.anchorSharingServiceClient.SendAnchorIdAsync(anchorId);
                        this.AnchorPosted(response.AnchorNumber);
                    }
                    catch (CloudSpatialException ex)
                    {
                        this.CreateAnchorExceptionCompletion($"{ex.Message}, {ex.ErrorCode}");
                    }
                    catch (Exception ex)
                    {
                        this.CreateAnchorExceptionCompletion(ex.Message);
                        visual.SetColor(this, Color.Red);
                    }
                });
        }

        private void UpdateStatic()
        {
            new Handler().PostDelayed(() =>
            {
                switch (this.currentStep)
                {
                    case DemoStep.Start:
                        break;

                    case DemoStep.CreateAnchor:
                        this.textView.Text = this.feedbackText;
                        break;

                    case DemoStep.LocateAnchor:
                        if (!string.IsNullOrEmpty(this.anchorId))
                        {
                            this.textView.Text = "searching for\n" + this.anchorId;
                        }
                        break;

                    case DemoStep.SavingAnchor:
                        this.textView.Text = "saving...";
                        break;

                    case DemoStep.EnterAnchorNumber:
                        break;
                }

                this.UpdateStatic();
            }, 500);
        }
    }
}