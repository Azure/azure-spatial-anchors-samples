// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_ANDROID
using GoogleARCore;
#elif UNITY_IOS
using UnityEngine.XR.iOS;
#endif

public abstract class InputInteractionBase : MonoBehaviour
{
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
        UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourcePressed -= InteractionManager_InteractionSourcePressed;
#endif
    }

    /// <summary>
    /// Start is called on the frame when a script is enabled just before any
    /// of the Update methods are called the first time.
    /// </summary>
    public virtual void Start()
    {
#if WINDOWS_UWP || UNITY_WSA
        UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourcePressed += InteractionManager_InteractionSourcePressed;
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
#if UNITY_IOS
        var screenPosition = Camera.main.ScreenToViewportPoint(touch.position);
        ARPoint point = new ARPoint
        {
            x = screenPosition.x,
            y = screenPosition.y
        };

        var hitResults = UnityARSessionNativeInterface.GetARSessionNativeInterface().HitTest(point,
            ARHitTestResultType.ARHitTestResultTypeEstimatedHorizontalPlane | ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent);
        if (hitResults.Count > 0)
        {
            ARHitTestResult hitResult = hitResults[0];
            Vector3 pos = UnityARMatrixOps.GetPosition(hitResult.worldTransform);
            OnSelectObjectInteraction(pos, hitResult);
        }
#elif UNITY_ANDROID
        TrackableHit hit;
        TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon |
            TrackableHitFlags.FeaturePointWithSurfaceNormal;

        if (Frame.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit))
        {
            OnSelectObjectInteraction(hit.Pose.position, hit);
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

        return Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out target);
    }

#if WINDOWS_UWP || UNITY_WSA
    /// <summary>
    /// Handles the HoloLens interaction event.
    /// </summary>
    /// <param name="obj">The <see cref="UnityEngine.XR.WSA.Input.InteractionSourcePressedEventArgs"/> instance containing the event data.</param>
    private void InteractionManager_InteractionSourcePressed(UnityEngine.XR.WSA.Input.InteractionSourcePressedEventArgs obj)
    {
        if (obj.pressType == UnityEngine.XR.WSA.Input.InteractionSourcePressType.Select)
        {
            OnSelectInteraction();
        }
    }
#endif
}
