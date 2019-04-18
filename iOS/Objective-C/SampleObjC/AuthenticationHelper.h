// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#import <Foundation/Foundation.h>
#import <ADAL/ADAL.h>

@interface AuthenticationHelper : NSObject

+ (void) acquireAuthenticationToken: (void (^)(NSString *token, NSError *error))completionBlock;

@end
