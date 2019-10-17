// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Support.V4.Content;
using Google.AR.Core;
using Google.AR.Sceneform;
using System;

namespace SampleXamarin
{
    internal static class SceneformHelper
    {
        private const string CAMERA_PERMISSION = Manifest.Permission.Camera;

        // Check to see we have the necessary permissions for this app
        public static bool HasCameraPermission(Activity activity)
        {
            return ContextCompat.CheckSelfPermission(activity, CAMERA_PERMISSION)
                    == Permission.Granted;
        }

        public static bool TrySetupSessionForSceneView(Context context, ArSceneView sceneView)
        {
            try
            {
                Session session = new Session(context);
                Config config = new Config(session);
                config.SetUpdateMode(Config.UpdateMode.LatestCameraImage);
                session.Configure(config);
                sceneView.SetupSession(session);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("ASADemo: ", ex.ToString());

                return false;
            }

            return true;
        }
    }
}