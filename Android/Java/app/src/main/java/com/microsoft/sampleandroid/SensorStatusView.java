// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.content.Context;
import android.support.annotation.Nullable;
import android.support.v4.content.ContextCompat;
import android.util.AttributeSet;
import android.widget.LinearLayout;
import android.widget.TextView;

public class SensorStatusView extends LinearLayout {
    private SensorStatusModel model;

    private TextView geoLocationStatusIcon;
    private TextView wifiStatusIcon;
    private TextView bluetoothStatusIcon;

    public SensorStatusView(Context context) {
        super(context);
        init(context);
    }

    public SensorStatusView(Context context, @Nullable AttributeSet attrs) {
        super(context, attrs);
        init(context);
    }

    public SensorStatusView(Context context, @Nullable AttributeSet attrs, int defStyleAttr) {
        super(context, attrs, defStyleAttr);
        init(context);
    }

    public void setModel(SensorStatusModel model) {
        this.model = model;
    }

    public void update() {
        geoLocationStatusIcon.setTextColor(getStatusColor(model.getGeoLocationStatus()));
        wifiStatusIcon.setTextColor(getStatusColor(model.getWifiSignalStatus()));
        bluetoothStatusIcon.setTextColor(getStatusColor(model.getBluetoothSignalStatus()));
    }

    private void init(Context context) {
        setOrientation(LinearLayout.VERTICAL);
        inflate(context, R.layout.sensor_status, this);
        geoLocationStatusIcon = findViewById(R.id.geolocation_status);
        wifiStatusIcon = findViewById(R.id.wifi_status);
        bluetoothStatusIcon = findViewById(R.id.bluetooth_status);
    }

    private int getStatusColor(SensorStatus status) {
        switch (status) {
            case Indeterminate:
                return ContextCompat.getColor(getContext(), R.color.sensorStatusIndeterminate);
            case Blocked:
                return ContextCompat.getColor(getContext(), R.color.sensorStatusBlocked);
            case Unavailable:
                return ContextCompat.getColor(getContext(), R.color.sensorStatusUnavailable);
            case Available:
                return ContextCompat.getColor(getContext(), R.color.sensorStatusAvailable);
        }

        throw new IllegalArgumentException("Invalid sensor status");
    }
}
