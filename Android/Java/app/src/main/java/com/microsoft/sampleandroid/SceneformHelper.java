// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.content.Context;
import android.util.Log;

import com.google.ar.core.Config;
import com.google.ar.core.Session;
import com.google.ar.core.exceptions.UnavailableException;
import com.google.ar.sceneform.ArSceneView;

class SceneformHelper {
    public static void setupSessionForSceneView(Context context, ArSceneView sceneView) {
        try {
            Session session = new Session(context);
            Config config = new Config(session);
            config.setUpdateMode(Config.UpdateMode.LATEST_CAMERA_IMAGE);
            session.configure(config);
            sceneView.setupSession(session);
        }
        catch (UnavailableException e) {
            Log.e("ASADemo: ", e.toString());
        }
    }
}
