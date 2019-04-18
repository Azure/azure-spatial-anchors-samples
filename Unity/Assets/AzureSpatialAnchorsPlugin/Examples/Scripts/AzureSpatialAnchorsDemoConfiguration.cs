// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity.Samples
{
    [CreateAssetMenu(fileName = "AzureSpatialAnchorsDemoConfig", menuName = "AzureSpatialAnchors/DemoConfiguration")]
    public class AzureSpatialAnchorsDemoConfiguration : ScriptableObject
    {
        /// <summary>
        /// Set this string to the Spatial Anchors account ID provided in the Spatial Anchors resource.
        /// </summary>
        public string SpatialAnchorsAccountId = "";

        /// <summary>
        /// Set this string to the Spatial Anchors account key provided in the Spatial Anchors resource.
        /// </summary>
        public string SpatialAnchorsAccountKey = "";

        /// <summary>
        /// Set this string to the service url created from the sample in Sharing\SharingServiceSample
        /// </summary>
        public string BaseSharingURL = "";
    }
}
