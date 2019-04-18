// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//#if UNITY_WSA && !UNITY_EDITOR
//using Microsoft.IdentityModel.Clients.ActiveDirectory;
//#endif

//namespace Microsoft.Azure.SpatialAnchors.Unity.Samples
//{
//    public class AuthenticationHelper
//    {
//        /// <summary>
//        /// Set this string to the App client ID.
//        /// </summary>
//        public static string ClientId = "";

//        /// <summary>
//        /// Set this string to the App tenant ID.
//        /// </summary>
//        public static string TenantId = "";

//#if UNITY_WSA && !UNITY_EDITOR
//        public static async Task<string> GetAuthenticationTokenAsync()
//        {
//            var authority = $"https://login.microsoftonline.com/{TenantId}";

//            AuthenticationContext authenticationContext = new AuthenticationContext(authority);
//            AuthenticationResult authenticationResult = await authenticationContext.AcquireTokenAsync("https://sts.mixedreality.azure.com", ClientId, new Uri("urn:ietf:wg:oauth:2.0:oob"), new PlatformParameters(PromptBehavior.Auto, useCorporateNetwork: false));

//            return authenticationResult.AccessToken;
//        }
//#endif
//    }
//}
