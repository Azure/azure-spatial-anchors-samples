// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

import ADAL
import Foundation

// You can use this helper method to get an authentication token via Azure Active Directory.
// For getting going quickly, you can instead set the spatialAnchorsAccountId and spatialAnchorsAccountKey in BaseViewController.swift
class AuthenticationHelper {
    
    // Set these strings to the Service URL provided for the Azure Spatial Service resource.
    static let AuthServiceBaseUrl = "https://sts.mixedreality.azure.com"
    static let ClientId = "Set me"
    static let TenantId = "Set me"
    
    static func acquireAuthenticationToken(completion: @escaping (_ accessToken: String?, Error?) -> Void) {
        let authContext = ADAuthenticationContext(authority: "https://login.microsoftonline.com/" + TenantId,
                                                  error: nil)
        
        authContext!.acquireToken(withResource: AuthServiceBaseUrl,
                                  clientId: ClientId,
                                  redirectUri: URL(string: "urn:ietf:wg:oauth:2.0:oob")) {
            (result) in
            
            if (result!.status != AD_SUCCEEDED) {
                completion(nil, result!.error)
            }
            else {
                completion(result!.accessToken, nil)
            }
        }
    }
}
