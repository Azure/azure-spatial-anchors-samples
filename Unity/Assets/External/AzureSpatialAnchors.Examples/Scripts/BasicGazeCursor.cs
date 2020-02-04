// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
#if UNITY_WSA
using UnityEngine.XR.WSA.Input;
#endif

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    /// <summary>
    /// On HoloLens we will manage our own input. This script handles positioning the gaze cursor
    /// and invoking select events when UI buttons are pressed.
    /// </summary>
    public class BasicGazeCursor : MonoBehaviour
    {
        // Keep track of the object we are targeting
        private Button targeted = null;

        void Start()
        {
#if !UNITY_WSA
        Destroy(this.gameObject);
        return;
#else
            UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourceReleased += InteractionManager_InteractionSourceReleased;

            // Since this script will be raising UX events on HoloLens, disable the built in input module to prevent
            // events being raised twice.
            StandaloneInputModule sim = FindObjectOfType<StandaloneInputModule>();
            if (sim != null)
            {
                Destroy(sim);
            }
#endif
        }

#if UNITY_WSA
        private void InteractionManager_InteractionSourceReleased(InteractionSourceReleasedEventArgs obj)
        {
            // On HoloLens 2 a grasp event is also raised that we don't want to pass along as a click.
            if (obj.pressType == InteractionSourcePressType.Grasp)
            {
                return;
            }

            if (targeted != null)
            {
                Debug.Log("Clicking >> " + targeted.gameObject.name);
                targeted.onClick.Invoke();
            }
        }
#endif

        void Update()
        {
            // Do a raycast into the world based on the user's
            // head position and orientation.
            Camera mainCamera = Camera.main;
            Vector3 headPosition = mainCamera.transform.position;
            Vector3 gazeDirection = mainCamera.transform.forward;

            RaycastHit hitInfo;
            if (Physics.Raycast(headPosition, gazeDirection, out hitInfo, 150.0f, ~(1 << 20)))
            {
                transform.position = hitInfo.point;
                transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
                // if the gaze is over a button, keep track of that so we can
                // send the clicked event to the button if the user taps.
                targeted = hitInfo.collider.GetComponent<Button>();
            }
            else
            {
                transform.position = headPosition + gazeDirection * 2.0f;
                transform.rotation = Quaternion.FromToRotation(Vector3.up, -gazeDirection);
                targeted = null;
            }
        }
    }
}
