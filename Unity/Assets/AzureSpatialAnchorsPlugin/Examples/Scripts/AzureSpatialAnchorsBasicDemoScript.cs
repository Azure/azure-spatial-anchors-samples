// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity.Samples
{
    public class AzureSpatialAnchorsBasicDemoScript : DemoScriptBase
    {
        internal enum AppState
        {
            DemoStepCreateSession = 0,
            DemoStepConfigSession,
            DemoStepStartSession,
            DemoStepCreateLocalAnchor,
            DemoStepSaveCloudAnchor,
            DemoStepSavingCloudAnchor,
            DemoStepStopSession,
            DemoStepDestroySession,
            DemoStepCreateSessionForQuery,
            DemoStepStartSessionForQuery,
            DemoStepLookForAnchor,
            DemoStepLookingForAnchor,
            DemoStepDeleteFoundAnchor,
            DemoStepStopSessionForQuery,
            DemoStepComplete
        }

        private readonly Dictionary<AppState, DemoStepParams> stateParams = new Dictionary<AppState, DemoStepParams>
    {
        { AppState.DemoStepCreateSession,new DemoStepParams(){ StepMessage = "Next: Create Azure Spatial Anchors Session", StepColor = Color.clear }},
        { AppState.DemoStepConfigSession,new DemoStepParams(){ StepMessage = "Next: Configure Azure Spatial Anchors Session", StepColor = Color.clear }},
        { AppState.DemoStepStartSession,new DemoStepParams(){ StepMessage = "Next: Start Azure Spatial Anchors Session", StepColor = Color.clear }},
        { AppState.DemoStepCreateLocalAnchor,new DemoStepParams(){ StepMessage = "Tap a surface to add the Local Anchor.", StepColor = Color.blue }},
        { AppState.DemoStepSaveCloudAnchor,new DemoStepParams(){ StepMessage = "Next: Save Local Anchor to cloud", StepColor = Color.yellow }},
        { AppState.DemoStepSavingCloudAnchor,new DemoStepParams(){ StepMessage = "Saving local Anchor to cloud...", StepColor = Color.yellow }},
        { AppState.DemoStepStopSession,new DemoStepParams(){ StepMessage = "Next: Stop Azure Spatial Anchors Session", StepColor = Color.green }},
        { AppState.DemoStepCreateSessionForQuery,new DemoStepParams(){ StepMessage = "Next: Create Azure Spatial Anchors Session for query", StepColor = Color.clear }},
        { AppState.DemoStepStartSessionForQuery,new DemoStepParams(){ StepMessage = "Next: Start Azure Spatial Anchors Session for query", StepColor = Color.clear }},
        { AppState.DemoStepLookForAnchor,new DemoStepParams(){ StepMessage = "Next: Look for Anchor", StepColor = Color.white }},
        { AppState.DemoStepLookingForAnchor,new DemoStepParams(){ StepMessage = "Looking for Anchor...", StepColor = Color.white }},
        { AppState.DemoStepDeleteFoundAnchor,new DemoStepParams(){ StepMessage = "Next: Delete Anchor", StepColor = Color.red }},
        { AppState.DemoStepStopSessionForQuery,new DemoStepParams(){ StepMessage = "Next: Stop Azure Spatial Anchors Session for query", StepColor = Color.clear }},
        { AppState.DemoStepComplete,new DemoStepParams(){ StepMessage = "Next: Restart demo", StepColor = Color.clear }}
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
                }
            }
        }

        private string currentAnchorId = "";

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
        }

        protected override void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
        {
            base.OnCloudAnchorLocated(args);

            if (args.Status == LocateAnchorStatus.Located)
            {
                currentCloudAnchor = args.Anchor;

                QueueOnUpdate(() =>
                {
                    currentAppState = AppState.DemoStepDeleteFoundAnchor;
                    Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
            anchorPose = currentCloudAnchor.GetAnchorPose();
#endif
                // HoloLens: The position will be set based on the unityARUserAnchor that was located.

                SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);
                });
            }
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
                float createProgress = CloudManager.GetSessionStatusIndicator(AzureSpatialAnchorsDemoWrapper.SessionStatusIndicatorType.RecommendedForCreate);
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

        protected override void OnSaveCloudAnchorSuccessful()
        {
            base.OnSaveCloudAnchorSuccessful();

            Debug.Log("Anchor created, yay!");

            currentAnchorId = currentCloudAnchor.Identifier;

            // Sanity check that the object is still where we expect
            QueueOnUpdate(new Action(() =>
            {
                Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
            anchorPose = currentCloudAnchor.GetAnchorPose();
#endif
            // HoloLens: The position will be set based on the unityARUserAnchor that was located.

            SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

                currentAppState = AppState.DemoStepStopSession;
            }));
        }

        protected override void OnSaveCloudAnchorFailed(Exception exception)
        {
            base.OnSaveCloudAnchorFailed(exception);

            currentAnchorId = string.Empty;
        }

        public override void AdvanceDemo()
        {
            switch (currentAppState)
            {
                case AppState.DemoStepCreateSession:
                    CloudManager.ResetSessionStatusIndicators();
                    currentAnchorId = "";
                    currentCloudAnchor = null;
                    currentAppState = AppState.DemoStepConfigSession;
                    break;
                case AppState.DemoStepConfigSession:
                    ConfigureSession();
                    currentAppState = AppState.DemoStepStartSession;
                    break;
                case AppState.DemoStepStartSession:
                    CloudManager.EnableProcessing = true;
                    currentAppState = AppState.DemoStepCreateLocalAnchor;
                    break;
                case AppState.DemoStepCreateLocalAnchor:
                    if (spawnedObject != null)
                    {
                        currentAppState = AppState.DemoStepSaveCloudAnchor;
                    }
                    break;
                case AppState.DemoStepSaveCloudAnchor:
                    currentAppState = AppState.DemoStepSavingCloudAnchor;
                    SaveCurrentObjectAnchorToCloud();
                    break;
                case AppState.DemoStepStopSession:
                    CloudManager.EnableProcessing = false;
                    CleanupSpawnedObjects();
                    CloudManager.ResetSession();
                    currentAppState = AppState.DemoStepCreateSessionForQuery;
                    break;
                case AppState.DemoStepCreateSessionForQuery:
                    ConfigureSession();
                    currentAppState = AppState.DemoStepStartSessionForQuery;
                    break;
                case AppState.DemoStepStartSessionForQuery:
                    CloudManager.EnableProcessing = true;
                    currentAppState = AppState.DemoStepLookForAnchor;
                    break;
                case AppState.DemoStepLookForAnchor:
                    currentAppState = AppState.DemoStepLookingForAnchor;
                    currentWatcher = CloudManager.CreateWatcher();
                    break;
                case AppState.DemoStepLookingForAnchor:
                    break;
                case AppState.DemoStepDeleteFoundAnchor:
                    Task.Run(async () =>
                    {
                        await CloudManager.DeleteAnchorAsync(currentCloudAnchor);
                        currentCloudAnchor = null;
                    });
                    currentAppState = AppState.DemoStepStopSessionForQuery;
                    CleanupSpawnedObjects();
                    break;
                case AppState.DemoStepStopSessionForQuery:
                    CloudManager.EnableProcessing = false;
                    currentWatcher = null;
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

        private void ConfigureSession()
        {
            List<string> anchorsToFind = new List<string>();
            if (currentAppState == AppState.DemoStepCreateSessionForQuery)
            {
                anchorsToFind.Add(currentAnchorId);
            }

            CloudManager.SetAnchorIdsToLocate(anchorsToFind);
        }
    }
}
