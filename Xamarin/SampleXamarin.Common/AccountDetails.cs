// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace SampleXamarin
{
    public static class AccountDetails
    {
        /// <summary>
        /// The Azure Spatial Anchors account identifier.
        /// </summary>
        /// <remarks>
        /// Set this to your account id found in the Azure Portal.
        /// </remarks>
        public const string SpatialAnchorsAccountId = "Set me";

        /// <summary>
        /// The Azure Spatial Anchors account key.
        /// Set this to your account id found in the Azure Portal.
        /// </summary>
        /// <remarks>
        /// Set this to your account key found in the Azure Portal.
        /// </remarks>
        public const string SpatialAnchorsAccountKey = "Set me";

        /// <summary>
        /// The full URL endpoint of the anchor sharing service.
        /// </summary>
        /// <remarks>
        /// Set this to your URL created when publishing your anchor sharing service in the Sharing sample.
        /// It should end in '/api/anchors'.
        /// </remarks>
        public const string AnchorSharingServiceUrl = "Set me";
    }
}
