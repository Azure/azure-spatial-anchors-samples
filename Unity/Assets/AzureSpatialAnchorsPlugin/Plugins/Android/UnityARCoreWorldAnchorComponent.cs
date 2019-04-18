// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#if UNITY_ANDROID

using GoogleARCore;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity.Android.ARCore
{
    public class UnityARCoreWorldAnchorComponent : MonoBehaviour
    {
        /// <summary>
        /// Gets the world anchor.
        /// </summary>
        public Anchor WorldAnchor { get; private set; }

        /// <summary>
        /// Gets the world anchor handle.
        /// </summary>
        public IntPtr WorldAnchorHandle => this.WorldAnchor.m_NativeHandle;

        /// <summary>
        /// Gets the world anchor identifier.
        /// </summary>
        public string WorldAnchorIdentifier => Marshal.PtrToStringAuto(this.WorldAnchorHandle);

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            this.WorldAnchor = AnchorHelpers.CreateWorldAnchor(this.gameObject.transform);
        }

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any
        /// of the Update methods are called the first time.
        /// </summary>
        private void Start()
        {
        }

        /// <summary>
        /// Destroying the attached Behaviour will result in the game or Scene
        /// receiving OnDestroy.
        /// </summary>
        private void OnDestroy()
        {
            Destroy(this.WorldAnchor);
        }
    }
}

#endif
