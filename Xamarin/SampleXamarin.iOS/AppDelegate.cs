// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Foundation;
using UIKit;

namespace SampleXamarin.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        // class-level declarations

        public override UIWindow Window { get; set; }

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            // create a new window instance based on the screen size
            this.Window = new UIWindow(UIScreen.MainScreen.Bounds);

            MainViewController mainViewController = new MainViewController();

            //set root to navigation controller
            this.Window.RootViewController = new UINavigationController(mainViewController);

            // make the window visible
            this.Window.MakeKeyAndVisible();
            return true;
        }
    }
}