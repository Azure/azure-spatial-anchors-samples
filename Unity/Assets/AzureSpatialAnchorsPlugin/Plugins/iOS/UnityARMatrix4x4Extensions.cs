// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#if UNITY_IOS

using UnityEngine;
using UnityEngine.XR.iOS;

namespace Microsoft.Azure.SpatialAnchors.Unity.IOS.ARKit
{
    public static class UnityARMatrix4x4Extensions
    {
        /// <summary>
        /// Converts a <see cref="UnityARMatrix4x4"/> to a <see cref="Matrix4x4"/>.
        /// </summary>
        /// <param name="input">The input matrix.</param>
        /// <returns><see cref="Matrix4x4"/>.</returns>
        public static Matrix4x4 ToMatrix4x4(this UnityARMatrix4x4 input)
        {
            return UnityARMatrixOps.GetMatrix(input);
        }
    }
}

#endif
