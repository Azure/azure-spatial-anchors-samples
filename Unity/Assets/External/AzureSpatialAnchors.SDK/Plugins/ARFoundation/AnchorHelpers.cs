// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#if UNITY_ANDROID || UNITY_IOS
using UnityEngine.XR.ARFoundation;
using System;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Microsoft.Azure.SpatialAnchors.Unity.ARFoundation
{
    internal static class AnchorHelpers
    {
        /// <summary>
        /// Creates a world anchor in the form of an ARReference point from the specified <see cref="Transform"/>.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <returns>An ARFoundation <see cref="ARReferencePoint"/>.</returns>
        public static ARReferencePoint CreateWorldAnchor(Transform transform)
        {
            return CreateReferencePoint(transform.position, transform.rotation);
        }

        /// <summary>
        /// Creates an ARReferencePoint from the specified <see cref="Transform"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="rotation">The rotation.</param>
        /// <returns>An ARFoundation <see cref="ARReferencePoint"/>.</returns>
        /// <exception cref="InvalidOperationException">Unable to create an anchor.</exception>
        public static ARReferencePoint CreateReferencePoint(Vector3 position, Quaternion rotation)
        {
            Pose anchorPose = new Pose(position, rotation);
            ARReferencePoint referencePoint = SpatialAnchorManager.arReferencePointManager.AddReferencePoint(anchorPose);
            
            if (referencePoint == null)
            {
                Debug.LogError("Unable to create an anchor.");
                throw new InvalidOperationException("Unable to create an anchor.");
            }

            return referencePoint;
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
        /// Gets a <see cref="Pose"/> from the specified <see cref="ARReferencePoint"/>.
        /// </summary>
        /// <param name="referencePoint">The anchor.</param>
        /// <returns><see cref="Pose"/>.</returns>
        public static Pose GetPose(ARReferencePoint referencePoint)
        {
            return new Pose(referencePoint.transform.position, referencePoint.transform.rotation);
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

            ARReferencePoint referencePoint = SpatialAnchorManager.ReferencePointFromPointer(anchorPointer);

            if (referencePoint == null)
            {
                Debug.Log("Didn't find the anchor");
                return Pose.identity;
            }

            return new Pose(referencePoint.transform.position, referencePoint.transform.rotation);
        }
    }
}

#endif
