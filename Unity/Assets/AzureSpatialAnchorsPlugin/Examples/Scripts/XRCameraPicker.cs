// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity.Samples
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
        /// The parent of the Arkit (iOS) game objects.
        /// </summary>
        public GameObject ArkitCameraTree;
        /// <summary>
        /// The parent of the ArCore (Android) game objects.
        /// </summary>
        public GameObject ArCoreCameraTree;
        /// <summary>
        /// The parent for the game objects to use in the editor.
        /// </summary>
        public GameObject EditorCameraTree;

        void Awake()
        {
            GameObject targetCamera = EditorCameraTree;
#if UNITY_WSA
            targetCamera = HoloLensCameraTree;
#elif UNITY_IOS
           targetCamera = ArkitCameraTree;
#elif UNITY_ANDROID
            targetCamera = ArCoreCameraTree;
#elif !UNITY_EDITOR
             Debug.LogError("Unexpected platform for XRCameraPicker. Did you intend to include this script in your scene?");     
#endif
            Instantiate(targetCamera);
        }
    }
}
