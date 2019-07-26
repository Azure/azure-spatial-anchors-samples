// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    public class AzureSpatialAnchorsSharedAnchorDemoScript : DemoScriptBase
    {
        internal enum AppState
        {
            DemoStepChooseFlow = 0,
            DemoStepInputAnchorNumber,
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
            DemoStepStopSessionForQuery,
            DemoStepComplete,
        }

        internal enum DemoFlow
        {
            CreateFlow = 0,
            LocateFlow
        }

        private readonly Dictionary<AppState, DemoStepParams> stateParams = new Dictionary<AppState, DemoStepParams>
        {
            { AppState.DemoStepChooseFlow,new DemoStepParams() { StepMessage = "Next: Choose your Demo Flow", StepColor = Color.clear }},
            { AppState.DemoStepInputAnchorNumber,new DemoStepParams() { StepMessage = "Next: Input anchor number", StepColor = Color.clear }},
            { AppState.DemoStepCreateSession,new DemoStepParams() { StepMessage = "Next: Create CloudSpatialAnchorSession", StepColor = Color.clear }},
            { AppState.DemoStepConfigSession,new DemoStepParams() { StepMessage = "Next: Configure CloudSpatialAnchorSession", StepColor = Color.clear }},
            { AppState.DemoStepStartSession,new DemoStepParams() { StepMessage = "Next: Start CloudSpatialAnchorSession", StepColor = Color.clear }},
            { AppState.DemoStepCreateLocalAnchor,new DemoStepParams() { StepMessage = "Tap a surface to add the local anchor.", StepColor = Color.blue }},
            { AppState.DemoStepSaveCloudAnchor,new DemoStepParams() { StepMessage = "Next: Save local anchor to cloud", StepColor = Color.yellow }},
            { AppState.DemoStepSavingCloudAnchor,new DemoStepParams() { StepMessage = "Saving local anchor to cloud...", StepColor = Color.yellow }},
            { AppState.DemoStepStopSession,new DemoStepParams() { StepMessage = "Next: Stop cloud anchor session", StepColor = Color.green }},
            { AppState.DemoStepDestroySession,new DemoStepParams() { StepMessage = "Next: Destroy Cloud Anchor session", StepColor = Color.clear }},
            { AppState.DemoStepCreateSessionForQuery,new DemoStepParams() { StepMessage = "Next: Create CloudSpatialAnchorSession for query", StepColor = Color.clear }},
            { AppState.DemoStepStartSessionForQuery,new DemoStepParams() { StepMessage = "Next: Start CloudSpatialAnchorSession for query", StepColor = Color.clear }},
            { AppState.DemoStepLookForAnchor,new DemoStepParams() { StepMessage = "Next: Look for anchor", StepColor = Color.clear }},
            { AppState.DemoStepLookingForAnchor,new DemoStepParams() { StepMessage = "Looking for anchor...", StepColor = Color.clear }},
            { AppState.DemoStepStopSessionForQuery,new DemoStepParams() { StepMessage = "Next: Stop CloudSpatialAnchorSession for query", StepColor = Color.yellow }},
            { AppState.DemoStepComplete,new DemoStepParams() { StepMessage = "Next: Restart demo", StepColor = Color.clear }}
        };

        #if !UNITY_EDITOR
        public AnchorExchanger anchorExchanger = new AnchorExchanger();
        #endif

        #region Member Variables
        private AppState _currentAppState = AppState.DemoStepChooseFlow;
        private DemoFlow _currentDemoFlow = DemoFlow.CreateFlow;
        private readonly List<GameObject> otherSpawnedObjects = new List<GameObject>();
        private int anchorsLocated = 0;
        private int anchorsExpected = 0;
        private readonly List<string> localAnchorIds = new List<string>();
        private string _anchorKeyToFind = null;
        private long? _anchorNumberToFind;
        #endregion // Member Variables

        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("The base URL for the example sharing service.")]
        private string baseSharingUrl = "";
        #endregion // Unity Inspector Variables

        private AppState currentAppState
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

                    feedbackBox.text = stateParams[_currentAppState].StepMessage;
                    EnableCorrectUIControls();
                }
            }
        }

        protected override void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
        {
            base.OnCloudAnchorLocated(args);

            if (args.Status == LocateAnchorStatus.Located)
            {
                CloudSpatialAnchor nextCsa = args.Anchor;
                currentCloudAnchor = args.Anchor;

                UnityDispatcher.InvokeOnAppThread(() =>
                {
                    anchorsLocated++;
                    currentCloudAnchor = nextCsa;
                    Pose anchorPose = Pose.identity;

                    #if UNITY_ANDROID || UNITY_IOS
                    anchorPose = currentCloudAnchor.GetPose();
                    #endif
                    // HoloLens: The position will be set based on the unityARUserAnchor that was located.

                    GameObject nextObject = SpawnNewAnchoredObject(anchorPose.position, anchorPose.rotation, currentCloudAnchor);
                    spawnedObjectMat = nextObject.GetComponent<MeshRenderer>().material;
                    AttachTextMesh(nextObject, _anchorNumberToFind);
                    otherSpawnedObjects.Add(nextObject);

                    if (anchorsLocated >= anchorsExpected)
                    {
                        currentAppState = AppState.DemoStepStopSessionForQuery;
                    }
                });
            }
        }

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any
        /// of the Update methods are called the first time.
        /// </summary>
        public override void Start()
        {
            base.Start();

            if (!SanityCheckAccessConfiguration())
            {
                XRUXPickerForSharedAnchorDemo.Instance.GetDemoButtons()[1].gameObject.SetActive(false);
                XRUXPickerForSharedAnchorDemo.Instance.GetDemoButtons()[0].gameObject.SetActive(false);
                XRUXPickerForSharedAnchorDemo.Instance.GetDemoInputField().gameObject.SetActive(false);
                return;
            }

            SpatialAnchorSamplesConfig samplesConfig = Resources.Load<SpatialAnchorSamplesConfig>("SpatialAnchorSamplesConfig");
            if (string.IsNullOrWhiteSpace(BaseSharingUrl) && samplesConfig != null)
            {
                BaseSharingUrl = samplesConfig.BaseSharingURL;
            }

            if (string.IsNullOrEmpty(BaseSharingUrl))
            {
                feedbackBox.text = $"Need to set {nameof(BaseSharingUrl)}.";
                XRUXPickerForSharedAnchorDemo.Instance.GetDemoButtons()[1].gameObject.SetActive(false);
                XRUXPickerForSharedAnchorDemo.Instance.GetDemoButtons()[0].gameObject.SetActive(false);
                XRUXPickerForSharedAnchorDemo.Instance.GetDemoInputField().gameObject.SetActive(false);
                return;
            }
            else
            {
                Uri result;
                if (!Uri.TryCreate(BaseSharingUrl, UriKind.Absolute, out result))
                {
                    feedbackBox.text = $"{nameof(BaseSharingUrl)} is not a valid url";
                    return;
                }
                else
                {
                    BaseSharingUrl = $"{result.Scheme}://{result.Host}/api/anchors";
                }
            }

            #if !UNITY_EDITOR
            anchorExchanger.WatchKeys(BaseSharingUrl);
            #endif

            feedbackBox.text = stateParams[currentAppState].StepMessage;

            Debug.Log("Azure Spatial Anchors Shared Demo script started");
            EnableCorrectUIControls();
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
                spawnedObjectMat.color = stateParams[currentAppState].StepColor * rat;
            }
        }

        protected override bool IsPlacingObject()
        {
            return currentAppState == AppState.DemoStepCreateLocalAnchor;
        }

        protected override Color GetStepColor()
        {
            if (currentCloudAnchor == null || localAnchorIds.Contains(currentCloudAnchor.Identifier))
            {
                return stateParams[currentAppState].StepColor;
            }

            return Color.magenta;
        }

        private void AttachTextMesh(GameObject parentObject, long? dataToAttach)
        {
            GameObject go = new GameObject();

            TextMesh tm = go.AddComponent<TextMesh>();
            if (!dataToAttach.HasValue)
            {
                tm.text = string.Format("{0}:{1}", localAnchorIds.Contains(currentCloudAnchor.Identifier) ? "L" : "R", currentCloudAnchor.Identifier);
            }
            else if (dataToAttach != -1)
            {
                tm.text = $"Anchor Number:{dataToAttach}";
            }
            else
            {
                tm.text = $"Failed to store the anchor key using '{BaseSharingUrl}'";
            }
            tm.fontSize = 32;
            go.transform.SetParent(parentObject.transform, false);
            go.transform.localPosition = Vector3.one * 0.25f;
            go.transform.rotation = Quaternion.AngleAxis(0, Vector3.up);
            go.transform.localScale = Vector3.one * .1f;

            otherSpawnedObjects.Add(go);
        }

