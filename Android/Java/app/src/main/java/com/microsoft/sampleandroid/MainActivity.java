// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.view.*;

public class MainActivity extends Activity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
    }

    public void onBasicDemoClick(View v) {
        Intent intent = new Intent(this, AzureSpatialAnchorsActivity.class);
        intent.putExtra("BasicDemo", true);
        startActivity(intent);
    }

    public void onNearbyDemoClick(View v) {
        Intent intent = new Intent(this, AzureSpatialAnchorsActivity.class);
        intent.putExtra("BasicDemo", false);
        startActivity(intent);
    }

    public void onSharedDemoClick(View v) {
        Intent intent = new Intent(this, SharedActivity.class);
        startActivity(intent);
    }

    public void onCoarseRelocDemoClick(View v) {
        Intent intent = new Intent(this, CoarseRelocActivity.class);
        startActivity(intent);
    }
}
