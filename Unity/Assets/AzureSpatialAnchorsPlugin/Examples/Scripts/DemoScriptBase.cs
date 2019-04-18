// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Microsoft.Azure.SpatialAnchors.Unity.Samples
{
    public abstract class DemoScriptBase : InputInteractionBase
    {
        public GameObject AnchoredObjectPrefab = null;

        protected bool isErrorActive = false;

        protected Text feedbackBox;

        protected AzureSpatialAnchorsDemoWrapper CloudManager { get; private set; }

        protected CloudSpatialAnchor currentCloudAnchor;

        protected CloudSpatialAnchorWatcher currentWatcher;

        protected GameObject spawnedObject = null;

        protected Material spawnedObjectMat = null;

        private readonly Queue<Action> dispatchQueue = new Queue<Action>();

        /// <summary>
        /// Destroying the attached Behaviour will result in the game or Scene
        /// receiving OnDestroy.
        /// </summary>
        /// <remarks>OnDestroy will only be called on game objects that have previously been active.</remarks>
        public override void OnDestroy()
        {
            if (CloudManager != null)
            {
                CloudManager.EnableProcessing = false;
            }

            if (currentWatcher != null)
            {
                currentWatcher.Stop();
                currentWatcher = null;
            }

            CleanupSpawnedObjects();
        }

        public virtual bool SanityCheckAccessConfiguration()
        {
            if (string.IsNullOrWhiteSpace(CloudManager.SpatialAnchorsAccountId) || string.IsNullOrWhiteSpace(CloudManager.SpatialAnchorsAccountKey))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any
        /// of the Update methods are called the first time.
        /// </summary>
        public override void Start()
        {
            feedbackBox = XRUXPicker.Instance.GetFeedbackText();
            if (feedbackBox == null)
            {
                Debug.Log($"{nameof(feedbackBox)} not found in scene by XRUXPicker.");
                Destroy(this);
                return;
            }

            CloudManager = AzureSpatialAnchorsDemoWrapper.Instance;

            if (CloudManager == null)
            {
                feedbackBox.text = "AzureSpatialAnchorsDemoWrapper doesn't exist in the scene. Make sure it has been added.";
                return;
            }

            if (!SanityCheckAccessConfiguration())
            {
                feedbackBox.text = $"{nameof(AzureSpatialAnchorsDemoWrapper.SpatialAnchorsAccountId)} and {nameof(AzureSpatialAnchorsDemoWrapper.SpatialAnchorsAccountKey)} must be set on AzureSpatialAnchorsDemoConfig in Examples/Resources";
            }
            

            if (AnchoredObjectPrefab == null)
            {
                feedbackBox.text = "CreationTarget must be set on the demo script.";
                return;
            }

            CloudManager.OnSessionUpdated += CloudManager_SessionUpdated;
            CloudManager.OnAnchorLocated += CloudManager_OnAnchorLocated;
            CloudManager.OnLocateAnchorsCompleted += CloudManager_OnLocateAnchorsCompleted;
            CloudManager.OnLogDebug += CloudManager_OnLogDebug;
            CloudManager.OnSessionError += CloudManager_OnSessionError;

            base.Start();
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        public override void Update()
        {
            lock (dispatchQueue)
            {
                if (dispatchQueue.Count > 0)
                {
                    dispatchQueue.Dequeue()();
                }
            }

            base.Update();
        }

        /// <summary>
        /// Advances the demo.
        /// </summary>
        public abstract void AdvanceDemo();

        /// <summary>
        /// returns to the launcher scene.
        /// </summary>
        public void ReturnToLauncher()
        {
            SceneManager.LoadScene(0);
        }

        /// <summary>
        /// Cleans up spawned objects.
        /// </summary>
        protected virtual void CleanupSpawnedObjects()
        {
            if (spawnedObject != null)
            {
                Destroy(spawnedObjectMat);
                Destroy(spawnedObject);
                spawnedObject = null;
                spawnedObjectMat = null;
            }
        }

        /// <summary>
        /// Gets the color of the current demo step.
        /// </summary>
        /// <returns><see cref="Color"/>.</returns>
        protected abstract Color GetStepColor();

        /// <summary>
        /// Determines whether the demo is in a mode that should place an object.
        /// </summary>
        /// <returns><c>true</c> to place; otherwise, <c>false</c>.</returns>
        protected abstract bool IsPlacingObject();

        /// <summary>
        /// Moves the specified anchored object.
        /// </summary>
        /// <param name="objectToMove">The anchored object to move.</param>
        /// <param name="worldPos">The world position.</param>
        /// <param name="worldRot">The world rotation.</param>
        /// <param name="cloudSpatialAnchor">The cloud spatial anchor.</param>
        protected virtual void MoveAnchoredObject(GameObject objectToMove, Vector3 worldPos, Quaternion worldRot, CloudSpatialAnchor cloudSpatialAnchor = null)
        {
#if UNITY_ANDROID || UNITY_IOS
        // On Android and iOS, we expect the position and rotation to be passed in.
        objectToMove.RemoveARAnchor();
        objectToMove.transform.position = worldPos;
        objectToMove.transform.rotation = worldRot;
        objectToMove.AddARAnchor();
#elif WINDOWS_UWP || UNITY_WSA
            // On HoloLens, if we do not have a cloudAnchor already, we will position the
            // object based on the passed in worldPos/worldRot. Then we attach a new world anchor
            // so we are ready to commit the anchor to the cloud if requested.
            // If we do have a cloudAnchor, we will use it's pointer to setup the world anchor
            // This will position the object automatically.
            if (cloudSpatialAnchor == null)
            {
                objectToMove.RemoveARAnchor();
                objectToMove.transform.position = worldPos;
                objectToMove.transform.rotation = worldRot;
                objectToMove.AddARAnchor();
            }
            else
            {
                objectToMove.GetComponent<UnityEngine.XR.WSA.WorldAnchor>().SetNativeSpatialAnchorPtr(cloudSpatialAnchor.LocalAnchor);
            }
#else
        throw new PlatformNotSupportedException();
#endif
        }

        /// <summary>
        /// Called when a cloud anchor is located.
        /// </summary>
        /// <param name="args">The <see cref="AnchorLocatedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
        {
            // To be overridden.
        }

        /// <summary>
        /// Called when cloud anchor location has completed.
        /// </summary>
        /// <param name="args">The <see cref="LocateAnchorsCompletedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnCloudLocateAnchorsCompleted(LocateAnchorsCompletedEventArgs args)
        {
            Debug.Log("Locate pass complete");
        }

        /// <summary>
        /// Called when the current cloud session is updated.
        /// </summary>
        protected virtual void OnCloudSessionUpdated()
        {
            // To be overridden.
        }

        /// <summary>
        /// Called when gaze interaction occurs.
        /// </summary>
        protected override void OnGazeInteraction()
        {
#if WINDOWS_UWP || UNITY_WSA
            // HoloLens gaze interaction
            if (IsPlacingObject())
            {
                base.OnGazeInteraction();
            }
#endif
        }

        /// <summary>
        /// Called when gaze interaction begins.
        /// </summary>
        /// <param name="hitPoint">The hit point.</param>
        /// <param name="target">The target.</param>
        protected override void OnGazeObjectInteraction(Vector3 hitPoint, Vector3 hitNormal)
        {
            base.OnGazeObjectInteraction(hitPoint, hitNormal);

#if WINDOWS_UWP || UNITY_WSA
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hitNormal);
            SpawnOrMoveCurrentAnchoredObject(hitPoint, rotation);
#endif
        }

        /// <summary>
        /// Called when a cloud anchor is not saved successfully.
        /// </summary>
        /// <param name="exception">The exception.</param>
        protected virtual void OnSaveCloudAnchorFailed(Exception exception)
        {
            // we will block the next step to show the exception message in the UI.
            isErrorActive = true;
            Debug.LogException(exception);
            Debug.Log("Failed to save anchor " + exception.ToString());

            QueueOnUpdate(new Action(() => this.feedbackBox.text = string.Format("Error: {0}", exception.ToString())));
        }

        /// <summary>
        /// Called when a cloud anchor is saved successfully.
        /// </summary>
        protected virtual void OnSaveCloudAnchorSuccessful()
        {
            // To be overridden.
        }

        /// <summary>
        /// Called when a select interaction occurs.
        /// </summary>
        /// <remarks>Currently only called for HoloLens.</remarks>
        protected override void OnSelectInteraction()
        {
#if WINDOWS_UWP || UNITY_WSA
            // On HoloLens, we just advance the demo.
            QueueOnUpdate(new Action(() => AdvanceDemo()));
#endif

            base.OnSelectInteraction();
        }

        /// <summary>
        /// Called when a touch object interaction occurs.
        /// </summary>
        /// <param name="hitPoint">The position.</param>
        /// <param name="target">The target.</param>
        protected override void OnSelectObjectInteraction(Vector3 hitPoint, object target)
        {
            if (IsPlacingObject())
            {
                Quaternion rotation = Quaternion.AngleAxis(0, Vector3.up);

                SpawnOrMoveCurrentAnchoredObject(hitPoint, rotation);
            }
        }

        /// <summary>
        /// Called when a touch interaction occurs.
        /// </summary>
        /// <param name="touch">The touch.</param>
        protected override void OnTouchInteraction(Touch touch)
        {
            if (IsPlacingObject())
            {
                base.OnTouchInteraction(touch);
            }
        }

        /// <summary>
        /// Queues the specified <see cref="Action"/> on update.
        /// </summary>
        /// <param name="updateAction">The update action.</param>
        protected void QueueOnUpdate(Action updateAction)
        {
            lock (dispatchQueue)
            {
                dispatchQueue.Enqueue(updateAction);
            }
        }

        /// <summary>
        /// Saves the current object anchor to the cloud.
        /// </summary>
        protected virtual void SaveCurrentObjectAnchorToCloud()
        {
            CloudSpatialAnchor localCloudAnchor = new CloudSpatialAnchor();

            localCloudAnchor.LocalAnchor = spawnedObject.GetNativeAnchorPointer();

            if (localCloudAnchor.LocalAnchor == IntPtr.Zero)
            {
                Debug.Log("Didn't get the local XR anchor pointer...");
                return;
            }

            // In this sample app we delete the cloud anchor explicitly, but here we show how to set an anchor to expire automatically
            localCloudAnchor.Expiration = DateTimeOffset.Now.AddDays(7);

            Task.Run(async () =>
            {
                while (!CloudManager.EnoughDataToCreate)
                {
                    await Task.Delay(330);
                    float createProgress = CloudManager.GetSessionStatusIndicator(AzureSpatialAnchorsDemoWrapper.SessionStatusIndicatorType.RecommendedForCreate);
                    QueueOnUpdate(new Action(() => feedbackBox.text = $"Move your device to capture more environment data: {createProgress:0%}"));
                }

                bool success = false;
                try
                {
                    QueueOnUpdate(new Action(() => feedbackBox.text = "Saving..."));

                    currentCloudAnchor = await CloudManager.StoreAnchorInCloud(localCloudAnchor);
                    success = currentCloudAnchor != null;
                    localCloudAnchor = null;

                    if (success && !isErrorActive)
                    {
                        OnSaveCloudAnchorSuccessful();
                    }
                    else
                    {
                        OnSaveCloudAnchorFailed(new Exception("Failed to save, but no exception was thrown."));
                    }
                }
                catch (Exception ex)
                {
                    OnSaveCloudAnchorFailed(ex);
                }
            });
        }

        /// <summary>
        /// Spawns a new anchored object.
        /// </summary>
        /// <param name="worldPos">The world position.</param>
        /// <param name="worldRot">The world rotation.</param>
        /// <returns><see cref="GameObject"/>.</returns>
        protected virtual GameObject SpawnNewAnchoredObject(Vector3 worldPos, Quaternion worldRot)
        {
            GameObject newGameObject = GameObject.Instantiate(AnchoredObjectPrefab, worldPos, worldRot);
            newGameObject.AddARAnchor();

            newGameObject.GetComponent<MeshRenderer>().material.color = GetStepColor();

            return newGameObject;
        }

        /// <summary>
        /// Spawns a new object.
        /// </summary>
        /// <param name="worldPos">The world position.</param>
        /// <param name="worldRot">The world rotation.</param>
        /// <param name="cloudSpatialAnchor">The cloud spatial anchor.</param>
        /// <returns><see cref="GameObject"/>.</returns>
        protected virtual GameObject SpawnNewAnchoredObject(Vector3 worldPos, Quaternion worldRot, CloudSpatialAnchor cloudSpatialAnchor)
        {
            GameObject newGameObject = SpawnNewAnchoredObject(worldPos, worldRot);

#if WINDOWS_UWP || UNITY_WSA
            // On HoloLens, if we do not have a cloudAnchor already, we will have already positioned the
            // object based on the passed in worldPos/worldRot and attached a new world anchor,
            // so we are ready to commit the anchor to the cloud if requested.
            // If we do have a cloudAnchor, we will use it's pointer to setup the world anchor,
            // which will position the object automatically.
            if (cloudSpatialAnchor != null)
            {
                newGameObject.GetComponent<UnityEngine.XR.WSA.WorldAnchor>().SetNativeSpatialAnchorPtr(cloudSpatialAnchor.LocalAnchor);
            }
#endif

            newGameObject.GetComponent<MeshRenderer>().material.color = GetStepColor();

            return newGameObject;
        }

        /// <summary>
        /// Spawns a new anchored object and makes it the current object or moves the
        /// current anchored object if one exists.
        /// </summary>
        /// <param name="worldPos">The world position.</param>
        /// <param name="worldRot">The world rotation.</param>
        protected virtual void SpawnOrMoveCurrentAnchoredObject(Vector3 worldPos, Quaternion worldRot)
        {
            // Create the object if we need to, and attach the platform appropriate
            // Anchor behavior to the spawned object
            if (spawnedObject == null)
            {
                spawnedObject = SpawnNewAnchoredObject(worldPos, worldRot, currentCloudAnchor);

                spawnedObjectMat = spawnedObject.GetComponent<MeshRenderer>().material;
            }
            else
            {
                MoveAnchoredObject(spawnedObject, worldPos, worldRot, currentCloudAnchor);
            }
        }

        private void CloudManager_OnAnchorLocated(object sender, AnchorLocatedEventArgs args)
        {
            Debug.LogFormat("Anchor recognized as a possible anchor {0} {1}", args.Identifier, args.Status);
            if (args.Status == LocateAnchorStatus.Located)
            {
                OnCloudAnchorLocated(args);
            }
        }

        private void CloudManager_OnLocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs args)
        {
            OnCloudLocateAnchorsCompleted(args);
        }

        private void CloudManager_SessionUpdated(object sender, SessionUpdatedEventArgs args)
        {
            OnCloudSessionUpdated();
        }

        private void CloudManager_OnSessionError(object sender, SessionErrorEventArgs args)
        {
            isErrorActive = true;
            Debug.Log(args.ErrorMessage);

            QueueOnUpdate(new Action(() => this.feedbackBox.text = string.Format("Error: {0}", args.ErrorMessage)));
        }

        private void CloudManager_OnLogDebug(object sender, OnLogDebugEventArgs args)
        {
            Debug.Log(args.Message);
        }

        protected struct DemoStepParams
        {
            public Color StepColor { get; set; }
            public string StepMessage { get; set; }
        }
    }
}
