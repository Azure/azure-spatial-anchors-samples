// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.os.Handler;
import android.os.Looper;

class MainThreadContext {
    private static final Handler mainHandler = new Handler(Looper.getMainLooper());
    private static final Looper mainLooper = Looper.getMainLooper();

    public static void runOnUiThread(Runnable runnable){
        if (mainLooper.isCurrentThread()) {
            runnable.run();
        } else {
            mainHandler.post(runnable);
        }
    }
}
