// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.app.Application;

import com.microsoft.CloudServices;

public class SampleApplication extends Application {

    @Override
    public void onCreate() {
        super.onCreate();

        // Use application's context to initialize CloudServices!
        CloudServices.initialize(this);
    }
}
