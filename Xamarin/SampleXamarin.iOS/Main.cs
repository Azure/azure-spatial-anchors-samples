// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using UIKit;

namespace SampleXamarin.iOS
{
    public static class Application
    {
        // This is the main entry point of the application.
        public static void Main(string[] args)
        {
            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}