// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace SampleXamarin.iOS
{
    [Register ("SensorStatusView")]
    partial class SensorStatusView
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIImageView bluetoothStatusIcon { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIImageView geoLocationStatusIcon { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIImageView wifiStatusIcon { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (bluetoothStatusIcon != null) {
                bluetoothStatusIcon.Dispose ();
                bluetoothStatusIcon = null;
            }

            if (geoLocationStatusIcon != null) {
                geoLocationStatusIcon.Dispose ();
                geoLocationStatusIcon = null;
            }

            if (wifiStatusIcon != null) {
                wifiStatusIcon.Dispose ();
                wifiStatusIcon = null;
            }
        }
    }
}