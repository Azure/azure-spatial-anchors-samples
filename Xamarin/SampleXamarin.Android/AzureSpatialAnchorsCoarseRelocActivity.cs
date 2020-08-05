// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Google.AR.Sceneform;
using Google.AR.Sceneform.UX;
using Java.Interop;
using Microsoft.Azure.SpatialAnchors;
using System;
using System.Collections.Generic;

namespace SampleXamarin
{
    [Activity(Label = "AzureSpatialAnchorsCoarseRelocActivity")]
    internal class AzureSpatialAnchorsCoarseRelocActivity : AppCompatActivity
    {
        private AzureSpatialAnchorsManager cloudAnchorManager;
        private PlatformLocationProvider locationProvider;

        private ArFragment arFragment;
        private ArSceneView sceneView;
        private SensorStatusView sensorStatusView;

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            SensorPermissionsHelper.PermissionsResult outcome =
                    SensorPermissionsHelper.OnRequestPermissionsResult(this, requestCode);
            if (outcome == SensorPermissionsHelper.PermissionsResult.Allowed)
            {
                if (locationProvider != null)
                {
                    SensorPermissionsHelper.EnableAllowedSensors(this, locationProvider);
                }
            }
            else if (outcome == SensorPermissionsHelper.PermissionsResult.Denied)
            {
                Toast.MakeText(
                        this,
                        "Location permission is needed to run this demo",
                        ToastLength.Long)
                        .Show();
                Finish();
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_coarse_reloc);

            arFragment = (ArFragment)SupportFragmentManager.FindFragmentById(Resource.Id.ar_fragment);
            sceneView = arFragment.ArSceneView;

            sensorStatusView = FindViewById<SensorStatusView>(Resource.Id.sensor_status);

            Scene scene = sceneView.Scene;
            scene.Update += (_, args) =>
            {
                // Pass frames to Spatial Anchors for processing.
                cloudAnchorManager?.Update(sceneView.ArFrame);
                sensorStatusView.Update();
            };

            FragmentHelper.ReplaceFragment(this, new ActionSelectionFragment());
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

            if (sceneView?.Session is null && !SceneformHelper.TrySetupSessionForSceneView(this, sceneView))
            {
                // Exception will be logged and SceneForm will handle any ARCore specific issues.
                Finish();
                return;
            }

            if (string.IsNullOrWhiteSpace(AccountDetails.SpatialAnchorsAccountId) || AccountDetails.SpatialAnchorsAccountId == "Set me"
                || string.IsNullOrWhiteSpace(AccountDetails.SpatialAnchorsAccountKey) || AccountDetails.SpatialAnchorsAccountKey == "Set me"
                || string.IsNullOrWhiteSpace(AccountDetails.SpatialAnchorsAccountDomain) || AccountDetails.SpatialAnchorsAccountDomain == "Set me")
            {
                Toast.MakeText(this, $"\"Set {AccountDetails.SpatialAnchorsAccountId}, {AccountDetails.SpatialAnchorsAccountKey}, and {AccountDetails.SpatialAnchorsAccountDomain} in {nameof(AccountDetails)}.cs\"", ToastLength.Long)
                        .Show();

                Finish();
                return;
            }

            SensorPermissionsHelper.RequestMissingPermissions(this);

            cloudAnchorManager = new AzureSpatialAnchorsManager(sceneView.Session);
            cloudAnchorManager.StartSession();

            locationProvider = new PlatformLocationProvider();
            locationProvider.Sensors.SetKnownBeaconProximityUuids(CoarseRelocSettings.KnownBluetoothProximityUuids);
            SensorPermissionsHelper.EnableAllowedSensors(this, locationProvider);
            cloudAnchorManager.LocationProvider = locationProvider;

            sensorStatusView.Model = new LocationProviderSensorStatus(locationProvider);
        }

        protected override void OnPause()
        {
            sensorStatusView.Model = null;

            if (cloudAnchorManager != null)
            {
                cloudAnchorManager.StopSession();
                cloudAnchorManager = null;
            }
            locationProvider = null;

            base.OnPause();
        }

        [Export("OnAddAnchorClicked")]
        public void OnAddAnchorClicked(View view)
        {
            var placementFragment = new AnchorPlacementFragment();
            placementFragment.OnAnchorPlaced = OnAnchorPlaced;
            FragmentHelper.PushFragment(this, placementFragment);
        }

        public void OnAnchorPlaced(AnchorVisual placedAnchor)
        {
            var creationFragment = new AnchorCreationFragment();
            creationFragment.OnAnchorCreated = OnAnchorCreated;
            creationFragment.OnAnchorCreationFailed = OnAnchorCreationFailed;
            creationFragment.CloudAnchorManager = cloudAnchorManager;
            creationFragment.PlacedVisual = placedAnchor;
            FragmentHelper.BackToPreviousFragment(this);
            FragmentHelper.PushFragment(this, creationFragment);
        }

        public void OnAnchorCreated(AnchorVisual createdAnchor)
        {
            createdAnchor.SetColor(this, Android.Graphics.Color.Green);
            FragmentHelper.BackToPreviousFragment(this);
        }

        public void OnAnchorCreationFailed(AnchorVisual placedAnchor, string errorMessage)
        {
            placedAnchor.Destroy();
            FragmentHelper.BackToPreviousFragment(this);
            RunOnUiThread(() =>
            {
                string toastMessage = "Failed to save anchor: " + errorMessage;
                Toast.MakeText(this, toastMessage, ToastLength.Long).Show();
            });
        }

        [Export("OnStartWatcherClicked")]
        public void OnStartWatcherClicked(View view)
        {
            WatcherFragment watcherFragment = new WatcherFragment();
            watcherFragment.CloudAnchorManager = cloudAnchorManager;
            watcherFragment.OnAnchorDiscovered = OnAnchorDiscovered;
            FragmentHelper.PushFragment(this, watcherFragment);
        }

        public void OnAnchorDiscovered(CloudSpatialAnchor cloudAnchor)
        {
            AnchorVisual visual = new AnchorVisual(arFragment, cloudAnchor);
            visual.SetColor(this, Android.Graphics.Color.Green);
            IDictionary<string, string> properties = cloudAnchor.AppProperties;
            if (properties.ContainsKey("Shape"))
            {
                if (Enum.TryParse(properties["Shape"], out AnchorVisual.NamedShape savedShape))
                {
                    visual.Shape = savedShape;
                }
            }
            visual.AddToScene(arFragment);
        }

        [Export("OnBackClicked")]
        public void OnBackClicked(View view)
        {
            if (!FragmentHelper.BackToPreviousFragment(this))
            {
                Finish();
            }
        }
    }
}