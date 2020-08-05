// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#if UNITY_ANDROID || UNITY_IOS
using UnityEngine.XR.ARFoundation;
using System;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

#if !UNITY_2019_3_OR_NEWER
// Adapt AR Foundation 3 types to AR Foundation 2 types Unity 2019.2 and earlier.
using ARAnchor = UnityEngine.XR.ARFoundation.ARReferencePoint;
#endif

namespace Microsoft.Azure.SpatialAnchors.Unity.ARFoundation
{
    internal static class AnchorHelpers
    {
        /// <summary>
        /// Creates a world anchor in the form of an <see cref="ARAnchor"/> from the specified <see cref="Transform"/>.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <returns>An AR Foundation <see cref="ARAnchor"/>.</returns>
        public static ARAnchor CreateWorldAnchor(Transform transform)
        {
            return CreateAnchor(transform.position, transform.rotation);
        }

        /// <summary>
        /// Creates an <see cref="ARAnchor"/> from the specified <see cref="Transform"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="rotation">The rotation.</param>
        /// <returns>An AR Foundation <see cref="ARAnchor"/>.</returns>
        /// <exception cref="InvalidOperationException">Unable to create an anchor.</exception>
        public static ARAnchor CreateAnchor(Vector3 position, Quaternion rotation)
        {
            Pose anchorPose = new Pose(position, rotation);

#if UNITY_2019_3_OR_NEWER
            ARAnchor anchor = SpatialAnchorManager.arAnchorManager.AddAnchor(anchorPose);
#else
            ARAnchor anchor = SpatialAnchorManager.arAnchorManager.AddReferencePoint(anchorPose);
#endif

            if (anchor == null)
            {
                Debug.LogError("Unable to create an anchor.");
                throw new InvalidOperationException("Unable to create an anchor.");
            }

            return anchor;
        }

        /// <summary>
        /// Gets an anchor <see cref="Pose"/> from the specified <see cref="CloudSpatialAnchor"/>.
        /// </summary>
        /// <param name="anchor">The anchor.</param>
        /// <returns><see cref="Pose"/>.</returns>
        public static Pose GetPose(CloudSpatialAnchor anchor)
        {
            if (anchor.LocalAnchor == IntPtr.Zero)
            {
                throw new InvalidOperationException("CloudSpatialAnchor did not have a valid local anchor.");
            }

            return GetPose(anchor.LocalAnchor);
        }

        /// <summary>
        /// Gets a <see cref="Pose"/> from the specified <see cref="ARAnchor"/>.
        /// </summary>
        /// <param name="anchor">The anchor.</param>
        /// <returns><see cref="Pose"/>.</returns>
        public static Pose GetPose(ARAnchor anchor)
        {
            return new Pose(anchor.transform.position, anchor.transform.rotation);
        }

        /// <summary>
        /// Gets an anchor <see cref="Pose"/> from the specified native anchor <see cref="IntPtr"/>.
        /// </summary>
        /// <param name="anchorPointer">The anchor pointer.</param>
        /// <returns><see cref="Pose"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Invalid anchor pointer. Can't get the pose.</exception>
        private static Pose GetPose(IntPtr anchorPointer)
        {
            if (anchorPointer == IntPtr.Zero)
            {
                throw new InvalidOperationException("Invalid anchor pointer. Can't get the pose.");
            }

            ARAnchor anchor = SpatialAnchorManager.AnchorFromPointer(anchorPointer);

            if (anchor == null)
            {
                Debug.Log("Didn't find the anchor");
                return Pose.identity;
            }

            return new Pose(anchor.transform.position, anchor.transform.rotation);
        }
    }
}

#endif
