// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Azure.SpatialAnchors;
using System;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_ANDROID || UNITY_IOS
using Microsoft.Azure.SpatialAnchors.Unity.ARFoundation;
using NativeAnchor = Microsoft.Azure.SpatialAnchors.Unity.ARFoundation.UnityARFoundationAnchorComponent;
#elif WINDOWS_UWP || UNITY_WSA
using UnityEngine.XR.WSA;
using NativeAnchor = UnityEngine.XR.WSA.WorldAnchor;
#else
using NativeAnchor = UnityEngine.MonoBehaviour;
#endif

namespace Microsoft.Azure.SpatialAnchors.Unity
{
    /// <summary>
    /// A behavior that helps keep platform native anchors in sync with a
    /// <see cref="CloudSpatialAnchor"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For iOS the native anchor is <see cref="UnityARUserAnchorComponent"/>,
    /// for Android it is <see cref="UnityARCoreWorldAnchorComponent"/> and for
    /// Windows Mixed Reality / HoloLens it is <see cref="WorldAnchor"/>.
    /// </para>
    /// <para>
    /// WorldAnchors can be updated when new cloud data is available, but
    /// iOS and Android anchors need to be recreated each time. This behavior
    /// helps manage updates, and provides a convenient way access both cloud
    /// and native versions of the anchors applied to the object.
    /// </para>
    /// <para>
    /// If a developer prefers not to use this behavior, many of the same
    /// capabilities are available as extension methods in
    /// <see cref="SpatialAnchorExtensions"/>.
    /// </para>
    /// </remarks>
    public class CloudNativeAnchor : MonoBehaviour
    {
        #region Member Variables
        private CloudSpatialAnchor cloudAnchor;
        private NativeAnchor nativeAnchor;
        #endregion // Member Variables

        #region Unity Overrides
        protected virtual void Awake()
        {
            // If there's already a native anchor, go ahead and reference it
            nativeAnchor = gameObject.FindNativeAnchor();
        }
        #endregion // Unity Overrides

        #region Public Methods
        /// <summary>
        /// Stores the specified cloud version of the anchor and creates or updates the native anchor
        /// to match.
        /// </summary>
        /// <param name="cloudAnchor">
        /// The cloud version of the anchor.
        /// </param>
        /// <remarks>
        /// When this method completes, <see cref="CloudAnchor"/> will point to the anchor specified
        /// by <paramref name="cloudAnchor"/> and <see cref="NativeAnchor"/> will return a new or updated
        /// native anchor with the same information.
        /// </remarks>
        public void CloudToNative(CloudSpatialAnchor cloudAnchor)
        {
            // Validate
            if (cloudAnchor == null) { throw new ArgumentNullException(nameof(cloudAnchor)); }

            // Apply and store updated native anchor
            nativeAnchor = gameObject.ApplyCloudAnchor(cloudAnchor);
        }

        /// <summary>
        /// Creates or updates the <see cref="CloudSpatialAnchor"/> returned by
        /// <see cref="CloudAnchor"/> to reflect the same data as the native anchor.
        /// </summary>
        /// <param name="useExisting">
        /// <c>true</c> to reuse any existing cloud anchor; <c>false</c> to
        /// always create a new one.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If no native anchor exists on the game object it will be created.
        /// </para>
        /// </remarks>
        public void NativeToCloud(bool useExisting)
        {
            // Make sure there's a native anchor
            if (nativeAnchor == null) { nativeAnchor = gameObject.FindOrCreateNativeAnchor(); }

            // If there is no cloud anchor, create it
            if ((!useExisting) || (cloudAnchor == null))
            {
                cloudAnchor = nativeAnchor.ToCloud();
            }
        }

        /// <summary>
        /// Creates or updates the <see cref="CloudSpatialAnchor"/> returned by
        /// <see cref="CloudAnchor"/> to reflect the same data as the native anchor.
        /// If a cloud anchor already exists it will be reused.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If no native anchor exists on the game object it will be created.
        /// </para>
        /// </remarks>
        public void NativeToCloud()
        {
            NativeToCloud(useExisting: true);
        }

        /// <summary>
        /// Sets the pose of the attached <see cref="GameObject"/>, modifying
        /// native anchors if necessary.
        /// </summary>
        /// <param name="gameObject">
        /// The <see cref="GameObject"/> to set the pose on.
        /// </param>
        /// <param name="position">
        /// The new position to set.
        /// </param>
        /// <param name="rotation">
        /// The new rotation to set.
        /// </param>
        public void SetPose(Vector3 position, Quaternion rotation)
        {
            // Changing the position resets both native and cloud anchors
            cloudAnchor = null;
            nativeAnchor = null;

            // Use extension method to update position and native anchor
            gameObject.SetPose(position, rotation);

            // Get the native anchor back (if there was one)
            nativeAnchor = gameObject.FindNativeAnchor();
        }

        /// <summary>
        /// Sets the pose of the attached <see cref="GameObject"/>, modifying
        /// native anchors if necessary.
        /// </summary>
        /// <param name="gameObject">
        /// The <see cref="GameObject"/> to set the pose on.
        /// </param>
        /// <param name="pose">
        /// The pose to set.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="gameObject"/> is <see langword = "null" />.
        /// </exception>
        public void SetPose(Pose pose)
        {
            SetPose(pose.position, pose.rotation);
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets the cloud version of the anchor.
        /// </summary>
        /// <value>
        /// The cloud version of the anchor.
        /// </value>
        public CloudSpatialAnchor CloudAnchor { get { return cloudAnchor; } }

        /// <summary>
        /// Gets the native version of the anchor.
        /// </summary>
        /// <value>
        /// The native version of the anchor.
        /// </value>
        public NativeAnchor NativeAnchor { get { return nativeAnchor; } }
        #endregion // Public Properties
    }
}