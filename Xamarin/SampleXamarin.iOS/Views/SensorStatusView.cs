// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using CoreGraphics;
using Foundation;
using ObjCRuntime;
using System;
using UIKit;

namespace SampleXamarin.iOS
{
    [Register(nameof(SensorStatusView))]
    public class SensorStatusView : UIView
    {
        [Outlet] private UIImageView GeoLocationStatusIcon { get; set; }
        [Outlet] private UIImageView WifiStatusIcon { get; set; }
        [Outlet] private UIImageView BluetoothStatusIcon { get; set; }

        public SensorStatusModel Model { private get; set; }

        public SensorStatusView(IntPtr handle)
            : base(handle)
        {
            Initialize();
        }

        public SensorStatusView(CGRect frame)
            : base(frame)
        {
            Initialize();
        }

        public SensorStatusView(NSCoder aDecoder)
            : base(aDecoder)
        {
            Initialize();
        }

        private void Initialize()
        {
            var ownType = typeof(SensorStatusView);
            var bundle = NSBundle.FromClass(new Class(ownType));
            var nib = bundle.LoadNib(ownType.Name, owner: this, options: null);
            var contentView = nib?.GetItem<UIView>(0);
            if (contentView != null)
            {
                var adjustedFrame = Frame;
                adjustedFrame.Size = contentView.Frame.Size;
                Frame = adjustedFrame;
                AddSubview(contentView);
            }
        }

        public void Update()
        {
            GeoLocationStatusIcon.Image = GetStatusIcon(status: Model?.GeoLocationStatus);
            WifiStatusIcon.Image = GetStatusIcon(status: Model?.WifiSignalStatus);
            BluetoothStatusIcon.Image = GetStatusIcon(status: Model?.BluetoothSignalStatus);
        }

        private UIImage GetStatusIcon(SensorStatus? status)
        {
            switch (status ?? SensorStatus.Indeterminate)
            {
                case SensorStatus.Indeterminate: return UIImage.FromBundle("gray-circle");
                case SensorStatus.Blocked: return UIImage.FromBundle("red-circle");
                case SensorStatus.Unavailable: return UIImage.FromBundle("orange-circle");
                case SensorStatus.Available: return UIImage.FromBundle("green-circle");
            }

            throw new InvalidOperationException("Invalid sensor status");
        }
    }
}