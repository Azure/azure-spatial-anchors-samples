// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    public class AzureSpatialAnchorsCoarseRelocDemoScript : DemoScriptBase
    {
        internal enum AppState
        {
            DemoStepCreateSession = 0,
            DemoStepConfigSession,
            DemoStepStartSession,
            DemoStepCreateLocationProvider,
            DemoStepConfigureSensors,
            DemoStepCreateLocalAnchor,
            DemoStepSaveCloudAnchor,
            DemoStepSavingCloudAnchor,
            DemoStepStopSession,
            DemoStepDestroySession,
            DemoStepCreateSessionForQuery,
            DemoStepStartSessionForQuery,
            DemoStepLookForAnchorsNearDevice,
            DemoStepLookingForAnchorsNearDevice,
            DemoStepStopWatcher,
            DemoStepDeleteAnchors,
            DemoStepEnumerateAnchors,
            DemoStepStopSessionForQuery,
            DemoStepComplete
        }

        private readonly Dictionary<AppState, DemoStepParams> stateParams = new Dictionary<AppState, DemoStepParams>
        {
            { AppState.DemoStepCreateSession,new DemoStepParams() { StepMessage = "Next: Create Azure Spatial Anchors Session", StepColor = Color.clear }},
            { AppState.DemoStepConfigSession,new DemoStepParams() { StepMessage = "Next: Configure Azure Spatial Anchors Session", StepColor = Color.clear }},
            { AppState.DemoStepStartSession,new DemoStepParams() { StepMessage = "Next: Start Azure Spatial Anchors Session", StepColor = Color.clear }},
            { AppState.DemoStepCreateLocationProvider,new DemoStepParams() { StepMessage = "Next: Create Location Provider", StepColor = Color.clear }},
            { AppState.DemoStepConfigureSensors,new DemoStepParams() { StepMessage = "Next: Configure Sensors", StepColor = Color.clear }},
            { AppState.DemoStepCreateLocalAnchor,new DemoStepParams() { StepMessage = "Tap a surface to add the Local Anchor.", StepColor = Color.blue }},
            { AppState.DemoStepSaveCloudAnchor,new DemoStepParams() { StepMessage = "Next: Save Local Anchor to cloud", StepColor = Color.yellow }},
            { AppState.DemoStepSavingCloudAnchor,new DemoStepParams() { StepMessage = "Saving local Anchor to cloud...", StepColor = Color.yellow }},
            { AppState.DemoStepStopSession,new DemoStepParams() { StepMessage = "Next: Stop Azure Spatial Anchors Session", StepColor = Color.green }},
            { AppState.DemoStepCreateSessionForQuery,new DemoStepParams() { StepMessage = "Next: Create Azure Spatial Anchors Session for query", StepColor = Color.clear }},
            { AppState.DemoStepStartSessionForQuery,new DemoStepParams() { StepMessage = "Next: Start Azure Spatial Anchors Session for query", StepColor = Color.clear }},
            { AppState.DemoStepLookForAnchorsNearDevice,new DemoStepParams() { StepMessage = "Next: Look for Anchors near device", StepColor = Color.clear }},
            { AppState.DemoStepLookingForAnchorsNearDevice,new DemoStepParams() { StepMessage = "Looking for Anchors near device...", StepColor = Color.clear }},
            { AppState.DemoStepStopWatcher,new DemoStepParams() { StepMessage = "Next: Stop Watcher", StepColor = Color.green }},
            { AppState.DemoStepDeleteAnchors,new DemoStepParams() { StepMessage = "Next: Delete Anchors", StepColor = Color.green }},
            { AppState.DemoStepEnumerateAnchors,new DemoStepParams() { StepMessage = "Next: Enumerate Anchors", StepColor = Color.green }},
            { AppState.DemoStepStopSessionForQuery,new DemoStepParams() { StepMessage = "Next: Stop Azure Spatial Anchors Session for query", StepColor = Color.grey }},
            { AppState.DemoStepComplete,new DemoStepParams() { StepMessage = "Next: Restart demo", StepColor = Color.clear }}
        };

        private AppState _currentAppState = AppState.DemoStepCreateSession;

        AppState currentAppState
        {
            get
            {
                return _currentAppState;
            }
            set
            {
                if (_currentAppState != value)
                {
                    Debug.LogFormat("State from {0} to {1}", _currentAppState, value);
                    _currentAppState = value;
                    if (spawnedObjectMat != null)
                    {
                        spawnedObjectMat.color = stateParams[_currentAppState].StepColor;
                    }

                    if (!isErrorActive)
                    {
                        feedbackBox.text = stateParams[_currentAppState].StepMessage;
                    }
                    EnableCorrectUIControls();
                }
            }
        }

        private PlatformLocationProvider locationProvider;
        private List<GameObject> allDiscoveredAnchors = new List<GameObject>();

        private int nextButtonIndex = 0;
        private int enumerateButtonIndex = 2;
        private int deleteNewAnchorButtonIndex = 3;
        private int deleteAllAnchorsButtonIndex = 4;

        private void EnableCorrectUIControls()
        {

            switch (currentAppState)
            {
                case AppState.DemoStepCreateLocalAnchor:
                case AppState.DemoStepSavingCloudAnchor:
                case AppState.DemoStepLookingForAnchorsNearDevice:
                #if WINDOWS_UWP || UNITY_WSA
                    // Sample disables "Next step" button on Hololens, so it doesn't overlay with placing the anchor and async operations, 
                    // which are not affected by user input.
                    // This is also part of a workaround for placing anchor interaction, which doesn't receive callback when air tapping for placement
                    // This is not applicable to Android/iOS versions.
                    XRUXPicker.Instance.GetDemoButtons()[nextButtonIndex].gameObject.SetActive(false);
                #endif
                    break;
                case AppState.DemoStepEnumerateAnchors:
                    XRUXPicker.Instance.GetDemoButtons()[enumerateButtonIndex].interactable = true;
                    XRUXPicker.Instance.GetDemoButtons()[enumerateButtonIndex].gameObject.SetActive(true);
                    XRUXPicker.Instance.GetDemoButtons()[deleteNewAnchorButtonIndex].gameObject.SetActive(false);
                    XRUXPicker.Instance.GetDemoButtons()[deleteAllAnchorsButtonIndex].gameObject.SetActive(false);
                    break;
                case AppState.DemoStepDeleteAnchors:
                    XRUXPicker.Instance.GetDemoButtons()[deleteNewAnchorButtonIndex].interactable = true;
                    XRUXPicker.Instance.GetDemoButtons()[deleteAllAnchorsButtonIndex].interactable = true;
                    XRUXPicker.Instance.GetDemoButtons()[deleteAllAnchorsButtonIndex].gameObject.SetActive(true);
                    XRUXPicker.Instance.GetDemoButtons()[deleteNewAnchorButtonIndex].gameObject.SetActive(true);
                    XRUXPicker.Instance.GetDemoButtons()[deleteAllAnchorsButtonIndex].gameObject.SetActive(true);
                    break;
                default:
                    XRUXPicker.Instance.GetDemoButtons()[nextButtonIndex].gameObject.SetActive(true);
                    XRUXPicker.Instance.GetDemoButtons()[enumerateButtonIndex].gameObject.SetActive(false);
                    XRUXPicker.Instance.GetDemoButtons()[deleteNewAnchorButtonIndex].gameObject.SetActive(false);
                    XRUXPicker.Instance.GetDemoButtons()[deleteAllAnchorsButtonIndex].gameObject.SetActive(false);
                    break;
            }
        }

        public SensorStatus GeoLocationStatus
        {
            get
            {
                if (locationProvider == null)
                    return SensorStatus.MissingSensorFingerprintProvider;
                if (!locationProvider.Sensors.GeoLocationEnabled)
                    return SensorStatus.DisabledCapability;
                switch (locationProvider.GeoLocationStatus)
                {
                    case GeoLocationStatusResult.Available:
                        return SensorStatus.Available;
                    case GeoLocationStatusResult.DisabledCapability:
                        return SensorStatus.DisabledCapability;
                    case GeoLocationStatusResult.MissingSensorFingerprintProvider:
                        return SensorStatus.MissingSensorFingerprintProvider;
                    case GeoLocationStatusResult.NoGPSData:
                        return SensorStatus.NoData;
                    default:
                        return SensorStatus.MissingSensorFingerprintProvider;
                }
            }
        }

        public SensorStatus WifiStatus
        {
            get
            {
                if (locationProvider == null)
                    return SensorStatus.MissingSensorFingerprintProvider;
                if (!locationProvider.Sensors.WifiEnabled)
                    return SensorStatus.DisabledCapability;
                switch (locationProvider.WifiStatus)
                {
                    case WifiStatusResult.Available:
                        return SensorStatus.Available;
                    case WifiStatusResult.DisabledCapability:
                        return SensorStatus.DisabledCapability;
                    case WifiStatusResult.MissingSensorFingerprintProvider:
                        return SensorStatus.MissingSensorFingerprintProvider;
                    case WifiStatusResult.NoAccessPointsFound:
                        return SensorStatus.NoData;
                    default:
                        return SensorStatus.MissingSensorFingerprintProvider;
                }
            }
        }

        public SensorStatus BluetoothStatus
        {
            get
            {
                if (locationProvider == null)
                    return SensorStatus.MissingSensorFingerprintProvider;
                if (!locationProvider.Sensors.BluetoothEnabled)
                    return SensorStatus.DisabledCapability;
                switch (locationProvider.BluetoothStatus)
                {
                    case BluetoothStatusResult.Available:
                        return SensorStatus.Available;
                    case BluetoothStatusResult.DisabledCapability:
                        return SensorStatus.DisabledCapability;
                    case BluetoothStatusResult.MissingSensorFingerprintProvider:
                        return SensorStatus.MissingSensorFingerprintProvider;
                    case BluetoothStatusResult.NoBeaconsFound:
                        return SensorStatus.NoData;
                    default:
                        return SensorStatus.MissingSensorFingerprintProvider;
                }
            }
        }

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any
        /// of the Update methods are called the first time.
        /// </summary>
        public override void Start()
        {
            Debug.Log(">>Azure Spatial Anchors Demo Script Start");

            base.Start();

            if (!SanityCheckAccessConfiguration())
            {
                return;
            }
            feedbackBox.text = stateParams[currentAppState].StepMessage;

            Debug.Log("Azure Spatial Anchors Demo script started");

            enableAdvancingOnSelect = false;

            EnableCorrectUIControls();
        }

        protected override void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
        {
            base.OnCloudAnchorLocated(args);

            if (args.Status == LocateAnchorStatus.Located)
            {
                CloudSpatialAnchor cloudAnchor = args.Anchor;

                UnityDispatcher.InvokeOnAppThread(() =>
                {
                    currentAppState = AppState.DemoStepStopWatcher;
                    Pose anchorPose = cloudAnchor.GetPose();
                    GameObject spawnedObject = SpawnNewAnchoredObject(anchorPose.position, anchorPose.rotation, cloudAnchor);
                    allDiscoveredAnchors.Add(spawnedObject);
                });
            }
        }

        public void OnApplicationFocus(bool focusStatus)
        {
#if UNITY_ANDROID
            // We may get additional permissions at runtime. Enable the sensors once app is resumed
            if (focusStatus && locationProvider != null)
            {
                ConfigureSensors();
            }
#endif
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        public override void Update()
        {
            base.Update();

            if (spawnedObjectMat != null)
            {
                float rat = 0.1f;
                float createProgress = 0f;
                if (CloudManager.SessionStatus != null)
                {
                    createProgress = CloudManager.SessionStatus.RecommendedForCreateProgress;
                }
                rat += (Mathf.Min(createProgress, 1) * 0.9f);
                spawnedObjectMat.color = GetStepColor() * rat;
            }
        }

        protected override bool IsPlacingObject()
        {
            return currentAppState == AppState.DemoStepCreateLocalAnchor;
        }

        protected override Color GetStepColor()
        {
            return stateParams[currentAppState].StepColor;
        }

        protected override async Task OnSaveCloudAnchorSuccessfulAsync()
        {
            await base.OnSaveCloudAnchorSuccessfulAsync();

            Debug.Log("Anchor created, yay!");

            currentAppState = AppState.DemoStepStopSession;
        }

        protected override void OnSaveCloudAnchorFailed(Exception exception)
        {
            base.OnSaveCloudAnchorFailed(exception);
        }

        public async override Task AdvanceDemoAsync()
        {
            switch (currentAppState)
            {
                case AppState.DemoStepCreateSession:
                    if (CloudManager.Session == null)
                    {
                        await CloudManager.CreateSessionAsync();
                    }
                    currentCloudAnchor = null;
                    currentAppState = AppState.DemoStepConfigSession;
                    break;
                case AppState.DemoStepConfigSession:
                    ConfigureSession();
                    currentAppState = AppState.DemoStepStartSession;
                    break;
                case AppState.DemoStepStartSession:
                    await CloudManager.StartSessionAsync();
                    currentAppState = AppState.DemoStepCreateLocationProvider;
                    break;
                case AppState.DemoStepCreateLocationProvider:
                    locationProvider = new PlatformLocationProvider();
                    CloudManager.Session.LocationProvider = locationProvider;
                    currentAppState = AppState.DemoStepConfigureSensors;
                    break;
                case AppState.DemoStepConfigureSensors:
                    SensorPermissionHelper.RequestSensorPermissions();
                    ConfigureSensors();
                    currentAppState = AppState.DemoStepCreateLocalAnchor;
                    // Enable advancing to next step on Air Tap, which is an easier interaction for placing the anchor.
                    // (placing the anchor with Air tap automatically advances the demo).
                    enableAdvancingOnSelect = true;
                    break;
                case AppState.DemoStepCreateLocalAnchor:
                    if (spawnedObject != null)
                    {
                        currentAppState = AppState.DemoStepSaveCloudAnchor;
                    }
                    enableAdvancingOnSelect = false;
                    break;
                case AppState.DemoStepSaveCloudAnchor:
                    currentAppState = AppState.DemoStepSavingCloudAnchor;
                    await SaveCurrentObjectAnchorToCloudAsync();
                    break;
                case AppState.DemoStepStopSession:
                    CloudManager.StopSession();
                    CleanupSpawnedObjects();
                    await CloudManager.ResetSessionAsync();
                    locationProvider = null;
                    currentAppState = AppState.DemoStepCreateSessionForQuery;
                    break;
                case AppState.DemoStepCreateSessionForQuery:
                    ConfigureSession();
                    locationProvider = new PlatformLocationProvider();
                    CloudManager.Session.LocationProvider = locationProvider;
                    ConfigureSensors();
                    currentAppState = AppState.DemoStepStartSessionForQuery;
                    break;
                case AppState.DemoStepStartSessionForQuery:
                    await CloudManager.StartSessionAsync();
                    currentAppState = AppState.DemoStepLookForAnchorsNearDevice;
                    break;
                case AppState.DemoStepLookForAnchorsNearDevice:
                    currentAppState = AppState.DemoStepLookingForAnchorsNearDevice;
                    currentWatcher = CreateWatcher();
                    break;
                case AppState.DemoStepLookingForAnchorsNearDevice:
                    break;
                case AppState.DemoStepStopWatcher:
                    if (currentWatcher != null)
                    {
                        currentWatcher.Stop();
                        currentWatcher = null;
                    }
                    currentAppState = AppState.DemoStepDeleteAnchors;
                    break;
                case AppState.DemoStepDeleteAnchors:
                    currentAppState = AppState.DemoStepEnumerateAnchors;
                    break;
                case AppState.DemoStepEnumerateAnchors:
                    currentAppState = AppState.DemoStepStopSessionForQuery;
                    break;
                case AppState.DemoStepStopSessionForQuery:
                    CloudManager.StopSession();
                    currentWatcher = null;
                    locationProvider = null;
                    currentAppState = AppState.DemoStepComplete;
                    break;
                case AppState.DemoStepComplete:
                    currentCloudAnchor = null;
                    currentAppState = AppState.DemoStepCreateSession;
                    CleanupSpawnedObjects();
                    break;
                default:
                    Debug.Log("Shouldn't get here for app state " + currentAppState.ToString());
                    break;
            }
        }

        public void DeleteNewAnchor()
        {
            feedbackBox.text = "Deleting the anchor that was just created...";
            XRUXPicker.Instance.GetDemoButtons()[nextButtonIndex].interactable = false;
            XRUXPicker.Instance.GetDemoButtons()[deleteNewAnchorButtonIndex].interactable = false;
            bool deleteAllAnchorsButtonPreviousState = XRUXPicker.Instance.GetDemoButtons()[deleteAllAnchorsButtonIndex].interactable;
            XRUXPicker.Instance.GetDemoButtons()[deleteAllAnchorsButtonIndex].interactable = false;

            UnityDispatcher.InvokeOnAppThread(async () =>
            {
                if (currentCloudAnchor == null)
                {
                    feedbackBox.text = "Could not obtain identifier for anchor that was just created";
                    XRUXPicker.Instance.GetDemoButtons()[nextButtonIndex].interactable = true;
                    XRUXPicker.Instance.GetDemoButtons()[deleteNewAnchorButtonIndex].interactable = true;
                    XRUXPicker.Instance.GetDemoButtons()[deleteAllAnchorsButtonIndex].interactable = deleteAllAnchorsButtonPreviousState;
                    return;
                }

                // Acquire the CloudSpatialAnchor
                string newAnchorIdentifier = currentCloudAnchor.Identifier;
                CloudSpatialAnchor newAnchor = await CloudManager.Session.GetAnchorPropertiesAsync(newAnchorIdentifier);
                if (newAnchor == null)
                {
                    feedbackBox.text = $"Failed to acquire the CloudSpatialAnchor for identifier {newAnchorIdentifier}";
                    XRUXPicker.Instance.GetDemoButtons()[nextButtonIndex].interactable = true;
                    XRUXPicker.Instance.GetDemoButtons()[deleteNewAnchorButtonIndex].interactable = true;
                    XRUXPicker.Instance.GetDemoButtons()[deleteAllAnchorsButtonIndex].interactable = deleteAllAnchorsButtonPreviousState;
                    return;
                }

                // Delete from the service
                await CloudManager.DeleteAnchorAsync(newAnchor);

                // Destroy the GameObject if it exists
                GameObject newAnchorGameObject = allDiscoveredAnchors.Find(x => x.GetComponent<CloudNativeAnchor>().CloudAnchor.Identifier == newAnchorIdentifier);
                if (newAnchorGameObject != null)
                {
                    allDiscoveredAnchors.Remove(newAnchorGameObject);
                    Destroy(newAnchorGameObject);
                }

                feedbackBox.text = $"Deleted anchor that was just created";
                XRUXPicker.Instance.GetDemoButtons()[nextButtonIndex].interactable = true;
                XRUXPicker.Instance.GetDemoButtons()[deleteAllAnchorsButtonIndex].interactable = deleteAllAnchorsButtonPreviousState;
            });
        }

        public void DeleteAllFoundAnchors()
        {
            feedbackBox.text = "Deleting all found anchors...";
            XRUXPicker.Instance.GetDemoButtons()[nextButtonIndex].interactable = false;
            bool deleteNewAnchorButtonPreviousState = XRUXPicker.Instance.GetDemoButtons()[deleteNewAnchorButtonIndex].interactable;
            XRUXPicker.Instance.GetDemoButtons()[deleteNewAnchorButtonIndex].interactable = false;
            XRUXPicker.Instance.GetDemoButtons()[deleteAllAnchorsButtonIndex].interactable = false;

            UnityDispatcher.InvokeOnAppThread(async () =>
            {
                int deletedCount = 0;
                foreach (GameObject anchor in allDiscoveredAnchors)
                {
                    // Acquire the CloudSpatialAnchor
                    CloudNativeAnchor cloudNativeAnchor = anchor.GetComponent<CloudNativeAnchor>();
                    if (cloudNativeAnchor == null)
                    {
                        Debug.LogError("Found game object without CloudNativeAnchor");
                        continue;
                    }
                    if (cloudNativeAnchor.CloudAnchor == null)
                    {
                        Debug.LogError("Found CloudNativeAnchor without CloudSpatialAnchor");
                        continue;
                    }

                    // Delete from the service
                    await CloudManager.DeleteAnchorAsync(cloudNativeAnchor.CloudAnchor);

                    // Destroy the GameObject
                    Destroy(anchor);

                    // Check if this anchor was also the cuurentCloudAnchor
                    if (currentCloudAnchor != null && cloudNativeAnchor.CloudAnchor.Identifier == currentCloudAnchor.Identifier)
                    {
                        // Cleanup to prevent app from calling DeleteNewAnchor() after this
                        deleteNewAnchorButtonPreviousState = false;
                        currentCloudAnchor = null;
                    }

                    ++deletedCount;
                }

                allDiscoveredAnchors.Clear();

                feedbackBox.text = $"Deleted {deletedCount} anchors";
                XRUXPicker.Instance.GetDemoButtons()[nextButtonIndex].interactable = true;
                XRUXPicker.Instance.GetDemoButtons()[deleteNewAnchorButtonIndex].interactable = deleteNewAnchorButtonPreviousState;
            });
        }

        public void EnumerateAllNearbyAnchors()
        {
            feedbackBox.text = "Enumerating nearby anchors...";
            XRUXPicker.Instance.GetDemoButtons()[nextButtonIndex].interactable = false;
            XRUXPicker.Instance.GetDemoButtons()[enumerateButtonIndex].interactable = false;

            UnityDispatcher.InvokeOnAppThread(async () =>
            {
                NearDeviceCriteria criteria = new NearDeviceCriteria();
                criteria.DistanceInMeters = 5;
                criteria.MaxResultCount = 20;

                IList<string> spatialAnchorIds = await CloudManager.Session.GetNearbyAnchorIdsAsync(criteria);

                Debug.LogFormat("Got ids for {0} anchors", spatialAnchorIds.Count);

                List<CloudSpatialAnchor> spatialAnchors = new List<CloudSpatialAnchor>();

                foreach (string anchorId in spatialAnchorIds)
                {
                    CloudSpatialAnchor anchor = await CloudManager.Session.GetAnchorPropertiesAsync(anchorId);
                    Debug.LogFormat("Received information about spatial anchor {0}", anchor.Identifier);
                    spatialAnchors.Add(anchor);
                }

                feedbackBox.text = $"Found {spatialAnchors.Count} anchors nearby";
                XRUXPicker.Instance.GetDemoButtons()[nextButtonIndex].interactable = true;
                XRUXPicker.Instance.GetDemoButtons()[enumerateButtonIndex].interactable = true;
            });
        }

        protected override void CleanupSpawnedObjects()
        {
            base.CleanupSpawnedObjects();

            foreach (GameObject anchor in allDiscoveredAnchors)
            {
                Destroy(anchor);
            }
            allDiscoveredAnchors.Clear();
        }

        private void ConfigureSession()
        {
            const float distanceInMeters = 8.0f;
            const int maxAnchorsToFind = 25;
            SetNearDevice(distanceInMeters, maxAnchorsToFind);
        }

        private void ConfigureSensors()
        {
            locationProvider.Sensors.GeoLocationEnabled = SensorPermissionHelper.HasGeoLocationPermission();

            locationProvider.Sensors.WifiEnabled = SensorPermissionHelper.HasWifiPermission();

            locationProvider.Sensors.BluetoothEnabled = SensorPermissionHelper.HasBluetoothPermission();
            locationProvider.Sensors.KnownBeaconProximityUuids = CoarseRelocSettings.KnownBluetoothProximityUuids;
        }
    }
}
