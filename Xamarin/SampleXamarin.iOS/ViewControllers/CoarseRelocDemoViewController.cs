// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Foundation;
using Microsoft.Azure.SpatialAnchors;
using System;

namespace SampleXamarin.iOS
{
    [Register(nameof(CoarseRelocDemoViewController))]
    public class CoarseRelocDemoViewController : StepByStepDemoViewControllerBase
    {
        /// <summary>
        /// Whether the "Access WiFi Information" capability is enabled.
        /// If available, the MAC address of the connected Wi-Fi access point can be used
        /// to help find nearby anchors.
        /// </summary>
        /// <remarks>This entitlement requires a paid Apple Developer account.</remarks>
        private const bool HaveAccessWifiInformationEntitlement = false;

        [Outlet] private SensorStatusView sensorStatusView { get; set; }

        private PlatformLocationProvider locationProvider;
        private CloudSpatialAnchorWatcher nearDeviceWatcher;
        private int numAnchorsFound;

        protected CoarseRelocDemoViewController(IntPtr handle)
            : base(handle)
        {
            OnAnchorLocated += UpdateStatusTextOnNewAnchorsLocated;
        }

        public override void OnCloudAnchorCreated()
        {
            ignoreMainButtonTaps = false;
            step = DemoStep.LocateNearbyAnchors;

            BeginInvokeOnMainThread(() =>
            {
                HideStatusLabel(true);
                UpdateMainStatusTitle("Tap to start next Session & look for anchors near device.");
            });
        }

        public void UpdateStatusTextOnNewAnchorsLocated(object sender, AnchorLocatedEventArgs args)
        {
            if (args.Status == LocateAnchorStatus.Located)
            {
                ignoreMainButtonTaps = false;
                step = DemoStep.StopWatcher;

                BeginInvokeOnMainThread(() =>
                {
                    ++numAnchorsFound;
                    HideStatusLabel(true);
                    UpdateMainStatusTitle($"{numAnchorsFound} anchor(s) found! Tap to stop watcher.");
                });
            }
        }

        public override void OnUpdateScene(double timeInSeconds)
        {
            base.OnUpdateScene(timeInSeconds);

            BeginInvokeOnMainThread(() => sensorStatusView.Update());
        }

        public override void MainButtonTap()
        {
            if (ignoreMainButtonTaps)
            {
                return;
            }

            switch (step)
            {
                case DemoStep.Start:
                    {
                        UpdateMainStatusTitle("Tap to start Session");
                        step = DemoStep.CreateAnchor;
                        CreateLocationProvider();
                        break;
                    }
                case DemoStep.CreateAnchor:
                    {
                        ignoreMainButtonTaps = true;
                        currentlyPlacingAnchor = true;
                        saveCount = 0;

                        StartSession();
                        AttachLocationProviderToSession();

                        // When you tap on the screen, touchesBegan will call createLocalAnchor and create a local ARAnchor.
                        // We will then put that anchor in the anchorVisuals dictionary with a special key and call CreateCloudAnchor when there is enough data for saving.
                        // CreateCloudAnchor will call OnCloudAnchorCreated when its async method returns to move to the next step.
                        UpdateMainStatusTitle("Tap on the screen to create an Anchor ☝️");
                        break;
                    }
                case DemoStep.LocateNearbyAnchors:
                    {
                        ignoreMainButtonTaps = true;
                        StopSession();
                        StartSession();
                        AttachLocationProviderToSession();
                        LookForAnchorsNearDevice();
                        break;
                    }
                case DemoStep.StopWatcher:
                    {
                        step = DemoStep.StopSession;
                        nearDeviceWatcher?.Stop();
                        nearDeviceWatcher = null;
                        UpdateMainStatusTitle("Tap to stop Session and return to the main menu");
                        break;
                    }
                case DemoStep.StopSession:
                    {
                        StopSession();
                        locationProvider = null;
                        sensorStatusView.Model = null;
                        MoveToMainMenu();
                        break;
                    }

                default:
                    {
                        ShowLogMessage("Demo has somehow entered an invalid state", SubView.ErrorView);
                        break;
                    }
            }
        }

        private void CreateLocationProvider()
        {
            locationProvider = new PlatformLocationProvider();

            // Register known Bluetooth beacons
            locationProvider.Sensors.KnownBeaconProximityUuids =
                CoarseRelocSettings.KnownBluetoothProximityUuids;

            // Display the sensor status
            var sensorStatus = new LocationProviderSensorStatus(locationProvider);
            sensorStatusView.Model = sensorStatus;

            EnableAllowedSensors();
        }

        private void EnableAllowedSensors()
        {
            if (locationProvider != null)
            {
                SensorCapabilities sensors = locationProvider.Sensors;
                sensors.GeoLocationEnabled = true;
                sensors.WifiEnabled = HaveAccessWifiInformationEntitlement;
                sensors.BluetoothEnabled = true;
            }
        }

        private void AttachLocationProviderToSession()
        {
            cloudSession.LocationProvider = locationProvider;
        }

        private void LookForAnchorsNearDevice()
        {
            var locateCriteria = new AnchorLocateCriteria
            {
                NearDevice = new NearDeviceCriteria
                {
                    DistanceInMeters = 8.0f,
                    MaxResultCount = 25
                }
            };

            nearDeviceWatcher = cloudSession.CreateWatcher(locateCriteria);

            UpdateMainStatusTitle("Looking for anchors near device...");
        }
    }
}