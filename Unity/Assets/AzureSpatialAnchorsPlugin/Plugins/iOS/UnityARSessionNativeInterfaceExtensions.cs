// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#if UNITY_IOS

using System;
using UnityEngine.XR.iOS;

namespace Microsoft.Azure.SpatialAnchors.Unity.IOS.ARKit
{
    public static class UnityARSessionNativeInterfaceExtensions
    {
        /// <summary>
        /// Gets the anchor pointer from the anchor identifier.
        /// </summary>
        /// <param name="arkitSessionNativeInterface">The ARKit session native interface.</param>
        /// <param name="anchorId">The anchor identifier.</param>
        /// <returns><see cref="IntPtr"/>.</returns>
        public static IntPtr GetAnchorPointerFromId(this UnityARSessionNativeInterface arkitSessionNativeInterface, string anchorId)
        {
            return ARKitNativeHelpers.GetAnchorPointerFromId(arkitSessionNativeInterface.GetNativeSessionPtr(), anchorId);
        }
    }
}

#endif
