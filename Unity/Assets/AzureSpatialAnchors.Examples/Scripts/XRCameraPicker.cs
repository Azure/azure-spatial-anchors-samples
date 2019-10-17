// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    /// <summary>
    /// XRCameraPicker enables the platform specific GameObject that contains
    /// the camera and accoutrements such as planes and point clouds.
    /// The associated game objects should be disabled when the application starts.
    /// </summary>
    public class XRCameraPicker : MonoBehaviour
    {
        /// <summary>
        /// The parent of the HoloLens game objects.
        /// </summary>
        public GameObject HoloLensCameraTree;
        /// <summary>
        /// The parent of the ARFoundation game objects.
        /// </summary>
        public GameObject ARFoundationCameraTree;
        /// <summary>
        /// The parent for the game objects to use in the editor.
        /// </summary>
        public GameObject EditorCameraTree;

        void Awake()
        {
            GameObject targetCamera = EditorCameraTree;
#if UNITY_WSA
            targetCamera = HoloLensCameraTree;
#elif UNITY_ANDROID || UNITY_IOS
           targetCamera = ARFoundationCameraTree;
#elif !UNITY_EDITOR
             Debug.LogError("Unexpected platform for XRCameraPicker. Did you intend to include this script in your scene?");     
#endif
            Instantiate(targetCamera);
        }
    }
}
