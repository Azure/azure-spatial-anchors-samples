// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#if UNITY_IOS

using System;
using UnityEngine.XR.iOS;

namespace Microsoft.Azure.SpatialAnchors.Unity.IOS.ARKit
{
    public static class UnityARUserAnchorComponentExtensions
    {
        /// <summary>
        /// Gets the ARKit anchor pointer.
        /// </summary>
        /// <param name="anchorComponent">The anchor component.</param>
        /// <returns><see cref="IntPtr"/>.</returns>
        public static IntPtr GetAnchorPointer(this UnityARUserAnchorComponent anchorComponent)
        {
            return UnityARSessionNativeInterface.GetARSessionNativeInterface().GetAnchorPointerFromId(anchorComponent.AnchorId);
        }
    }
}

#endif
