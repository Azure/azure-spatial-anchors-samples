// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Android.OS;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Microsoft.Azure.SpatialAnchors;
using Xamarin.Essentials;
using static Android.Views.View;

namespace SampleXamarin
{
    internal class AnchorCreationFragment : Fragment, IOnClickListener
    {
        private bool isCreatingAnchor = false;

        private Button createAnchorButton;
        private ProgressBar requiredScanProgress;
        private ProgressBar recommendedScanProgress;

        public delegate void AnchorCreatedListener(AnchorVisual createdAnchor);
        public delegate void AnchorCreationFailedListener(AnchorVisual placedAnchor, string errorMessage);

        public AzureSpatialAnchorsManager CloudAnchorManager { private get; set; }
        public AnchorVisual PlacedVisual { private get; set; }
        public AnchorCreatedListener OnAnchorCreated { private get; set; }
        public AnchorCreationFailedListener OnAnchorCreationFailed { private get; set; }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.coarse_reloc_anchor_creation, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            createAnchorButton = view.FindViewById<Button>(Resource.Id.create_anchor);
            requiredScanProgress = view.FindViewById<ProgressBar>(Resource.Id.required_scan_progress);
            recommendedScanProgress = view.FindViewById<ProgressBar>(Resource.Id.recommended_scan_progress);
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (CloudAnchorManager == null)
            {
                FragmentHelper.BackToPreviousFragment(Activity);
            }
        }

        public override void OnStart()
        {
            base.OnStart();

            createAnchorButton.Enabled = false;
            createAnchorButton.SetOnClickListener(this);
            CloudAnchorManager.OnSessionUpdated += OnSessionUpdated;
        }

        public override void OnStop()
        {
            if (CloudAnchorManager != null)
            {
                CloudAnchorManager.OnSessionUpdated -= OnSessionUpdated;
            }

            if (PlacedVisual != null)
            {
                PlacedVisual.Destroy();
                PlacedVisual = null;
            }

            base.OnStop();
        }

        private bool CanCreateAnchor()
        {
            return !isCreatingAnchor && PlacedVisual != null;
        }

        private void OnSessionUpdated(object sender, SessionUpdatedEvent sessionUpdatedEvent)
        {
            float requiredForCreateProgress = sessionUpdatedEvent.Status.ReadyForCreateProgress;
            float recommendedForCreateProgress = sessionUpdatedEvent.Status.RecommendedForCreateProgress;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                requiredScanProgress.Progress = (int)(100 * requiredForCreateProgress);
                recommendedScanProgress.Progress = (int)(100 * recommendedForCreateProgress);
                bool allowCreation = CanCreateAnchor() && requiredForCreateProgress >= 1.0f;
                createAnchorButton.Enabled = allowCreation;
            });
        }

        async void IOnClickListener.OnClick(View view)
        {
            if (!CanCreateAnchor())
            {
                return;
            }

            createAnchorButton.Enabled = false;
            CloudSpatialAnchor cloudAnchor = new CloudSpatialAnchor();
            cloudAnchor.LocalAnchor = PlacedVisual.LocalAnchor;
            cloudAnchor.AppProperties.Add("Shape", PlacedVisual.Shape.ToString());

            isCreatingAnchor = true;

            await CloudAnchorManager.CreateAnchorAsync(cloudAnchor);
            if ((cloudAnchor.Identifier?.Length ?? 0) == 0)
            {
                OnAnchorCreationFailed?.Invoke(PlacedVisual, "Failed to create anchor");
                return;
            }

            AnchorVisual createdAnchor = PlacedVisual;
            PlacedVisual = null;
            OnAnchorCreated?.Invoke(createdAnchor);
        }
    }
}