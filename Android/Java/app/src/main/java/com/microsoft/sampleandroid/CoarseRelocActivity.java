// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.os.Bundle;
import android.support.annotation.NonNull;
import android.support.v4.app.FragmentActivity;
import android.view.View;
import android.widget.Toast;

import com.google.ar.sceneform.ArSceneView;
import com.google.ar.sceneform.Scene;
import com.google.ar.sceneform.ux.ArFragment;
import com.microsoft.azure.spatialanchors.CloudSpatialAnchor;
import com.microsoft.azure.spatialanchors.PlatformLocationProvider;

import java.util.Map;

import static android.graphics.Color.GREEN;

public class CoarseRelocActivity extends FragmentActivity
        implements AnchorPlacementListener, AnchorCreationListener, AnchorDiscoveryListener {
    private AzureSpatialAnchorsManager cloudAnchorManager;
    private PlatformLocationProvider locationProvider;

    private ArFragment arFragment;
    private ArSceneView sceneView;
    private SensorStatusView sensorStatusView;
    private static final int REQUEST_CODE_ALL_SENSORS = 1;

    @Override
    public void onRequestPermissionsResult(int requestCode, @NonNull String[] permissions, @NonNull int[] grantResults) {
        if (requestCode == REQUEST_CODE_ALL_SENSORS) {
            if (!SensorPermissionsHelper.hasAllRequiredPermissionGranted(this)) {
                Toast.makeText(
                        this,
                        "Location permission is needed to run this demo",
                        Toast.LENGTH_LONG)
                        .show();
                finish();
            } else if (locationProvider != null) {
                SensorPermissionsHelper.enableAllowedSensors(this, locationProvider);
            }
        }
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_coarse_reloc);

        arFragment = (ArFragment)getSupportFragmentManager().findFragmentById(R.id.ar_fragment);
        sceneView = arFragment.getArSceneView();
        sensorStatusView = findViewById(R.id.sensor_status);

        Scene scene = sceneView.getScene();
        scene.addOnUpdateListener(frameTime -> {
            if (cloudAnchorManager != null) {
                // Pass frames to Azure Spatial Anchors for processing.
                cloudAnchorManager.update(sceneView.getArFrame());
            }

            sensorStatusView.update();
        });

        FragmentHelper.replaceFragment(this, new ActionSelectionFragment());
    }

    @Override
    protected void onResume() {
        super.onResume();

        // ArFragment of Sceneform automatically requests the camera permission before creating the AR session,
        // so we don't need to request the camera permission explicitly.
        // This will cause onResume to be called again after the user responds to the permission request.
        if (!SceneformHelper.hasCameraPermission(this)) {
            return;
        }

        if (sceneView != null && sceneView.getSession() == null) {
            if (!SceneformHelper.trySetupSessionForSceneView(this, sceneView)) {
                finish();
                return;
            }
        }

        if ((AzureSpatialAnchorsManager.SpatialAnchorsAccountId == null || AzureSpatialAnchorsManager.SpatialAnchorsAccountId.equals("Set me"))
                || (AzureSpatialAnchorsManager.SpatialAnchorsAccountKey == null|| AzureSpatialAnchorsManager.SpatialAnchorsAccountKey.equals("Set me"))) {
            Toast.makeText(this, "\"Set SpatialAnchorsAccountId and SpatialAnchorsAccountKey in AzureSpatialAnchorsManager.java\"", Toast.LENGTH_LONG)
                    .show();

            finish();
        }

        SensorPermissionsHelper.requestMissingPermissions(this, REQUEST_CODE_ALL_SENSORS);

        locationProvider = new PlatformLocationProvider();
        locationProvider.getSensors().setKnownBeaconProximityUuids(
                CoarseRelocSettings.KNOWN_BLUETOOTH_PROXIMITY_UUIDS);
        SensorPermissionsHelper.enableAllowedSensors(this, locationProvider);

        cloudAnchorManager = new AzureSpatialAnchorsManager(sceneView.getSession());
        cloudAnchorManager.setLocationProvider(locationProvider);
        cloudAnchorManager.start();

        sensorStatusView.setModel(new LocationProviderSensorStatus(locationProvider));
    }

    @Override
    protected void onPause() {

        sensorStatusView.setModel(null);

        if (cloudAnchorManager != null) {
            cloudAnchorManager.stop();
            cloudAnchorManager = null;
        }
        locationProvider = null;

        super.onPause();
    }

    public void onAddAnchorClicked(View view) {
        AnchorPlacementFragment placementFragment = new AnchorPlacementFragment();
        placementFragment.setListener(this);
        FragmentHelper.pushFragment(this, placementFragment);
    }

    @Override
    public void onAnchorPlaced(AnchorVisual placedAnchor) {
        AnchorCreationFragment creationFragment = new AnchorCreationFragment();
        creationFragment.setListener(this);
        creationFragment.setCloudAnchorManager(cloudAnchorManager);
        creationFragment.setPlacement(placedAnchor);
        FragmentHelper.backToPreviousFragment(this);
        FragmentHelper.pushFragment(this, creationFragment);
    }

    @Override
    public void onAnchorCreated(AnchorVisual createdAnchor) {
        createdAnchor.setColor(this, GREEN);
        FragmentHelper.backToPreviousFragment(this);
    }

    @Override
    public void onAnchorCreationFailed(AnchorVisual placedAnchor, String errorMessage) {
        placedAnchor.destroy();
        FragmentHelper.backToPreviousFragment(this);
        runOnUiThread(() -> {
            String toastMessage = "Failed to save anchor: " + errorMessage;
            Toast.makeText(this, toastMessage, Toast.LENGTH_LONG).show();
        });
    }

    public void onStartWatcherClicked(View view) {
        WatcherFragment watcherFragment = new WatcherFragment();
        watcherFragment.setCloudAnchorManager(cloudAnchorManager);
        watcherFragment.setListener(this);
        FragmentHelper.pushFragment(this, watcherFragment);
    }

    @Override
    public void onAnchorDiscovered(CloudSpatialAnchor cloudAnchor) {
        AnchorVisual visual = new AnchorVisual(arFragment, cloudAnchor);
        visual.setColor(this, GREEN);
        Map<String, String> properties = cloudAnchor.getAppProperties();
        if (properties.containsKey("Shape")) {
            try {
                AnchorVisual.Shape savedShape = AnchorVisual.Shape.valueOf(properties.get("Shape"));
                visual.setShape(savedShape);
            } catch (IllegalArgumentException ex) {
                // Invalid shape property, keep default shape
            }
        }
        visual.render(arFragment);
    }

    public void onBackClicked(View view) {
        if (!FragmentHelper.backToPreviousFragment(this)) {
            finish();
        }
    }
}
