// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    public class AzureSpatialAnchorsNearbyDemoScript : DemoScriptBase
    {
        internal enum AppState
        {
            Placing = 0,
            Saving,
            ReadyToGraph,
            Graphing,
            ReadyToSearch,
            Searching,
            ReadyToNeighborQuery,
            Neighboring,
            Done,
            ModeCount
        }

        private readonly Color[] colors =
        {
            Color.white,
            Color.magenta,
            Color.magenta,
            Color.yellow,
            Color.magenta,
            Color.cyan,
            Color.magenta,
            Color.green,
            Color.grey
        };

        private readonly Vector3[] scaleMods =
        {
            new Vector3(0,0,0),
            new Vector3(0,0,0),
            new Vector3(0,0,0),
            new Vector3(.1f,0,0),
            new Vector3(0,0,0),
            new Vector3(0,0,.1f),
            new Vector3(0,0,0),
            new Vector3(0,.1f,0),
            new Vector3(0,0,0)
        };
        private readonly int numToMake = 3;

        private AppState _currentAppState = AppState.Placing;

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

                }
            }
        }

        readonly List<string> anchorIds = new List<string>();
        readonly Dictionary<AppState, Dictionary<string, GameObject>> spawnedObjectsPerAppState = new Dictionary<AppState, Dictionary<string, GameObject>>();

        Dictionary<string, GameObject> spawnedObjectsInCurrentAppState
        {
            get
            {
                if (spawnedObjectsPerAppState.ContainsKey(_currentAppState) == false)
                {
                    spawnedObjectsPerAppState.Add(_currentAppState, new Dictionary<string, GameObject>());
                }

                return spawnedObjectsPerAppState[_currentAppState];
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

            feedbackBox.text = "Find nearby demo.  First, we need to place a few anchors. Tap somewhere to place the first one";

            Debug.Log("Azure Spatial Anchors Demo script started");
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        public override void Update()
        {
            base.Update();

            HandleCurrentAppState();
        }

        private void HandleCurrentAppState()
        {
            int timeLeft = (int)(dueDate - DateTime.Now).TotalSeconds;
            switch (currentAppState)
            {
                case AppState.ReadyToGraph:
                    feedbackBox.text = "Next: Tap to start a query for all anchors we just made.";
                    break;
                case AppState.Graphing:
                    feedbackBox.text = $"Making sure we can find the anchors we just made. ({locatedCount}/{numToMake})";
                    break;
                case AppState.ReadyToSearch:
                    feedbackBox.text = "Next: Tap to start looking for just the first anchor we placed.";
                    break;
                case AppState.Searching:
                    feedbackBox.text = $"Looking for the first anchor you made. Give up in {timeLeft}";
                    if (timeLeft < 0)
                    {
                        Debug.Log("Out of time");
                        // Restart the demo..
                        feedbackBox.text = "Failed to find the first anchor.  Try again.";
                        currentAppState = AppState.Done;
                    }
                    break;
                case AppState.ReadyToNeighborQuery:
                    feedbackBox.text = "Next: Tap to start looking for anchors nearby the first anchor we placed.";
                    break;
                case AppState.Neighboring:
                    // We should find all anchors except for the anchor we are using as the source anchor.
                    feedbackBox.text = $"Looking for anchors nearby the first anchor. {locatedCount}/{numToMake - 1} {timeLeft}";
                    if (timeLeft < 0)
                    {
                        feedbackBox.text = "Failed to find all the neighbors.  Try again.";
                        currentAppState = AppState.Done;
                    }
                    if (locatedCount == numToMake - 1)
                    {
                        feedbackBox.text = "Found them all!";
                        currentAppState = AppState.Done;
                    }
                    break;
            }
        }

        protected override bool IsPlacingObject()
        {
            return currentAppState == AppState.Placing;
        }

        protected override Color GetStepColor()
        {
            return colors[(int)currentAppState];
        }

        private int locatedCount = 0;

        protected override void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
        {
            base.OnCloudAnchorLocated(args);

            if (args.Status == LocateAnchorStatus.Located)
            {
                UnityDispatcher.InvokeOnAppThread(() =>
                {
                    locatedCount++;
                    currentCloudAnchor = args.Anchor;
                    Pose anchorPose = Pose.identity;

                    #if UNITY_ANDROID || UNITY_IOS
                    anchorPose = currentCloudAnchor.GetPose();
                    #endif
                    // HoloLens: The position will be set based on the unityARUserAnchor that was located.

                    SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

                    spawnedObject.transform.localScale += scaleMods[(int)currentAppState];
                    spawnedObject = null;

                    if (currentAppState == AppState.Graphing)
                    {
                        if (spawnedObjectsInCurrentAppState.Count == anchorIds.Count)
                        {
                            currentAppState = AppState.ReadyToSearch;
                        }
                    }
                    else if (currentAppState == AppState.Searching)
                    {
                        currentAppState = AppState.ReadyToNeighborQuery;
                    }
                });
            }
        }

        private DateTime dueDate = DateTime.Now;
        private readonly List<GameObject> allSpawnedObjects = new List<GameObject>();
        private readonly List<Material> allSpawnedMaterials = new List<Material>();

        protected override void SpawnOrMoveCurrentAnchoredObject(Vector3 worldPos, Quaternion worldRot)
        {
            if (currentCloudAnchor != null && spawnedObjectsInCurrentAppState.ContainsKey(currentCloudAnchor.Identifier))
            {
                spawnedObject = spawnedObjectsInCurrentAppState[currentCloudAnchor.Identifier];
            }

            bool spawnedNewObject = spawnedObject == null;

            base.SpawnOrMoveCurrentAnchoredObject(worldPos, worldRot);

            if (spawnedNewObject)
            {
                allSpawnedObjects.Add(spawnedObject);
                allSpawnedMaterials.Add(spawnedObjectMat);

                if (currentCloudAnchor != null && spawnedObjectsInCurrentAppState.ContainsKey(currentCloudAnchor.Identifier) == false)
                {
                    spawnedObjectsInCurrentAppState.Add(currentCloudAnchor.Identifier, spawnedObject);
                }
            }

            #if WINDOWS_UWP || UNITY_WSA
            if (currentCloudAnchor != null
                    && spawnedObjectsInCurrentAppState.ContainsKey(currentCloudAnchor.Identifier) == false)
            {
                spawnedObjectsInCurrentAppState.Add(currentCloudAnchor.Identifier, spawnedObject);
            }
            #endif
        }

        public async override Task AdvanceDemoAsync()
        {
            switch (currentAppState)
            {
                case AppState.Placing:
                    if (spawnedObject != null)
                    {
                        currentAppState = AppState.Saving;
                        if (!CloudManager.IsSessionStarted)
                        {
                            await CloudManager.StartSessionAsync();
                        }
                        await SaveCurrentObjectAnchorToCloudAsync();
                    }
                    break;
                case AppState.ReadyToGraph:
                    await DoGraphingPassAsync();
                    break;
                case AppState.ReadyToSearch:
                    await DoSearchingPassAsync();
                    break;
                case AppState.ReadyToNeighborQuery:
                    await DoNeighboringPassAsync();
                    break;
                case AppState.Done:
                    await CloudManager.ResetSessionAsync();
                    CleanupObjectsBetweenPasses();
                    currentAppState = AppState.Placing;
                    feedbackBox.text = $"Place an object. {allSpawnedObjects.Count}/{numToMake} ";
                    break;
            }
        }

        protected override async Task OnSaveCloudAnchorSuccessfulAsync()
        {
            await base.OnSaveCloudAnchorSuccessfulAsync();

            Debug.Log("Anchor created, yay!");

            anchorIds.Add(currentCloudAnchor.Identifier);

            // Sanity check that the object is still where we expect
            Pose anchorPose = Pose.identity;

            #if UNITY_ANDROID || UNITY_IOS
            anchorPose = currentCloudAnchor.GetPose();
            #endif
            // HoloLens: The position will be set based on the unityARUserAnchor that was located.

            SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

            spawnedObject = null;
            currentCloudAnchor = null;
            if (allSpawnedObjects.Count < numToMake)
            {
                feedbackBox.text = $"Saved...Make another {allSpawnedObjects.Count}/{numToMake} ";
                currentAppState = AppState.Placing;
                CloudManager.StopSession();
            }
            else
            {
                feedbackBox.text = "Saved... ready to start finding them.";
                CloudManager.StopSession();
                currentAppState = AppState.ReadyToGraph;
            }
        }

        protected override void OnSaveCloudAnchorFailed(Exception exception)
        {
            base.OnSaveCloudAnchorFailed(exception);
        }

        private async Task DoGraphingPassAsync()
        {
            SetGraphEnabled(false);
            await CloudManager.ResetSessionAsync();
            locatedCount = 0;
            SetAnchorIdsToLocate(anchorIds);
            SetNearbyAnchor(null, 10, numToMake);
            await CloudManager.StartSessionAsync();
            currentWatcher = CreateWatcher();
            currentAppState = AppState.Graphing; //do the recall..
        }

        private async Task DoSearchingPassAsync()
        {
            await CloudManager.ResetSessionAsync();
            await CloudManager.StartSessionAsync();
            SetGraphEnabled(false);
            IEnumerable<string> anchorsToFind = new[] { anchorIds[0] };
            SetAnchorIdsToLocate(anchorsToFind);
            locatedCount = 0;
            dueDate = DateTime.Now.AddSeconds(30);
            currentWatcher = CreateWatcher();
            currentAppState = AppState.Searching;
        }

        private async Task DoNeighboringPassAsync()
        {
            await CloudManager.StartSessionAsync();
            SetGraphEnabled(true);
            ResetAnchorIdsToLocate();
            SetNearbyAnchor(currentCloudAnchor, 10, numToMake);
            locatedCount = 0; 
            dueDate = DateTime.Now.AddSeconds(30);
            currentWatcher = CreateWatcher();
            currentAppState = AppState.Neighboring;
        }

        private void CleanupObjectsBetweenPasses()
        {
            foreach (GameObject go in allSpawnedObjects)
            {
                Destroy(go);
            }
            allSpawnedObjects.Clear();

            foreach (Material m in allSpawnedMaterials)
            {
                Destroy(m);
            }
            allSpawnedMaterials.Clear();

            currentCloudAnchor = null;
            spawnedObject = null;
            spawnedObjectMat = null;
            spawnedObjectsPerAppState.Clear();
            anchorIds.Clear();
        }
    }
}
