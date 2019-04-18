// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#if UNITY_ANDROID
using GoogleARCore;
using System;
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity.Android.ARCore
{
    internal static class AnchorHelpers
    {
        /// <summary>
        /// Creates a world anchor from the specified <see cref="Transform"/>.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <returns>An ARCore <see cref="Anchor"/>.</returns>
        public static Anchor CreateWorldAnchor(Transform transform)
        {
            return CreateWorldAnchor(transform.position, transform.rotation);
        }

        /// <summary>
        /// Creates a world anchor from the specified <see cref="Transform"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="rotation">The rotation.</param>
        /// <returns>An ARCore <see cref="Anchor"/>.</returns>
        /// <exception cref="InvalidOperationException">Unable to create an anchor.</exception>
        public static Anchor CreateWorldAnchor(Vector3 position, Quaternion rotation)
        {
            Pose anchorPose = new Pose(position, rotation);
            Anchor anchor = Session.CreateAnchor(anchorPose);

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
        public static Pose GetAnchorPose(CloudSpatialAnchor anchor)
        {
            if (anchor.LocalAnchor == IntPtr.Zero)
            {
                throw new InvalidOperationException("CloudSpatialAnchor did not have a valid local anchor.");
            }

            return GetAnchorPose(anchor.LocalAnchor);
        }

        /// <summary>
        /// Gets an anchor <see cref="Pose"/> from the specified <see cref="Anchor"/>.
        /// </summary>
        /// <param name="anchor">The anchor.</param>
        /// <returns><see cref="Pose"/>.</returns>
        public static Pose GetAnchorPose(Anchor anchor)
        {
            return GetAnchorPose(anchor.m_NativeHandle);
        }

        /// <summary>
        /// Gets an anchor <see cref="Pose"/> from the specified native anchor <see cref="IntPtr"/>.
        /// </summary>
        /// <param name="anchorPointer">The anchor pointer.</param>
        /// <returns><see cref="Pose"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Invalid anchor pointer. Can't get the pose.</exception>
        private static Pose GetAnchorPose(IntPtr anchorPointer)
        {
            if (anchorPointer == IntPtr.Zero)
            {
                throw new InvalidOperationException("Invalid anchor pointer. Can't get the pose.");
            }

            return GoogleARCoreInternal.LifecycleManager.Instance.NativeSession.AnchorApi.GetPose(anchorPointer);
        }
    }
}

#endif
