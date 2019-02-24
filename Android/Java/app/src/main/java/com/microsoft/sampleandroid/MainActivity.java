// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.os.Handler;
import android.widget.Button;
import android.widget.TextView;
import android.view.*;

import com.google.ar.core.*;
import com.google.ar.core.exceptions.UnavailableDeviceNotCompatibleException;
import com.google.ar.core.exceptions.UnavailableUserDeclinedInstallationException;

import static com.google.ar.core.ArCoreApk.getInstance;

public class MainActivity extends Activity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        findViewById(R.id.arBasicDemo).setVisibility(View.GONE);
        findViewById(R.id.arSharedDemo).setVisibility(View.GONE);
    }

    public void onArClick(View v) throws UnavailableUserDeclinedInstallationException, UnavailableDeviceNotCompatibleException {
        TextView tv = findViewById(R.id.sample_text);
        ArCoreApk.Availability availability = getInstance().checkAvailability(this);
        if (availability.isTransient()) {
            final View view = v;
            new Handler().postDelayed(new Runnable() {
                @Override
                public void run() {
                    try {
                        onArClick(view);
                    } catch (UnavailableUserDeclinedInstallationException e) {
                        e.printStackTrace();
                    } catch (UnavailableDeviceNotCompatibleException e) {
                        e.printStackTrace();
                    }
                }
            }, 200);
            return;
        }

        if (availability.isSupported()) {
            tv.setText("ARCore is ready");
            Button button = findViewById(R.id.arGo);
            button.setVisibility(View.GONE);
            findViewById(R.id.arBasicDemo).setVisibility(View.VISIBLE);
            findViewById(R.id.arSharedDemo).setVisibility(View.VISIBLE);

        } else {
            tv.setText("unavailable " + availability);
        }
    }

    public void onBasicDemoClick(View v)
    {
        findViewById(R.id.arBasicDemo).setVisibility(View.GONE);
        findViewById(R.id.arSharedDemo).setVisibility(View.GONE);
        findViewById(R.id.arGo).setVisibility(View.VISIBLE);
        Intent intent = new Intent(this, AzureSpatialAnchorsActivity.class);
        startActivity(intent);
    }

    public void onSharedDemoClick(View v)
    {
        findViewById(R.id.arBasicDemo).setVisibility(View.GONE);
        findViewById(R.id.arSharedDemo).setVisibility(View.GONE);
        findViewById(R.id.arGo).setVisibility(View.VISIBLE);
        Intent intent = new Intent(this, Shared.class);
        startActivity(intent);
    }
}
