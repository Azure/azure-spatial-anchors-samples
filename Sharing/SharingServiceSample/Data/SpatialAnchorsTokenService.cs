// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SharingService.Data
{
    public class SpatialAnchorsTokenService
    {
        // Azure Spatial Anchors configuration
        // TODO: Update with your own values
        public string spatialAnchorsAccountId = Environment.GetEnvironmentVariable("SPATIAL_ANCHORS_ACCOUNT_ID"); // Account ID from Azure Spatial Anchors account
        public string spatialAnchorsResource = Environment.GetEnvironmentVariable("SPATIAL_ANCHORS_RESOURCE");

        // AAD configuration
        // TODO: Update with your own values
        public string aadClientId = Environment.GetEnvironmentVariable("AAD_CLIENT_ID"); // Application ID from AAD registration
        public string aadClientKey = Environment.GetEnvironmentVariable("AAD_CLIENT_KEY"); // Application key from AAD registration
        public string aadTenantId = Environment.GetEnvironmentVariable("AAD_TENANT_ID"); //  Specify the Azure tenant ID in which the application was registered

        private readonly HttpClient httpClient;

        public SpatialAnchorsTokenService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<string> RequestToken()
        {
            // Get the AAD app token
            var authority = $"https://login.microsoftonline.com/{aadTenantId}";
            var clientCredential = new ClientCredential(aadClientId, aadClientKey);
            AuthenticationContext authenticationContext = new AuthenticationContext(authority);
            AuthenticationResult authenticationResult = await authenticationContext.AcquireTokenAsync(spatialAnchorsResource, clientCredential);
            string aadAppToken = authenticationResult.AccessToken;

            // Use the AAD app token to request a Spatial Anchors token
            using (HttpRequestMessage httpRequest = new HttpRequestMessage())
            {
                Uri.TryCreate($"https://sts.mixedreality.azure.com/Accounts/{spatialAnchorsAccountId}/token", UriKind.Absolute, out Uri uri);
                httpRequest.Method = HttpMethod.Get;
                httpRequest.RequestUri = uri;
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", aadAppToken);

                using (HttpResponseMessage httpResponse = await this.httpClient.SendAsync(httpRequest))
                {
                    var responseContent = httpResponse.Content.ReadAsStringAsync().Result;
                    JObject responseJson = JObject.Parse(responseContent);

                    return responseJson["AccessToken"].ToObject<string>();
                }
            }
        }
    }
}
