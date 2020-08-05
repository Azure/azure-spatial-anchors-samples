// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity
{
    /// <summary>
    /// This menu item generates an optional configuration file which can be
    /// excluded from source control to avoid committing credentials there.
    /// </summary>
    [CreateAssetMenu(fileName = "SpatialAnchorConfig", menuName = "Azure Spatial Anchors/Configuration")]
    public class SpatialAnchorConfig : ScriptableObject
    {
        [Header("Authentication")]
        [SerializeField]
        [Tooltip("The method to use for authentication.")]
        protected AuthenticationMode authenticationMode = AuthenticationMode.ApiKey;
        public AuthenticationMode AuthenticationMode => authenticationMode;

        [Header("Credentials")]
        [SerializeField]
        [Tooltip("The Account ID provided by the Spatial Anchors service portal.")]
        protected string spatialAnchorsAccountId = "";
        public string SpatialAnchorsAccountId => spatialAnchorsAccountId;

        [SerializeField]
        [Tooltip("The Account Key provided by the Spatial Anchors service portal.")]
        protected string spatialAnchorsAccountKey = "";
        public string SpatialAnchorsAccountKey => spatialAnchorsAccountKey;

        [SerializeField]
        [Tooltip("The Account Domain to use when connecting to the Azure Spatial Anchors services.")]
        protected string spatialAnchorsAccountDomain = "";
        public string SpatialAnchorsAccountDomain => spatialAnchorsAccountDomain;

        [Header("Credentials")]
        [SerializeField]
        [Tooltip("The Client ID to use when authenticating using Azure Active Directory.")]
        protected string clientId = "";
        public string ClientId => clientId;

        [SerializeField]
        [Tooltip("The Tenant ID to use when authenticating using Azure Active Directory.")]
        protected string tenantId = "";
        public string TenantId => tenantId;
    }
}
