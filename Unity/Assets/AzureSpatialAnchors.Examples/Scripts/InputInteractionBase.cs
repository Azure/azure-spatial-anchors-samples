// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if WINDOWS_UWP || UNITY_WSA
using UnityEngine.XR.WindowsMR;
#endif

#if UNITY_ANDROID || UNITY_IOS
using UnityEngine.XR.ARFoundation;
#endif

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    public abstract class InputInteractionBase : MonoBehaviour
    {
#if UNITY_ANDROID || UNITY_IOS
        ARRaycastManager arRaycastManager;
#endif
        /// <summary>
        /// Start is called on the frame when a script is enabled just before any
        /// of the Update methods are called the first time.
        /// </summary>
        public virtual void Start()
        {
#if UNITY_ANDROID || UNITY_IOS
            arRaycastManager = FindObjectOfType<ARRaycastManager>();
            if (arRaycastManager == null)
            {
                Debug.Log("Missing ARRaycastManager in scene");
            }
#endif
#if WINDOWS_UWP || UNITY_WSA
            WindowsMRGestures mrGestures = FindObjectOfType<WindowsMRGestures>();
            if (mrGestures != null)
            {
                mrGestures.onTappedChanged += MrGesturesOnTappedChanged;
            }
            else
            {
                throw new InvalidOperationException("WindowsMRGestures not found");
            }
#endif
        }

        /// <summary>
        /// Destroying the attached Behaviour will result in the game or Scene
        /// receiving OnDestroy.
        /// </summary>
        /// <remarks>
        /// OnDestroy will only be called on game objects that have previously been active.
        /// </remarks>
        public virtual void OnDestroy()
        {
#if WINDOWS_UWP || UNITY_WSA
            WindowsMRGestures mrGestures = FindObjectOfType<WindowsMRGestures>();
            if (mrGestures != null)
            {
                mrGestures.onTappedChanged -= MrGesturesOnTappedChanged;
            }
#endif
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        public virtual void Update()
        {
            TriggerInteractions();
        }

        private void TriggerInteractions()
        {
            OnGazeInteraction();

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    return;
                }

                OnTouchInteraction(touch);
            }
        }

        /// <summary>
        /// Called when gaze interaction occurs.
        /// </summary>
        protected virtual void OnGazeInteraction()
        {
            // See if we hit a surface. If not, position the object in front of the user.
            RaycastHit target;
            if (TryGazeHitTest(out target))
            {
                OnGazeObjectInteraction(target.point, target.normal);
            }
            else
            {
                OnGazeObjectInteraction(Camera.main.transform.position + Camera.main.transform.forward * 1.5f, -Camera.main.transform.forward);
            }
        }

        /// <summary>
        /// Called when gaze interaction begins.
        /// </summary>
        /// <param name="hitPoint">The hit point.</param>
        /// <param name="target">The target.</param>
        protected virtual void OnGazeObjectInteraction(Vector3 hitPoint, Vector3 hitNormal)
        {
            // To be overridden.
        }

        /// <summary>
        /// Called when a touch interaction occurs.
        /// </summary>
        /// <param name="touch">The touch.</param>
        protected virtual void OnTouchInteraction(Touch touch)
        {
            if (touch.phase == TouchPhase.Ended)
            {
                OnTouchInteractionEnded(touch);
            }
        }

        /// <summary>
        /// Called when a touch interaction has ended.
        /// </summary>
        /// <param name="touch">The touch.</param>
        protected virtual void OnTouchInteractionEnded(Touch touch)
        {
#if UNITY_ANDROID || UNITY_IOS
            List<ARRaycastHit> aRRaycastHits = new List<ARRaycastHit>();
            if(arRaycastManager.Raycast(touch.position, aRRaycastHits) && aRRaycastHits.Count > 0)
            {
                ARRaycastHit hit = aRRaycastHits[0];
                
                OnSelectObjectInteraction(hit.pose.position, hit);
            }
#elif WINDOWS_UWP || UNITY_WSA
            RaycastHit hit;
            if (TryGazeHitTest(out hit))
            {
                OnSelectObjectInteraction(hit.point, hit);
            }
#endif
        }

        /// <summary>
        /// Called when a select interaction occurs.
        /// </summary>
        /// <remarks>Currently only called for HoloLens.</remarks>
        protected virtual void OnSelectInteraction()
        {
#if WINDOWS_UWP || UNITY_WSA
            RaycastHit hit;
            if (TryGazeHitTest(out hit))
            {
                OnSelectObjectInteraction(hit.point, hit);
            }
#endif
        }

#if WINDOWS_UWP || UNITY_WSA
        /// <summary>
        /// Called when a tap interaction occurs.
        /// </summary>
        /// <remarks>Currently only called for HoloLens.</remarks>
        private void MrGesturesOnTappedChanged(WindowsMRTappedGestureEvent obj)
        {
            OnSelectInteraction();
        }
#endif

        /// <summary>
        /// Called when a touch object interaction occurs.
        /// </summary>
        /// <param name="hitPoint">The position.</param>
        /// <param name="target">The target.</param>
        protected virtual void OnSelectObjectInteraction(Vector3 hitPoint, object target)
        {
            // To be overridden.
        }

        private bool TryGazeHitTest(out RaycastHit target)
        {
            Camera mainCamera = Camera.main;

            // Only detect collisions on the spatial mapping layer. Prevents cube placement issues
            // related to collisions with the UI that follows the user gaze.
            const float maxDetectionDistance = 15.0f;
            int layerMask = LayerMask.GetMask("Surfaces");
            return Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out target, maxDetectionDistance, layerMask);
        }
    }
}
