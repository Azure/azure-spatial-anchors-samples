// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#if UNITY_IOS

using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.XR;

namespace Microsoft.Azure.SpatialAnchors.Unity.IOS
{
    public static class ARKitNativeHelpers
    {
        [DllImport("__Internal")]
        private static extern IntPtr GetArkitAnchorId(IntPtr anchorPointer);

        /// <summary>
        /// Gets the anchor id from the anchor pointer.
        /// </summary>
        /// <param name="anchorPointer">The ARKit anchor pointer.</param>
        /// <returns><see cref="string"/>string containing the anchor id</returns>
        public static string GetArkitAnchorIdFromPointer(IntPtr anchorPointer)
        {
#if !UNITY_EDITOR && UNITY_IOS
            return Marshal.PtrToStringAuto(GetArkitAnchorId(anchorPointer));
#else
            return "";
#endif
        }
    }
}
#endif
