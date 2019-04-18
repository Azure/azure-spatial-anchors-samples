// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#if UNITY_IOS

using System;
using System.Runtime.InteropServices;
using UnityEngine.XR.iOS;

namespace Microsoft.Azure.SpatialAnchors.Unity.IOS.ARKit
{
    public static class ARKitNativeHelpers
    {
        [DllImport("__Internal")]
        private static extern IntPtr SessionGetArAnchorPointerForId(IntPtr nativeSession, [MarshalAs(UnmanagedType.LPStr)] string anchorIdentifier);

        [DllImport("__Internal")]
        private static extern UnityARMatrix4x4 SessionGetUnityTransformFromAnchorPtr(IntPtr nativeAnchor);

        /// <summary>
        /// Gets the anchor pointer from the anchor identifier.
        /// </summary>
        /// <param name="sessionHandle">The ARKit session handler.</param>
        /// <param name="anchorid">The anchor identifier.</param>
        /// <returns><see cref="IntPtr"/>.</returns>
        public static IntPtr GetAnchorPointerFromId(IntPtr sessionHandle, string anchorid)
        {
#if !UNITY_EDITOR && UNITY_IOS
            return SessionGetArAnchorPointerForId(sessionHandle, anchorid);
#else
            return IntPtr.Zero;
#endif
        }

        /// <summary>
        /// Gets the anchor transform.
        /// </summary>
        /// <param name="anchorHandle">The ARKit anchor handle.</param>
        /// <returns><see cref="UnityARMatrix4x4"/>.</returns>
        public static UnityARMatrix4x4 GetAnchorTransform(IntPtr anchorHandle)
        {
#if !UNITY_EDITOR && UNITY_IOS
            return SessionGetUnityTransformFromAnchorPtr(anchorHandle);
#else
            return new UnityARMatrix4x4();
#endif
        }
    }
}

#endif
