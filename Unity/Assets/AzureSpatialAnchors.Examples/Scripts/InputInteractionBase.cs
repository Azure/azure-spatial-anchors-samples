// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

#if WINDOWS_UWP || UNITY_WSA
#if MIXED_REALITY_OPENXR
using Microsoft.Azure.SpatialAnchors.Unity.Examples.OpenXR;
#else
using UnityEngine.XR.WindowsMR;
#endif
#endif
using UnityEngine.XR.ARFoundation;


namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    public abstract class InputInteractionBase : MonoBehaviour
    {
#if UNITY_ANDROID || UNITY_IOS
        ARRaycastManager arRaycastManager;
        private InputAction clickAction;
        
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
            clickAction = new InputAction("Click");
            clickAction.AddBinding("<Touchscreen>/touch*/position");
            
            clickAction.Enable();
            clickAction.performed += ClickAction_performed;
#endif
#if WINDOWS_UWP || UNITY_WSA
            WindowsMRGestures mrGestures = FindObjectOfType<WindowsMRGestures>();
            if (mrGestures == null)
            {
                mrGestures = gameObject.AddComponent<WindowsMRGestures>();
            }

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

#if UNITY_ANDROID || UNITY_IOS
        private void ClickAction_performed(InputAction.CallbackContext obj)
        {
            OnTouchInteractionEnded(clickAction.ReadValue<Vector2>());
        }
#endif

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
        protected virtual void OnTouchInteraction(Vector2 touchPosition)
        {
            OnTouchInteractionEnded(touchPosition);
        }

        private bool IsTouchOverUIButton(Vector2 touchPosition)
        {
            PointerEventData ped = new PointerEventData(EventSystem.current);
            ped.position = touchPosition;
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, raycastResults);
            
            foreach(RaycastResult raycastResult in raycastResults)
            {
                if (raycastResult.gameObject.GetComponentInChildren<Button>())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Called when a touch interaction has ended.
        /// </summary>
        /// <param name="touch">The touch.</param>
        protected virtual void OnTouchInteractionEnded(Vector2 touchPosition)
        {
#if UNITY_ANDROID || UNITY_IOS
            // check if the user is tapping a button
            if (IsTouchOverUIButton(touchPosition))
            {
                return;
            }

            List<ARRaycastHit> aRRaycastHits = new List<ARRaycastHit>();
           
            if(arRaycastManager.Raycast(touchPosition, aRRaycastHits) && aRRaycastHits.Count > 0)
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