#pragma warning disable CS1998 // Conditional compile statements are removing await
        protected override async Task OnSaveCloudAnchorSuccessfulAsync()
#pragma warning restore CS1998

        {
            await base.OnSaveCloudAnchorSuccessfulAsync();

            long anchorNumber = -1;

            localAnchorIds.Add(currentCloudAnchor.Identifier);

            #if !UNITY_EDITOR
            anchorNumber = (await anchorExchanger.StoreAnchorKey(currentCloudAnchor.Identifier));
            #endif

            Pose anchorPose = Pose.identity;

            #if UNITY_ANDROID || UNITY_IOS
            anchorPose = currentCloudAnchor.GetPose();
            #endif
            // HoloLens: The position will be set based on the unityARUserAnchor that was located.

            SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

            AttachTextMesh(spawnedObject, anchorNumber);

            currentAppState = AppState.DemoStepStopSession;

            feedbackBox.text = $"Created anchor {anchorNumber}. Next: Stop cloud anchor session";
        }

        protected override void OnSaveCloudAnchorFailed(Exception exception)
        {
            base.OnSaveCloudAnchorFailed(exception);
        }

        public async override Task AdvanceDemoAsync()
        {
            if (currentAppState == AppState.DemoStepChooseFlow || currentAppState == AppState.DemoStepInputAnchorNumber)
            {
                return;
            }

            if (_currentDemoFlow == DemoFlow.CreateFlow)
            {
                await AdvanceCreateFlowDemoAsync();
            }
            else if (_currentDemoFlow == DemoFlow.LocateFlow)
            {
                await AdvanceLocateFlowDemoAsync();
            }
        }

        public async Task InitializeCreateFlowDemoAsync()
        {
            if (currentAppState == AppState.DemoStepChooseFlow)
            {
                _currentDemoFlow = DemoFlow.CreateFlow;
                currentAppState = AppState.DemoStepCreateSession;
            }
            else
            {
                await AdvanceDemoAsync();
            }
        }

        /// <summary>
        /// This version only exists for Unity to wire up a button click to.
        /// If calling from code, please use the Async version above.
        /// </summary>
        public async void InitializeCreateFlowDemo()
        {
            try
            {
                await InitializeCreateFlowDemoAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(AzureSpatialAnchorsSharedAnchorDemoScript)} - Error in {nameof(InitializeCreateFlowDemo)}: {ex.Message}");
            }
        }


