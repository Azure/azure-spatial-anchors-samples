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
                return this._currentAppState;
            }
            set
            {
                if (this._currentAppState != value)
                {
                    Debug.LogFormat("State from {0} to {1}", this._currentAppState, value);
                    this._currentAppState = value;
                    if (this.spawnedObjectMat != null)
                    {
                        this.spawnedObjectMat.color = this.stateParams[this._currentAppState].StepColor;
                    }

                    if (!this.isErrorActive)
                    {
                        this.feedbackBox.text = this.stateParams[this._currentAppState].StepMessage;
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
            this.feedbackBox.text = this.stateParams[this.currentAppState].StepMessage;

            Debug.Log("Azure Spatial Anchors Demo script started");
        }

        protected override void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
        {
            base.OnCloudAnchorLocated(args);

            this.currentCloudAnchor = args.Anchor;

            this.QueueOnUpdate(() =>
            {
                this.currentAppState = AppState.DemoStepDeleteFoundAnchor;
                Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
            anchorPose = this.currentCloudAnchor.GetAnchorPose();
#endif
            // HoloLens: The position will be set based on the unityARUserAnchor that was located.

            this.SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);
            });
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        public override void Update()
        {
            base.Update();

            if (this.spawnedObjectMat != null)
            {
                float rat = 0.1f;
                float createProgress = this.CloudManager.GetSessionStatusIndicator(AzureSpatialAnchorsDemoWrapper.SessionStatusIndicatorType.RecommendedForCreate);
                rat += (Mathf.Min(createProgress, 1) * 0.9f);
                this.spawnedObjectMat.color = this.GetStepColor() * rat;
            }
        }

        protected override bool IsPlacingObject()
        {
            return this.currentAppState == AppState.DemoStepCreateLocalAnchor;
        }

        protected override Color GetStepColor()
        {
            return this.stateParams[this.currentAppState].StepColor;
        }

        protected override void OnSaveCloudAnchorSuccessful()
        {
            base.OnSaveCloudAnchorSuccessful();

            Debug.Log("Anchor created, yay!");

            this.currentAnchorId = this.currentCloudAnchor.Identifier;

            // Sanity check that the object is still where we expect
            this.QueueOnUpdate(new Action(() =>
            {
                Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
            anchorPose = this.currentCloudAnchor.GetAnchorPose();
#endif
            // HoloLens: The position will be set based on the unityARUserAnchor that was located.

            this.SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

                this.currentAppState = AppState.DemoStepStopSession;
            }));
        }

        protected override void OnSaveCloudAnchorFailed(Exception exception)
        {
            base.OnSaveCloudAnchorFailed(exception);

            this.currentAnchorId = string.Empty;
        }

        public override void AdvanceDemo()
        {
            switch (this.currentAppState)
            {
                case AppState.DemoStepCreateSession:
                    this.CloudManager.ResetSessionStatusIndicators();
                    this.currentAnchorId = "";
                    this.currentCloudAnchor = null;
                    this.currentAppState = AppState.DemoStepConfigSession;
                    break;
                case AppState.DemoStepConfigSession:
                    this.ConfigureSession();
                    this.currentAppState = AppState.DemoStepStartSession;
                    break;
                case AppState.DemoStepStartSession:
                    this.CloudManager.EnableProcessing = true;
                    this.currentAppState = AppState.DemoStepCreateLocalAnchor;
                    break;
                case AppState.DemoStepCreateLocalAnchor:
                    if (this.spawnedObject != null)
                    {
                        this.currentAppState = AppState.DemoStepSaveCloudAnchor;
                    }
                    break;
                case AppState.DemoStepSaveCloudAnchor:
                    this.currentAppState = AppState.DemoStepSavingCloudAnchor;
                    this.SaveCurrentObjectAnchorToCloud();
                    break;
                case AppState.DemoStepStopSession:
                    this.CloudManager.EnableProcessing = false;
                    this.CleanupSpawnedObjects();
                    this.CloudManager.ResetSession();
                    this.currentAppState = AppState.DemoStepCreateSessionForQuery;
                    break;
                case AppState.DemoStepCreateSessionForQuery:
                    this.ConfigureSession();
                    this.currentAppState = AppState.DemoStepStartSessionForQuery;
                    break;
                case AppState.DemoStepStartSessionForQuery:
                    this.CloudManager.EnableProcessing = true;
                    this.currentAppState = AppState.DemoStepLookForAnchor;
                    break;
                case AppState.DemoStepLookForAnchor:
                    this.currentAppState = AppState.DemoStepLookingForAnchor;
                    this.CloudManager.CreateWatcher();
                    break;
                case AppState.DemoStepLookingForAnchor:
                    break;
                case AppState.DemoStepDeleteFoundAnchor:
                    Task.Run(async () =>
                    {
                        await this.CloudManager.DeleteAnchorAsync(this.currentCloudAnchor);
                        this.currentCloudAnchor = null;
                    });
                    this.currentAppState = AppState.DemoStepStopSessionForQuery;
                    this.CleanupSpawnedObjects();
                    break;
                case AppState.DemoStepStopSessionForQuery:
                    this.CloudManager.EnableProcessing = false;
                    this.currentAppState = AppState.DemoStepComplete;
                    break;
                case AppState.DemoStepComplete:
                    this.currentCloudAnchor = null;
                    this.currentAppState = AppState.DemoStepCreateSession;
                    this.CleanupSpawnedObjects();
                    break;
                default:
                    Debug.Log("Shouldn't get here for app state " + this.currentAppState.ToString());
                    break;
            }
        }

        private void ConfigureSession()
        {
            List<string> anchorsToFind = new List<string>();
            if (this.currentAppState == AppState.DemoStepCreateSessionForQuery)
            {
                anchorsToFind.Add(this.currentAnchorId);
            }

            this.CloudManager.SetAnchorIdsToLocate(anchorsToFind);
        }
    }
}
