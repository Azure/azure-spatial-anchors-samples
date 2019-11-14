// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Android.Content;
using Android.Content.Res;
using Android.Util;
using Android.Widget;
using System;
using Android.Graphics;

namespace SampleXamarin
{
    public class SensorStatusView : LinearLayout
    {
        private TextView geoLocationStatusText;
        private TextView wifiStatusText;
        private TextView bluetoothStatusText;

        public SensorStatusView(Context context)
            : base(context)
        {
            Initialize(context);
        }

        public SensorStatusView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize(context);
        }

        public SensorStatusView(Context context, IAttributeSet attrs, int defStyleAttr)
            : base(context, attrs, defStyleAttr)
        {
            Initialize(context);
        }

        public SensorStatusModel Model { get; set; }

        public void Update()
        {
            geoLocationStatusText.SetTextColor(GetStatusColor(Model?.GeoLocationStatus));
            wifiStatusText.SetTextColor(GetStatusColor(Model?.WifiSignalStatus));
            bluetoothStatusText.SetTextColor(GetStatusColor(Model?.BluetoothSignalStatus));
        }

        private void Initialize(Context context)
        {
            Orientation = Android.Widget.Orientation.Vertical;
            Inflate(context, Resource.Layout.sensor_status, this);
            geoLocationStatusText = FindViewById<TextView>(Resource.Id.geolocation_status);
            wifiStatusText = FindViewById<TextView>(Resource.Id.wifi_status);
            bluetoothStatusText = FindViewById<TextView>(Resource.Id.bluetooth_status);
        }
        private Color GetStatusColor(SensorStatus? status)
        {
            if (!status.HasValue)
                return new Color(Context.GetColor(Resource.Color.sensorStatusIndeterminate));

            switch (status.Value)
            {
                case SensorStatus.Indeterminate:
                    return new Color(Context.GetColor(Resource.Color.sensorStatusIndeterminate));
                case SensorStatus.Blocked:
                    return new Color(Context.GetColor(Resource.Color.sensorStatusBlocked));
                case SensorStatus.Unavailable:
                    return new Color(Context.GetColor(Resource.Color.sensorStatusUnavailable));
                case SensorStatus.Available:
                    return new Color(Context.GetColor(Resource.Color.sensorStatusAvailable));
            }

            throw new ArgumentException("Invalid sensor status");
        }
    }
}