#pragma warning disable CS1998 // Conditional compile statements are removing await
        public async Task InitializeLocateFlowDemoAsync()
#pragma warning restore CS1998
        {
            if (currentAppState == AppState.DemoStepChooseFlow)
            {
                currentAppState = AppState.DemoStepInputAnchorNumber;
            }
            else if (currentAppState == AppState.DemoStepInputAnchorNumber)
            {
                long anchorNumber;
                string inputText = XRUXPickerForSharedAnchorDemo.Instance.GetDemoInputField().text;
                if (!long.TryParse(inputText, out anchorNumber))
                {
                    feedbackBox.text = "Invalid Anchor Number!";
                }
                else
                {
                    _anchorNumberToFind = anchorNumber;
                    #if !UNITY_EDITOR
                    _anchorKeyToFind = await anchorExchanger.RetrieveAnchorKey(_anchorNumberToFind.Value);
                    #endif
                    if (_anchorKeyToFind == null)
                    {
                        feedbackBox.text = "Anchor Number Not Found!";
                    }
                    else
                    {
                        _currentDemoFlow = DemoFlow.LocateFlow;
                        currentAppState = AppState.DemoStepCreateSession;
                        XRUXPickerForSharedAnchorDemo.Instance.GetDemoInputField().text = "";
                    }
                }
            }
            else
            {
                await AdvanceDemoAsync();
            }
        }

        /// <summary>
        /// This version only exists for Unity to wire up a button click to.
        /// If calling from code, please use the Async version above.
        /// </summary>
        public async void InitializeLocateFlowDemo()
        {
            try
            {
                await InitializeLocateFlowDemoAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(AzureSpatialAnchorsSharedAnchorDemoScript)} - Error in {nameof(InitializeLocateFlowDemo)}: {ex.Message}");
            }
        }

        private async Task AdvanceCreateFlowDemoAsync()
        {
            switch (currentAppState)
            {
                case AppState.DemoStepCreateSession:
                    currentCloudAnchor = null;
                    currentAppState = AppState.DemoStepConfigSession;
                    break;
                case AppState.DemoStepConfigSession:
                    ConfigureSession();
                    currentAppState = AppState.DemoStepStartSession;
                    break;
                case AppState.DemoStepStartSession:
                    await CloudManager.StartSessionAsync();
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
                    await SaveCurrentObjectAnchorToCloudAsync();
                    break;
                case AppState.DemoStepStopSession:
                    CloudManager.StopSession();
                    CleanupSpawnedObjects();
                    await CloudManager.ResetSessionAsync();
                    currentAppState = AppState.DemoStepComplete;
                    break;
                case AppState.DemoStepComplete:
                    currentCloudAnchor = null;
                    currentAppState = AppState.DemoStepChooseFlow;
                    CleanupSpawnedObjects();
                    break;
                default:
                    Debug.Log("Shouldn't get here for app state " + currentAppState);
                    break;
            }
        }

        private async Task AdvanceLocateFlowDemoAsync()
        {
            switch (currentAppState)
            {
                case AppState.DemoStepCreateSession:
                    currentAppState = AppState.DemoStepChooseFlow;
                    currentCloudAnchor = null;
                    currentAppState = AppState.DemoStepCreateSessionForQuery;
                    break;
                case AppState.DemoStepCreateSessionForQuery:
                    anchorsLocated = 0;
                    ConfigureSession();
                    currentAppState = AppState.DemoStepStartSessionForQuery;
                    break;
                case AppState.DemoStepStartSessionForQuery:
                    await CloudManager.StartSessionAsync();
                    currentAppState = AppState.DemoStepLookForAnchor;
                    break;
                case AppState.DemoStepLookForAnchor:
                    currentAppState = AppState.DemoStepLookingForAnchor;
                    currentWatcher = CreateWatcher();
                    break;
                case AppState.DemoStepLookingForAnchor:
                    // Advancement will take place when anchors have all been located.
                    break;
                case AppState.DemoStepStopSessionForQuery:
                    CloudManager.StopSession();
                    currentAppState = AppState.DemoStepComplete;
                    break;
                case AppState.DemoStepComplete:
                    currentCloudAnchor = null;
                    currentWatcher = null;
                    currentAppState = AppState.DemoStepChooseFlow;
                    CleanupSpawnedObjects();
                    break;
                default:
                    Debug.Log("Shouldn't get here for app state " + currentAppState);
                    break;
            }
        }

        private void EnableCorrectUIControls()
        {
            switch (currentAppState)
            {
                case AppState.DemoStepChooseFlow:

                    XRUXPickerForSharedAnchorDemo.Instance.GetDemoButtons()[1].gameObject.SetActive(true);
                    #if UNITY_WSA
                    XRUXPickerForSharedAnchorDemo.Instance.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.1f;
                    XRUXPickerForSharedAnchorDemo.Instance.transform.LookAt(Camera.main.transform);
                    XRUXPickerForSharedAnchorDemo.Instance.transform.Rotate(Vector3.up, 180);
                    XRUXPickerForSharedAnchorDemo.Instance.GetDemoButtons()[0].gameObject.SetActive(true);
                    #else
                    XRUXPickerForSharedAnchorDemo.Instance.GetDemoButtons()[0].transform.Find("Text").GetComponent<Text>().text = "Create & Share Anchor";
                    #endif
                    XRUXPickerForSharedAnchorDemo.Instance.GetDemoInputField().gameObject.SetActive(false);
                    break;
                case AppState.DemoStepInputAnchorNumber:
                    XRUXPickerForSharedAnchorDemo.Instance.GetDemoButtons()[1].gameObject.SetActive(true);
                    XRUXPickerForSharedAnchorDemo.Instance.GetDemoButtons()[0].gameObject.SetActive(false);
                    XRUXPickerForSharedAnchorDemo.Instance.GetDemoInputField().gameObject.SetActive(true);
                    break;
                default:
                    XRUXPickerForSharedAnchorDemo.Instance.GetDemoButtons()[1].gameObject.SetActive(false);
                    #if UNITY_WSA
                    XRUXPickerForSharedAnchorDemo.Instance.GetDemoButtons()[0].gameObject.SetActive(false);
                    #else
                    XRUXPickerForSharedAnchorDemo.Instance.GetDemoButtons()[0].gameObject.SetActive(true);
                    XRUXPickerForSharedAnchorDemo.Instance.GetDemoButtons()[0].transform.Find("Text").GetComponent<Text>().text = "Next Step";
                    #endif
                    XRUXPickerForSharedAnchorDemo.Instance.GetDemoInputField().gameObject.SetActive(false);
                    break;
            }
        }

        private void ConfigureSession()
        {
            List<string> anchorsToFind = new List<string>();

            if (currentAppState == AppState.DemoStepCreateSessionForQuery)
            {
                anchorsToFind.Add(_anchorKeyToFind);
            }
            {
                anchorsExpected = anchorsToFind.Count;
                SetAnchorIdsToLocate(anchorsToFind);
            }
        }

        protected override void CleanupSpawnedObjects()
        {
            base.CleanupSpawnedObjects();

            for (int index = 0; index < otherSpawnedObjects.Count; index++)
            {
                Destroy(otherSpawnedObjects[index]);
            }

            otherSpawnedObjects.Clear();
        }

        /// <summary>
        /// Gets or sets the base URL for the example sharing service.
        /// </summary>
        public string BaseSharingUrl { get => baseSharingUrl; set => baseSharingUrl = value; }
    }
}
