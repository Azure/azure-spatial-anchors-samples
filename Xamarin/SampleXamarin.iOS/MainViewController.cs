// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using CoreGraphics;
using UIKit;

namespace SampleXamarin.iOS
{
    public class MainViewController : UIViewController
    {
        public MainViewController()
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.View.BackgroundColor = UIColor.White;
            this.Title = "Sample Xamarin iOS";

            UILabel mainLabel = new UILabel()
            {
                Text = "Select a Spatial Anchors Demo",
                TextAlignment = UITextAlignment.Center,
                Frame = new CGRect(10, 75, this.View.Frame.Width - 20, 44)
            };
            mainLabel.Font = mainLabel.Font.WithSize(21);
            this.View.AddSubview(mainLabel);

            UIButton basicDemoButton = new UIButton(UIButtonType.System)
            {
                Frame = new CGRect(this.View.Frame.Width / 2 - 40, 150, 75, 44),
                BackgroundColor = UIColor.LightGray.ColorWithAlpha((System.nfloat)0.6)
            };
            basicDemoButton.SetTitle("Basic", UIControlState.Normal);
            basicDemoButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            this.View.AddSubview(basicDemoButton);

            UILabel basicDemoLabel = new UILabel()
            {
                Text = "Create and locate and anchor",
                TextAlignment = UITextAlignment.Center,
                Frame = new CGRect(10, 185, this.View.Frame.Width - 20, 44)
            };

            basicDemoButton.TouchUpInside += (sender, e) =>
            {
                this.NavigationController.PushViewController(new BasicDemoView(), true);
            };

            this.View.AddSubview(basicDemoLabel);

            UIButton nearbyDemoButton = new UIButton(UIButtonType.System)
            {
                Frame = new CGRect(this.View.Frame.Width / 2 - 40, 245, 75, 44),
                BackgroundColor = UIColor.LightGray.ColorWithAlpha((System.nfloat)0.6)
            };
            nearbyDemoButton.SetTitle("Nearby", UIControlState.Normal);
            nearbyDemoButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            this.View.AddSubview(nearbyDemoButton);

            UILabel nearbyDemoLabel = new UILabel()
            {
                Text = "Create 3 anchors and locate the last. Then, find the other 2 anchors using a query for neaby anchors",
                TextAlignment = UITextAlignment.Center,
                Frame = new CGRect(10, 270, this.View.Frame.Width - 20, 100),
                LineBreakMode = UILineBreakMode.WordWrap,
                Lines = 4
            };

            nearbyDemoButton.TouchUpInside += (sender, e) =>
            {
                this.NavigationController.PushViewController(new NearByDemoView(), true);
            };

            this.View.AddSubview(nearbyDemoLabel);

            UIButton shareDemoButton = new UIButton(UIButtonType.System)
            {
                Frame = new CGRect(this.View.Frame.Width / 2 - 40, 385, 75, 44),
                BackgroundColor = UIColor.LightGray.ColorWithAlpha((System.nfloat)0.6)
            };
            shareDemoButton.SetTitle("Shared", UIControlState.Normal);
            shareDemoButton.SetTitleColor(UIColor.White, UIControlState.Normal);
            this.View.AddSubview(shareDemoButton);

            UILabel shareDemoLabel = new UILabel()
            {
                Text = "Create and locate anchors between multiple devices",
                TextAlignment = UITextAlignment.Center,
                Frame = new CGRect(10, 400, this.View.Frame.Width - 20, 100),
                LineBreakMode = UILineBreakMode.WordWrap,
                Lines = 2
            };

            shareDemoButton.TouchUpInside += (sender, e) =>
            {
                this.NavigationController.PushViewController(new ShareDemoController(), true);
            };
            this.View.AddSubview(shareDemoLabel);
        }
    }
}