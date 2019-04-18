// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#import "AuthenticationHelper.h"

// You can use this helper method to get an authentication token via Azure Active Directory.
// For getting going quickly, you can instead set the SpatialAnchorsAccountId and SpatialAnchorsAccountKey in ViewController.m.

// Set these strings to the Service URL provided for the Azure Spatial Service resource.
NSString *const AuthServiceBaseUrl = @"https://sts.mixedreality.azure.com";
NSString *const ClientId = @"Set me";
NSString *const TenantId = @"Set me";

@implementation AuthenticationHelper {
}

+ (void) acquireAuthenticationToken:(void (^)(NSString *token, NSError *error))completionBlock
{
    ADAuthenticationError *error;
    ADAuthenticationContext *authContext = [ADAuthenticationContext authenticationContextWithAuthority:[NSString stringWithFormat:@"https://login.microsoftonline.com/%@", TenantId] error:&error];
    NSURL *redirectUri = [[NSURL alloc]initWithString:@"urn:ietf:wg:oauth:2.0:oob"];
    
    [authContext acquireTokenWithResource:AuthServiceBaseUrl
                                 clientId:ClientId
                              redirectUri:redirectUri
                          completionBlock:^(ADAuthenticationResult *result) {
                              
                              if (result.status != AD_SUCCEEDED)
                              {
                                  completionBlock(nil, result.error);
                              }
                              else
                              {
                                  completionBlock(result.accessToken, nil);
                              }
                          }];
}

@end
