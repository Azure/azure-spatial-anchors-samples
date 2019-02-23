// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity.Samples
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
                return this._currentAppState;
            }
            set
            {
                if (this._currentAppState != value)
                {
                    Debug.LogFormat("State from {0} to {1}", this._currentAppState, value);
                    this._currentAppState = value;

                }
            }
        }

        readonly List<string> anchorIds = new List<string>();
        readonly Dictionary<AppState, Dictionary<string, GameObject>> spawnedObjectsPerAppState = new Dictionary<AppState, Dictionary<string, GameObject>>();

        Dictionary<string, GameObject> spawnedObjectsInCurrentAppState
        {
            get
            {
                if (this.spawnedObjectsPerAppState.ContainsKey(this._currentAppState) == false)
                {
                    this.spawnedObjectsPerAppState.Add(this._currentAppState, new Dictionary<string, GameObject>());
                }

                return this.spawnedObjectsPerAppState[this._currentAppState];
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

            this.feedbackBox.text = "Find nearby demo.  First, we need to place a few anchors. Tap somewhere to place the first one";

            Debug.Log("Azure Spatial Anchors Demo script started");
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        public override void Update()
        {
            base.Update();

            this.HandleCurrentAppState();
        }

        private void HandleCurrentAppState()
        {
            int timeLeft = (int)(this.dueDate - DateTime.Now).TotalSeconds;
            switch (this.currentAppState)
            {
                case AppState.ReadyToGraph:
                    this.feedbackBox.text = "Next: Tap to start a query for all anchors we just made.";
                    break;
                case AppState.Graphing:
                    this.feedbackBox.text = $"Making sure we can find the anchors we just made. ({this.locatedCount}/{this.numToMake})";
                    break;
                case AppState.ReadyToSearch:
                    this.feedbackBox.text = "Next: Tap to start looking for just the first anchor we placed.";
                    break;
                case AppState.Searching:
                    this.feedbackBox.text = $"Looking for the first anchor you made. Give up in {timeLeft}";
                    if (timeLeft < 0)
                    {
                        Debug.Log("Out of time");
                        // Restart the demo..
                        this.feedbackBox.text = "Failed to find the first anchor.  Try again.";
                        this.currentAppState = AppState.Done;
                    }
                    break;
                case AppState.ReadyToNeighborQuery:
                    this.feedbackBox.text = "Next: Tap to start looking for anchors nearby the first anchor we placed.";
                    break;
                case AppState.Neighboring:
                    this.feedbackBox.text = $"Looking for anchors nearby the first anchor. {this.locatedCount}/{this.numToMake} {timeLeft}";
                    if (timeLeft < 0)
                    {
                        this.feedbackBox.text = "Failed to find all the neighbors.  Try again.";
                        this.currentAppState = AppState.Done;
                    }
                    if (this.locatedCount == this.numToMake)
                    {
                        this.feedbackBox.text = "Found them all!";
                        this.currentAppState = AppState.Done;
                    }
                    break;
            }
        }

        protected override bool IsPlacingObject()
        {
            return this.currentAppState == AppState.Placing;
        }

        protected override Color GetStepColor()
        {
            return this.colors[(int)this.currentAppState];
        }

        private int locatedCount = 0;

        protected override void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
        {
            base.OnCloudAnchorLocated(args);

            this.QueueOnUpdate(() =>
            {
                this.locatedCount++;
                this.currentCloudAnchor = args.Anchor;
                Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
            anchorPose = this.currentCloudAnchor.GetAnchorPose();
#endif
            // HoloLens: The position will be set based on the unityARUserAnchor that was located.

            this.SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

                this.spawnedObject.transform.localScale += this.scaleMods[(int)this.currentAppState];
                this.spawnedObject = null;

                if (this.currentAppState == AppState.Graphing)
                {
                    if (this.spawnedObjectsInCurrentAppState.Count == this.anchorIds.Count)
                    {
                        this.currentAppState = AppState.ReadyToSearch;
                    }
                }
                else if (this.currentAppState == AppState.Searching)
                {
                    this.currentAppState = AppState.ReadyToNeighborQuery;
                }
            });
        }

        private DateTime dueDate = DateTime.Now;
        private readonly List<GameObject> allSpawnedObjects = new List<GameObject>();
        private readonly List<Material> allSpawnedMaterials = new List<Material>();

        protected override void SpawnOrMoveCurrentAnchoredObject(Vector3 worldPos, Quaternion worldRot)
        {
            if (this.currentCloudAnchor != null && this.spawnedObjectsInCurrentAppState.ContainsKey(this.currentCloudAnchor.Identifier))
            {
                this.spawnedObject = this.spawnedObjectsInCurrentAppState[this.currentCloudAnchor.Identifier];
            }

            bool spawnedNewObject = this.spawnedObject == null;

            base.SpawnOrMoveCurrentAnchoredObject(worldPos, worldRot);

            if (spawnedNewObject)
            {
                this.allSpawnedObjects.Add(this.spawnedObject);
                this.allSpawnedMaterials.Add(this.spawnedObjectMat);

                if (this.currentCloudAnchor != null && this.spawnedObjectsInCurrentAppState.ContainsKey(this.currentCloudAnchor.Identifier) == false)
                {
                    this.spawnedObjectsInCurrentAppState.Add(this.currentCloudAnchor.Identifier, this.spawnedObject);
                }
            }

#if WINDOWS_UWP || UNITY_WSA
            if (this.currentCloudAnchor != null
                && this.spawnedObjectsInCurrentAppState.ContainsKey(this.currentCloudAnchor.Identifier) == false)
            {
                this.spawnedObjectsInCurrentAppState.Add(this.currentCloudAnchor.Identifier, this.spawnedObject);
            }
#endif
        }

        public override void AdvanceDemo()
        {
            this.QueueOnUpdate(new Action(() =>
            {
                switch (this.currentAppState)
                {
                    case AppState.Placing:
                        if (this.spawnedObject != null)
                        {
                            this.currentAppState = AppState.Saving;
                            this.CloudManager.EnableProcessing = true;
                            this.SaveCurrentObjectAnchorToCloud();
                        }
                        break;
                    case AppState.ReadyToGraph:
                        this.DoGraphingPass();
                        break;
                    case AppState.ReadyToSearch:
                        this.DoSearchingPass();
                        break;
                    case AppState.ReadyToNeighborQuery:
                        this.DoNeighboringPass();
                        break;
                    case AppState.Done:
                        this.CloudManager.ResetSession(() =>
                        {
                            this.CleanupObjectsBetweenPasses();
                            this.currentAppState = AppState.Placing;
                            this.feedbackBox.text = $"Place an object. {this.allSpawnedObjects.Count}/{this.numToMake} ";
                        });
                        break;
                }
            }));
        }

        protected override void OnSaveCloudAnchorSuccessful()
        {
            base.OnSaveCloudAnchorSuccessful();

            Debug.Log("Anchor created, yay!");

            this.anchorIds.Add(this.currentCloudAnchor.Identifier);

            // Sanity check that the object is still where we expect
            this.QueueOnUpdate(new Action(() =>
            {
                Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
            anchorPose = this.currentCloudAnchor.GetAnchorPose();
#endif
            // HoloLens: The position will be set based on the unityARUserAnchor that was located.

            this.SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

                this.spawnedObject = null;
                this.currentCloudAnchor = null;
                if (this.allSpawnedObjects.Count < this.numToMake)
                {
                    this.feedbackBox.text = $"Saved...Make another {this.allSpawnedObjects.Count}/{this.numToMake} ";
                    this.currentAppState = AppState.Placing;
                    this.CloudManager.EnableProcessing = false;
                    this.CloudManager.ResetSessionStatusIndicators();
                }
                else
                {
                    this.feedbackBox.text = "Saved... ready to start finding them.";
                    this.CloudManager.EnableProcessing = false;
                    this.CloudManager.ResetSessionStatusIndicators();
                    this.currentAppState = AppState.ReadyToGraph;
                }
            }));
        }

        protected override void OnSaveCloudAnchorFailed(Exception exception)
        {
            base.OnSaveCloudAnchorFailed(exception);
        }

        private void DoGraphingPass()
        {
            this.CloudManager.SetGraphEnabled(false);
            this.CloudManager.ResetSession(() =>
            {
                this.locatedCount = 0;
                this.CloudManager.SetAnchorIdsToLocate(this.anchorIds);
                this.CloudManager.SetNearbyAnchor(null, 1000, this.numToMake);
                this.CloudManager.EnableProcessing = true;
                this.CloudManager.CreateWatcher();
                this.currentAppState = AppState.Graphing; //do the recall..
        });
        }

        private void DoSearchingPass()
        {
            this.CloudManager.ResetSession(() =>
            {
                this.CloudManager.ResetSessionStatusIndicators();
                this.CloudManager.EnableProcessing = true;
                this.CloudManager.SetGraphEnabled(false);
                IEnumerable<string> anchorsToFind = new[] { this.anchorIds[0] };
                this.CloudManager.SetAnchorIdsToLocate(anchorsToFind);
                this.locatedCount = 0;
                this.dueDate = DateTime.Now.AddSeconds(30);
                this.CloudManager.CreateWatcher();
                this.currentAppState = AppState.Searching;
            });
        }

        private void DoNeighboringPass()
        {
            this.CloudManager.ResetSession(() =>
            {
                this.CloudManager.ResetSessionStatusIndicators();
                this.CloudManager.EnableProcessing = true;
                this.CloudManager.SetGraphEnabled(true);
                this.CloudManager.ResetAnchorIdsToLocate();
                this.CloudManager.SetNearbyAnchor(this.currentCloudAnchor, 1000, this.numToMake);
                this.locatedCount = 0;
                this.dueDate = DateTime.Now.AddSeconds(30);
                this.CloudManager.CreateWatcher();
                this.currentAppState = AppState.Neighboring;
            });
        }

        private void CleanupObjectsBetweenPasses()
        {
            foreach (GameObject go in this.allSpawnedObjects)
            {
                Destroy(go);
            }
            this.allSpawnedObjects.Clear();

            foreach (Material m in this.allSpawnedMaterials)
            {
                Destroy(m);
            }
            this.allSpawnedMaterials.Clear();

            this.currentCloudAnchor = null;
            this.spawnedObject = null;
            this.spawnedObjectMat = null;
            this.spawnedObjectsPerAppState.Clear();
            this.anchorIds.Clear();
        }
    }
}
