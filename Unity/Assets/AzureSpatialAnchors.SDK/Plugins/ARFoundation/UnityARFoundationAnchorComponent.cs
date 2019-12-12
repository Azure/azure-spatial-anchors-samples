#if UNITY_ANDROID || UNITY_IOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Microsoft.Azure.SpatialAnchors.Unity.ARFoundation
{
    public class UnityARFoundationAnchorComponent : MonoBehaviour
    {
        /// <summary>
        /// Gets the world anchor.
        /// </summary>
        public ARReferencePoint WorldAnchor { get; private set; }

        /// <summary>
        /// Gets the world anchor handle.
        /// </summary>
        public IntPtr WorldAnchorHandle => this.WorldAnchor.nativePtr.GetPlatformPointer();

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
            this.gameObject.transform.SetParent(WorldAnchor.transform, true);
        }

        /// <summary>
        /// Destroying the attached Behaviour will result in the game or Scene
        /// receiving OnDestroy.
        /// </summary>
        private void OnDestroy()
        {
            if (this.WorldAnchor != null)
            {
                SpatialAnchorManager.arReferencePointManager.RemoveReferencePoint(this.WorldAnchor);
                this.WorldAnchor = null;
            }
        }
    }
}
#endif
