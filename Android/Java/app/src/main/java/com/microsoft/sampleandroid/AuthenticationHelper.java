// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.app.Activity;
import android.content.Intent;
import android.text.TextUtils;
import android.util.Log;

import com.microsoft.aad.adal.*;

// You can use this helper method to get an authentication token via Azure Active Directory.
// For getting going quickly, you can instead set the SpatialAnchorsAccountId and SpatialAnchorsAccountKey in AzureSpatialAnchorsManager.java
class AuthenticationHelper {
    private static final String AuthServiceBaseUrl = "https://sts.mixedreality.azure.com";

    private static final String ClientId = "Set me";
    private static final String TenantId = "Set me";

    private static final String TAG = "AuthenticationHelper";

    private AuthenticationContext authContext;

    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (authContext != null) {
            authContext.onActivityResult(requestCode, resultCode, data);
        }
    }

    public void GetAuthenticationToken(Activity activity, AuthenticationHelperCallback callback) {
        if (authContext == null) {
            authContext = new AuthenticationContext(activity, String.format("https://login.microsoftonline.com/%s", TenantId), true);
        }

        String redirectUri = "urn:ietf:wg:oauth:2.0:oob";

        authContext.acquireToken(activity, AuthServiceBaseUrl, ClientId, redirectUri, PromptBehavior.Auto, getAuthenticationCallback(callback));
    }

    private AuthenticationCallback<AuthenticationResult> getAuthenticationCallback(AuthenticationHelperCallback callback) {
        return new AuthenticationCallback<AuthenticationResult>() {
            @Override
            public void onSuccess(AuthenticationResult authenticationResult) {
                if(authenticationResult == null || TextUtils.isEmpty(authenticationResult.getAccessToken())
                        || authenticationResult.getStatus()!= AuthenticationResult.AuthenticationStatus.Succeeded){
                    Log.e(TAG, "Authentication Result is invalid");
                    return;
                }

                Log.d(TAG, "Successfully authenticated");
                callback.complete(authenticationResult.getAccessToken());
            }

            @Override
            public void onError(Exception exception) {
                Log.e(TAG, "Authentication failed: " + exception.toString());
                if (exception instanceof AuthenticationException) {
                    ADALError  error = ((AuthenticationException)exception).getCode();

                    if(error == ADALError.AUTH_FAILED_CANCELLED) {
                        Log.e(TAG, "The user cancelled the authorization request");
                    }
                }

                callback.complete("");
            }
        };
    }
}
