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
    internal class WatcherFragment : Fragment, IOnClickListener
    {
        private CloudSpatialAnchorWatcher watcher;
        private Button stopWatcherButton;

        public delegate void AnchorDiscoveryListener(CloudSpatialAnchor cloudAnchor);

        public AzureSpatialAnchorsManager CloudAnchorManager { private get; set; }
        public AnchorDiscoveryListener OnAnchorDiscovered { private get; set; }

        public override View OnCreateView(
            LayoutInflater inflater,
            ViewGroup container,
            Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.coarse_reloc_watcher, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            stopWatcherButton = view.FindViewById<Button>(Resource.Id.stop_watcher);
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

            CloudAnchorManager.OnAnchorLocated += OnAnchorLocated;
            CloudAnchorManager.OnLocateAnchorsCompleted += OnLocateAnchorsCompleted;
            StartWatcher();
            stopWatcherButton.SetOnClickListener(this);
        }

        public override void OnStop()
        {
            stopWatcherButton.SetOnClickListener(null);
            StopWatcher();
            CloudAnchorManager.OnLocateAnchorsCompleted -= OnLocateAnchorsCompleted;
            CloudAnchorManager.OnAnchorLocated -= OnAnchorLocated;

            base.OnStop();
        }

        private void StartWatcher()
        {
            if (CloudAnchorManager.LocatingAnchors)
            {
                FragmentHelper.BackToPreviousFragment(Activity);
            }

            watcher = CloudAnchorManager.StartLocating(new AnchorLocateCriteria
            {
                NearDevice = new NearDeviceCriteria
                {
                    DistanceInMeters = 8.0f,
                    MaxResultCount = 25
                }
            });
        }

        private void StopWatcher()
        {
            if (watcher != null)
            {
                watcher.Stop();
                watcher = null;
            }
        }

        private void OnAnchorLocated(object sender, AnchorLocatedEvent anchorLocatedEvent)
        {
            if (OnAnchorDiscovered == null)
            {
                return;
            }

            if (anchorLocatedEvent.Status == LocateAnchorStatus.Located)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    OnAnchorDiscovered.Invoke(anchorLocatedEvent.Anchor));
            }
        }

        private void OnLocateAnchorsCompleted(
            object sender,
            LocateAnchorsCompletedEvent locateAnchorsCompletedEvent)
        {
            FragmentHelper.BackToPreviousFragment(Activity);
        }

        void IOnClickListener.OnClick(View view)
        {
            FragmentHelper.BackToPreviousFragment(Activity);
        }
    }

}
