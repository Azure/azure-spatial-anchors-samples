// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    /// <summary>
    /// For HoloLens, keeps the UI in view of the user
    /// </summary>
    public class FollowUserGaze : MonoBehaviour
    {
        void Start()
        {
            transform.localScale = Vector3.one * .25f;
            if (gameObject.GetComponent<Collider>() == null)
            {
                BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(4, 2, 0.5f);
            }
        }

        void Update()
        {
            Camera mainCamera = Camera.main;

            // Do a raycast into the world based on the user's
            // head position and orientation.
            var headPosition = mainCamera.transform.position;
            var gazeDirection = mainCamera.transform.forward;

            RaycastHit hitInfo;

            /// Check to see if we are colliding with anything aside from spatial mapping
            if (!Physics.Raycast(headPosition, gazeDirection, out hitInfo, 15.0f, ~(1 << 20)))
            {
                // If the raycast did not hit the canvas, update canvas position
                Vector3 nextPos = headPosition + gazeDirection * 2;
                transform.position = Vector3.Lerp(transform.position, nextPos, 1f / 60f);
                transform.LookAt(mainCamera.transform);
                transform.Rotate(Vector3.up, 180);
            }
        }
    }
}
