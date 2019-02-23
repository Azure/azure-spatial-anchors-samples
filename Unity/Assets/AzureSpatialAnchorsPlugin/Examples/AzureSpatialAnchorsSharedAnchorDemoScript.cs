// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.Azure.SpatialAnchors.Unity.Samples
{
    public class AzureSpatialAnchorsSharedAnchorDemoScript : DemoScriptBase
    {
        internal enum AppState
        {
            DemoStepChooseFlow = 0,
            DemoStepCreateSession,
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

        internal enum DemoFlow
        {
            CreateFlow = 0,
            LocateFlow
        }

        private readonly Dictionary<AppState, DemoStepParams> stateParams = new Dictionary<AppState, DemoStepParams>
    {
        { AppState.DemoStepChooseFlow,new DemoStepParams(){ StepMessage = "Next: Choose your Demo Flow", StepColor = Color.clear }},
        { AppState.DemoStepCreateSession,new DemoStepParams(){ StepMessage = "Next: Create CloudSpatialAnchorSession", StepColor = Color.clear }},
        { AppState.DemoStepConfigSession,new DemoStepParams(){ StepMessage = "Next: Configure CloudSpatialAnchorSession", StepColor = Color.clear }},
        { AppState.DemoStepStartSession,new DemoStepParams(){ StepMessage = "Next: Start CloudSpatialAnchorSession", StepColor = Color.clear }},
        { AppState.DemoStepCreateLocalAnchor,new DemoStepParams(){ StepMessage = "Tap a surface to add the local anchor.", StepColor = Color.blue }},
        { AppState.DemoStepSaveCloudAnchor,new DemoStepParams(){ StepMessage = "Next: Save local anchor to cloud", StepColor = Color.yellow }},
        { AppState.DemoStepSavingCloudAnchor,new DemoStepParams(){ StepMessage = "Saving local anchor to cloud...", StepColor = Color.yellow }},
        { AppState.DemoStepStopSession,new DemoStepParams(){ StepMessage = "Next: Stop cloud anchor session", StepColor = Color.green }},
        { AppState.DemoStepDestroySession,new DemoStepParams(){ StepMessage = "Next: Destroy Cloud Anchor session", StepColor = Color.clear }},
        { AppState.DemoStepCreateSessionForQuery,new DemoStepParams(){ StepMessage = "Next: Create CloudSpatialAnchorSession for query", StepColor = Color.clear }},
        { AppState.DemoStepStartSessionForQuery,new DemoStepParams(){ StepMessage = "Next: Start CloudSpatialAnchorSession for query", StepColor = Color.clear }},
        { AppState.DemoStepLookForAnchor,new DemoStepParams(){ StepMessage = "Next: Look for anchor", StepColor = Color.white }},
        { AppState.DemoStepLookingForAnchor,new DemoStepParams(){ StepMessage = "Looking for anchor...", StepColor = Color.white }},
        { AppState.DemoStepDeleteFoundAnchor,new DemoStepParams(){ StepMessage = "Next: Delete anchor", StepColor = Color.red }},
        { AppState.DemoStepStopSessionForQuery,new DemoStepParams(){ StepMessage = "Next: Stop CloudSpatialAnchorSession for query", StepColor = Color.clear }},
        { AppState.DemoStepComplete,new DemoStepParams(){ StepMessage = "Next: Restart demo", StepColor = Color.clear }}
    };

#if !UNITY_EDITOR
        public AnchorExchanger anchorExchanger = new AnchorExchanger();
#endif

#if UNITY_ANDROID || UNITY_IOS
        private static bool _runningOnHoloLens = false;
#else
        private static bool _runningOnHoloLens = true;
#endif

        private AppState _currentAppState = _runningOnHoloLens ? AppState.DemoStepCreateSession : AppState.DemoStepChooseFlow;
        private DemoFlow _currentDemoFlow = DemoFlow.CreateFlow;

        public string BaseSharingUrl = "";
        private readonly List<GameObject> otherSpawnedObjects = new List<GameObject>();
        private int anchorsLocated = 0;
        private int anchorsExpected = 0;
        private readonly List<string> localAnchorIds = new List<string>();
        private string _anchorKeyToFind;
        private long? _anchorNumberToFind;

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

                    this.feedbackBox.text = this.stateParams[this._currentAppState].StepMessage;
                }
            }
        }

        protected override void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
        {
            base.OnCloudAnchorLocated(args);

            CloudSpatialAnchor nextCsa = args.Anchor;
            this.currentCloudAnchor = args.Anchor;

            this.QueueOnUpdate(new Action(() =>
            {
                this.anchorsLocated++;
                this.currentCloudAnchor = nextCsa;
                Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
                anchorPose = this.currentCloudAnchor.GetAnchorPose();
#endif
                // HoloLens: The position will be set based on the unityARUserAnchor that was located.

                GameObject nextObject = this.SpawnNewAnchoredObject(anchorPose.position, anchorPose.rotation, this.currentCloudAnchor);
                this.AttachTextMesh(nextObject, _anchorNumberToFind);
                this.otherSpawnedObjects.Add(nextObject);

                if (this.anchorsLocated >= this.anchorsExpected)
                {
                    this.currentAppState = AppState.DemoStepStopSessionForQuery;
                }
            }));
        }

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any
        /// of the Update methods are called the first time.
        /// </summary>
        public override void Start()
        {
            base.Start();

            Debug.Log(">>MRCloud Demo Script Start");

            if (!SanityCheckAccessConfiguration())
            {
                return;
            }

            if (string.IsNullOrEmpty(this.BaseSharingUrl))
            {
                this.feedbackBox.text = "Need to set the BaseSharingUrl on the AzureSpatialAnchors object in your scene.";
                return;
            }
            else
            {
                Uri result;
                if (!Uri.TryCreate(this.BaseSharingUrl, UriKind.Absolute, out result))
                {
                    this.feedbackBox.text = "BaseSharingUrl, on the AzureSpatialAnchors object in your scene, is not a valid url";
                    return;
                }
                else
                {
                    this.BaseSharingUrl = $"{result.Scheme}://{result.Host}/api/anchors";
                }
            }

#if !UNITY_EDITOR
            anchorExchanger.WatchKeys(this.BaseSharingUrl);
#endif

            this.feedbackBox.text = this.stateParams[this.currentAppState].StepMessage;

            Debug.Log("MRCloud Demo script started");
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
                this.spawnedObjectMat.color = this.stateParams[this.currentAppState].StepColor * rat;
            }
        }

        protected override bool IsPlacingObject()
        {
            return this.currentAppState == AppState.DemoStepCreateLocalAnchor;
        }

        protected override Color GetStepColor()
        {
            if (this.currentCloudAnchor == null || this.localAnchorIds.Contains(this.currentCloudAnchor.Identifier))
            {
                return this.stateParams[this.currentAppState].StepColor;
            }

            return Color.magenta;
        }

        private void AttachTextMesh(GameObject parentObject, long? dataToAttach)
        {
            GameObject go = new GameObject();

            TextMesh tm = go.AddComponent<TextMesh>();
            if (!dataToAttach.HasValue)
            {
                tm.text = string.Format("{0}:{1}", this.localAnchorIds.Contains(this.currentCloudAnchor.Identifier) ? "L" : "R", this.currentCloudAnchor.Identifier);
            }
            else if (dataToAttach != -1)
            {
                tm.text = $"Anchor Number:{dataToAttach}";
            }
            else
            {
                tm.text = $"Failed to store the anchor key using '{this.BaseSharingUrl}'";
            }
            tm.fontSize = 32;
            go.transform.SetParent(parentObject.transform, false);
            go.transform.localPosition = Vector3.one * 0.25f;
            go.transform.rotation = Quaternion.AngleAxis(0, Vector3.up);
            go.transform.localScale = Vector3.one * .1f;

            this.otherSpawnedObjects.Add(go);
        }

        protected async override void OnSaveCloudAnchorSuccessful()
        {
            base.OnSaveCloudAnchorSuccessful();

            long anchorNumber = -1;

            this.localAnchorIds.Add(this.currentCloudAnchor.Identifier);

#if !UNITY_EDITOR
            anchorNumber = (await this.anchorExchanger.StoreAnchorKey(currentCloudAnchor.Identifier));
#endif

            this.QueueOnUpdate(new Action(() =>
            {
                Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
                anchorPose = this.currentCloudAnchor.GetAnchorPose();
#endif
                // HoloLens: The position will be set based on the unityARUserAnchor that was located.

                this.SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);
                if (_runningOnHoloLens)
                {
                    this.AttachTextMesh(this.spawnedObject, null);
                }
                else
                {
                    this.AttachTextMesh(this.spawnedObject, anchorNumber);
                }

                this.currentAppState = AppState.DemoStepStopSession;
            }));
        }

        protected override void OnSaveCloudAnchorFailed(Exception exception)
        {
            base.OnSaveCloudAnchorFailed(exception);
        }

        public override void AdvanceDemo()
        {
            if (this._currentDemoFlow == DemoFlow.CreateFlow)
            {
                AdvanceCreateFlowDemo();
            }
            else if (this._currentDemoFlow == DemoFlow.LocateFlow)
            {
                AdvanceLocateFlowDemo();
            }
        }

        public void InitializeCreateFlowDemo()
        {
            if (this.currentAppState == AppState.DemoStepChooseFlow)
            {
                this._currentDemoFlow = DemoFlow.CreateFlow;
                this.currentAppState = AppState.DemoStepCreateSession;
                SetChooseFlowUIVisibility(false);
            }
            else
            {
                AdvanceDemo();
            }
        }

        public async void InitializeLocateFlowDemo()
        {
            if (this.currentAppState == AppState.DemoStepChooseFlow)
            {
                long anchorNumber;
                string inputText = XRUXPickerForSharedAnchorDemo.Instance.GetDemoInputField().text;
                if (!long.TryParse(inputText, out anchorNumber))
                {
                    this.feedbackBox.text = "Invalid Anchor Number!";
                }
                else
                {
                    _anchorNumberToFind = anchorNumber;
#if !UNITY_EDITOR
                    _anchorKeyToFind = await anchorExchanger.RetrieveAnchorKey(_anchorNumberToFind.Value);
#endif
                    if (_anchorKeyToFind == null)
                    {
                        this.feedbackBox.text = "Anchor Number Not Found!";
                    }
                    else
                    {
                        this._currentDemoFlow = DemoFlow.LocateFlow;
                        this.currentAppState = AppState.DemoStepCreateSession;
                        SetChooseFlowUIVisibility(false);
                    }
                }
            }
            else
            {
                AdvanceDemo();
            }
        }

        private void AdvanceCreateFlowDemo()
        {
            switch (this.currentAppState)
            {
                case AppState.DemoStepCreateSession:
                    this.CloudManager.ResetSessionStatusIndicators();
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
                    this.SaveCurrentObjectAnchorToCloud();
                    this.currentAppState = AppState.DemoStepSavingCloudAnchor;
                    break;
                case AppState.DemoStepStopSession:
                    this.CloudManager.EnableProcessing = false;
                    this.CleanupSpawnedObjects();
                    this.CloudManager.ResetSession();
                    if (_runningOnHoloLens)
                    {
                        this.currentAppState = AppState.DemoStepCreateSessionForQuery;
                        this._currentDemoFlow = DemoFlow.LocateFlow;
                    }
                    else
                    {
                        this.currentAppState = AppState.DemoStepComplete;
                    }
                    break;
                case AppState.DemoStepComplete:
                    this.currentCloudAnchor = null;
                    this.currentAppState = AppState.DemoStepChooseFlow;
                    SetChooseFlowUIVisibility(true);
                    this.CleanupSpawnedObjects();
                    break;
                default:
                    Debug.Log("Shouldn't get here for app state " + this.currentAppState);
                    break;
            }
        }

        private void AdvanceLocateFlowDemo()
        {
            switch (this.currentAppState)
            {
                case AppState.DemoStepCreateSession:
                    this.currentAppState = AppState.DemoStepChooseFlow;
                    this.CloudManager.ResetSessionStatusIndicators();
                    this.currentCloudAnchor = null;
                    this.currentAppState = AppState.DemoStepCreateSessionForQuery;
                    break;
                case AppState.DemoStepCreateSessionForQuery:
                    this.anchorsLocated = 0;
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
                    // Advancement will take place when anchors have all been located.
                    break;
                case AppState.DemoStepStopSessionForQuery:
                    this.CloudManager.EnableProcessing = false;
                    this.CloudManager.ResetSession();
                    this.currentAppState = AppState.DemoStepComplete;
                    break;
                case AppState.DemoStepComplete:
                    this.currentCloudAnchor = null;
                    if (_runningOnHoloLens)
                    {
                        this.currentAppState = AppState.DemoStepCreateSession;
                        this._currentDemoFlow = DemoFlow.CreateFlow;
                    }
                    else
                    {
                        this.currentAppState = AppState.DemoStepChooseFlow;
                        SetChooseFlowUIVisibility(true);
                    }
                    this.CleanupSpawnedObjects();
                    break;
                default:
                    Debug.Log("Shouldn't get here for app state " + this.currentAppState);
                    break;
            }
        }

        private void SetChooseFlowUIVisibility(bool visibility)
        {
            XRUXPickerForSharedAnchorDemo.Instance.GetDemoButtons()[1].gameObject.SetActive(visibility);
            XRUXPickerForSharedAnchorDemo.Instance.GetDemoInputField().gameObject.SetActive(visibility);
            XRUXPickerForSharedAnchorDemo.Instance.GetDemoButtons()[0].transform.Find("Text").GetComponent<Text>().text = visibility ? "Create & Share Anchor" : "Next Step";
        }

        private void ConfigureSession()
        {
            List<string> anchorsToFind = new List<string>();

            if (currentAppState == AppState.DemoStepCreateSessionForQuery)
            {
                if (_runningOnHoloLens)
                {
#if !UNITY_EDITOR
                    anchorsToFind.AddRange(anchorExchanger.AnchorKeys);
#endif
                }
                else
                {
                    anchorsToFind.Add(_anchorKeyToFind);
                }
            }
            {
                this.anchorsExpected = anchorsToFind.Count;
                this.CloudManager.SetAnchorIdsToLocate(anchorsToFind);
            }
        }

        protected override void CleanupSpawnedObjects()
        {
            base.CleanupSpawnedObjects();

            for (int index = 0; index < this.otherSpawnedObjects.Count; index++)
            {
                Destroy(this.otherSpawnedObjects[index]);
            }

            this.otherSpawnedObjects.Clear();
        }
    }
}
