// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.os.Bundle;
import android.support.v7.app.AppCompatActivity;
import android.util.Log;
import android.view.MotionEvent;
import android.view.View;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import com.google.ar.core.Anchor;
import com.google.ar.core.HitResult;
import com.google.ar.core.Plane;
import com.google.ar.sceneform.ArSceneView;
import com.google.ar.sceneform.Scene;
import com.google.ar.sceneform.rendering.Color;
import com.google.ar.sceneform.rendering.Material;
import com.google.ar.sceneform.rendering.MaterialFactory;
import com.google.ar.sceneform.ux.ArFragment;

import com.microsoft.azure.spatialanchors.AnchorLocateCriteria;
import com.microsoft.azure.spatialanchors.AnchorLocatedEvent;
import com.microsoft.azure.spatialanchors.CloudSpatialAnchor;
import com.microsoft.azure.spatialanchors.CloudSpatialException;
import com.microsoft.azure.spatialanchors.LocateAnchorStatus;
import com.microsoft.azure.spatialanchors.LocateAnchorsCompletedEvent;
import com.microsoft.azure.spatialanchors.NearAnchorCriteria;
import com.microsoft.azure.spatialanchors.SessionUpdatedEvent;

import java.text.DecimalFormat;
import java.util.Calendar;
import java.util.Date;
import java.util.concurrent.ConcurrentHashMap;

public class AzureSpatialAnchorsActivity extends AppCompatActivity
{
    private String anchorID;
    private final ConcurrentHashMap<String, AnchorVisual> anchorVisuals = new ConcurrentHashMap<>();
    private boolean basicDemo = true;
    private AzureSpatialAnchorsManager cloudAnchorManager;
    private DemoStep currentDemoStep = DemoStep.Start;
    private boolean enoughDataForSaving;
    private static final int numberOfNearbyAnchors = 3;
    private final Object progressLock = new Object();
    private final Object renderLock = new Object();
    private int saveCount = 0;

    // Materials
    private static final int FAILED_COLOR = android.graphics.Color.RED;
    private static final int FOUND_COLOR = android.graphics.Color.YELLOW;
    private static final int READY_COLOR = android.graphics.Color.YELLOW;
    private static final int SAVED_COLOR = android.graphics.Color.GREEN;

    // UI Elements
    private ArFragment arFragment;
    private Button actionButton;
    private Button backButton;
    private TextView scanProgressText;
    private ArSceneView sceneView;
    private TextView statusText;

    public void exitDemoClicked(View v) {
        synchronized (renderLock) {
            destroySession();
            finish();
        }
    }

    @Override
    @SuppressWarnings({"AndroidApiChecker", "FutureReturnValueIgnored"})
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_anchors);

        basicDemo = getIntent().getBooleanExtra("BasicDemo", true);

        arFragment = (ArFragment)getSupportFragmentManager().findFragmentById(R.id.ar_fragment);
        arFragment.setOnTapArPlaneListener(this::onTapArPlaneListener);

        sceneView = arFragment.getArSceneView();

        Scene scene = sceneView.getScene();
        scene.addOnUpdateListener(frameTime -> {
            if (cloudAnchorManager != null) {
                // Pass frames to Spatial Anchors for processing.
                cloudAnchorManager.update(sceneView.getArFrame());
            }
        });

        backButton = findViewById(R.id.backButton);
        statusText = findViewById(R.id.statusText);
        scanProgressText = findViewById(R.id.scanProgressText);
        actionButton = findViewById(R.id.actionButton);
        actionButton.setOnClickListener((View v) -> advanceDemo());
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        destroySession();
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

        if (currentDemoStep == DemoStep.Start) {
            startDemo();
        }
    }

    private void advanceDemo() {
        switch (currentDemoStep) {
            case SaveCloudAnchor:
                AnchorVisual visual = anchorVisuals.get("");
                if (visual == null) {
                    return;
                }

                if (!enoughDataForSaving) {
                    return;
                }

                // Hide the back button until we're done
                runOnUiThread(() -> backButton.setVisibility(View.GONE));

                setupLocalCloudAnchor(visual);

                cloudAnchorManager.createAnchorAsync(visual.getCloudAnchor())
                    .thenAccept(this::anchorSaveSuccess)
                    .exceptionally(thrown -> {
                        thrown.printStackTrace();
                        String exceptionMessage = thrown.toString();
                        Throwable t = thrown.getCause();
                        if (t instanceof CloudSpatialException) {
                            exceptionMessage = (((CloudSpatialException) t).getErrorCode().toString());
                        }

                        anchorSaveFailed(exceptionMessage);
                        return null;
                    });

                synchronized (progressLock) {
                    runOnUiThread(() -> {
                        scanProgressText.setVisibility(View.GONE);
                        scanProgressText.setText("");
                        actionButton.setVisibility(View.INVISIBLE);
                        statusText.setText("Saving cloud anchor...");
                    });
                    currentDemoStep = DemoStep.SavingCloudAnchor;
                }

                break;

            case CreateSessionForQuery:
                cloudAnchorManager.stop();
                cloudAnchorManager.reset();
                clearVisuals();

                runOnUiThread(() -> {
                    statusText.setText("");
                    actionButton.setText("Locate anchor");
                });

                currentDemoStep = DemoStep.LookForAnchor;

                break;

            case LookForAnchor:
                // We need to restart the session to find anchors we created.
                startNewSession();

                AnchorLocateCriteria criteria = new AnchorLocateCriteria();
                criteria.setIdentifiers(new String[]{anchorID});

                // Cannot run more than one watcher concurrently
                stopWatcher();

                cloudAnchorManager.startLocating(criteria);

                runOnUiThread(() -> {
                    actionButton.setVisibility(View.INVISIBLE);
                    statusText.setText("Look for anchor");
                });

                break;

            case LookForNearbyAnchors:
                if (anchorVisuals.isEmpty() || !anchorVisuals.containsKey(anchorID)) {
                    runOnUiThread(() -> statusText.setText("Cannot locate nearby. Previous anchor not yet located."));

                    break;
                }

                AnchorLocateCriteria nearbyLocateCriteria = new AnchorLocateCriteria();
                NearAnchorCriteria nearAnchorCriteria = new NearAnchorCriteria();
                nearAnchorCriteria.setDistanceInMeters(10);
                nearAnchorCriteria.setSourceAnchor(anchorVisuals.get(anchorID).getCloudAnchor());
                nearbyLocateCriteria.setNearAnchor(nearAnchorCriteria);
                // Cannot run more than one watcher concurrently
                stopWatcher();
                cloudAnchorManager.startLocating(nearbyLocateCriteria);
                runOnUiThread(() -> {
                    actionButton.setVisibility(View.INVISIBLE);
                    statusText.setText("Locating...");
                });

                break;

            case End:
                for (AnchorVisual toDeleteVisual : anchorVisuals.values()) {
                    cloudAnchorManager.deleteAnchorAsync(toDeleteVisual.getCloudAnchor());
                }

                destroySession();

                runOnUiThread(() -> {
                    actionButton.setText("Restart");
                    statusText.setText("");
                    backButton.setVisibility(View.VISIBLE);
                });

                currentDemoStep = DemoStep.Restart;

                break;

            case Restart:
                startDemo();
                break;
        }
    }

    private void anchorSaveSuccess(CloudSpatialAnchor result) {
        saveCount++;

        anchorID = result.getIdentifier();
        Log.d("ASADemo:", "created anchor: " + anchorID);

        AnchorVisual visual = anchorVisuals.get("");
        visual.setColor(this, SAVED_COLOR);
        anchorVisuals.put(anchorID, visual);
        anchorVisuals.remove("");

        if (basicDemo || saveCount == numberOfNearbyAnchors) {
            runOnUiThread(() -> {
                statusText.setText("");
                actionButton.setVisibility(View.VISIBLE);
            });

            currentDemoStep = DemoStep.CreateSessionForQuery;
            advanceDemo();
        }
        else {
            // Need to create more anchors for nearby demo
            runOnUiThread(() -> {
                statusText.setText("Tap a surface to create next anchor");
                actionButton.setVisibility(View.INVISIBLE);
            });

            currentDemoStep = DemoStep.CreateLocalAnchor;
        }
    }

    private void anchorSaveFailed(String message) {
        runOnUiThread(() -> statusText.setText(message));
        AnchorVisual visual = anchorVisuals.get("");
        visual.setColor(this, FAILED_COLOR);
    }

    private void clearVisuals() {
        for (AnchorVisual visual : anchorVisuals.values()) {
            visual.destroy();
        }

        anchorVisuals.clear();
    }

    private Anchor createAnchor(HitResult hitResult) {
        AnchorVisual visual = new AnchorVisual(arFragment, hitResult.createAnchor());
        visual.setColor(this, READY_COLOR);
        visual.render(arFragment);
        anchorVisuals.put("", visual);

        runOnUiThread(() -> {
            scanProgressText.setVisibility(View.VISIBLE);
            if (enoughDataForSaving) {
                statusText.setText("Ready to save");
                actionButton.setText("Save cloud anchor");
                actionButton.setVisibility(View.VISIBLE);
            }
            else {
                statusText.setText("Move around the anchor");
            }
        });

        currentDemoStep = DemoStep.SaveCloudAnchor;

        return visual.getLocalAnchor();
    }

    private void destroySession() {
        if (cloudAnchorManager != null) {
            cloudAnchorManager.stop();
            cloudAnchorManager = null;
        }

        clearVisuals();
    }

    private void onAnchorLocated(AnchorLocatedEvent event) {
        LocateAnchorStatus status = event.getStatus();

        runOnUiThread(() -> {
            switch (status) {
                case AlreadyTracked:
                    break;

                case Located:
                    renderLocatedAnchor(event.getAnchor());
                    break;

                case NotLocatedAnchorDoesNotExist:
                    statusText.setText("Anchor does not exist");
                    break;
            }
        });
    }

    private void onLocateAnchorsCompleted(LocateAnchorsCompletedEvent event) {
        runOnUiThread(() -> statusText.setText("Anchor located!"));

        if (!basicDemo && currentDemoStep == DemoStep.LookForAnchor) {
            runOnUiThread(() -> {
                actionButton.setVisibility(View.VISIBLE);
                actionButton.setText("Look for anchors nearby");
            });
            currentDemoStep = DemoStep.LookForNearbyAnchors;
        } else {
            stopWatcher();
            runOnUiThread(() -> {
                actionButton.setVisibility(View.VISIBLE);
                actionButton.setText("Cleanup anchors");
            });
            currentDemoStep = DemoStep.End;
        }
    }

    private void onSessionUpdated(SessionUpdatedEvent args) {
        float progress = args.getStatus().getRecommendedForCreateProgress();
        enoughDataForSaving = progress >= 1.0;
        synchronized (progressLock) {
            if (currentDemoStep == DemoStep.SaveCloudAnchor) {
                DecimalFormat decimalFormat = new DecimalFormat("00");
                runOnUiThread(() -> {
                    String progressMessage = "Scan progress is " + decimalFormat.format(Math.min(1.0f, progress) * 100) + "%";
                    scanProgressText.setText(progressMessage);
                });

                if (enoughDataForSaving && actionButton.getVisibility() != View.VISIBLE) {
                    // Enable the save button
                    runOnUiThread(() -> {
                        statusText.setText("Ready to save");
                        actionButton.setText("Save cloud anchor");
                        actionButton.setVisibility(View.VISIBLE);
                    });
                    currentDemoStep = DemoStep.SaveCloudAnchor;
                }
            }
        }
    }

    private void onTapArPlaneListener(HitResult hitResult, Plane plane, MotionEvent motionEvent) {
        if (currentDemoStep == DemoStep.CreateLocalAnchor) {
            createAnchor(hitResult);
        }
    }

    private void renderLocatedAnchor(CloudSpatialAnchor anchor) {
        AnchorVisual foundVisual = new AnchorVisual(arFragment, anchor.getLocalAnchor());
        foundVisual.setCloudAnchor(anchor);
        foundVisual.getAnchorNode().setParent(arFragment.getArSceneView().getScene());
        String cloudAnchorIdentifier = foundVisual.getCloudAnchor().getIdentifier();
        foundVisual.setColor(this, FOUND_COLOR);
        foundVisual.render(arFragment);
        anchorVisuals.put(cloudAnchorIdentifier, foundVisual);
    }

    private void setupLocalCloudAnchor(AnchorVisual visual) {
        CloudSpatialAnchor cloudAnchor = new CloudSpatialAnchor();
        cloudAnchor.setLocalAnchor(visual.getLocalAnchor());
        visual.setCloudAnchor(cloudAnchor);

        // In this sample app we delete the cloud anchor explicitly, but you can also set it to expire automatically
        Date now = new Date();
        Calendar cal = Calendar.getInstance();
        cal.setTime(now);
        cal.add(Calendar.DATE, 7);
        Date oneWeekFromNow = cal.getTime();
        cloudAnchor.setExpiration(oneWeekFromNow);
    }

    private void startDemo() {
        saveCount = 0;
        startNewSession();
        runOnUiThread(() -> {
            scanProgressText.setVisibility(View.GONE);
            statusText.setText("Tap a surface to create an anchor");
            actionButton.setVisibility(View.INVISIBLE);
        });
        currentDemoStep = DemoStep.CreateLocalAnchor;
    }

    private void startNewSession() {
        destroySession();

        cloudAnchorManager = new AzureSpatialAnchorsManager(sceneView.getSession());
        cloudAnchorManager.addAnchorLocatedListener(this::onAnchorLocated);
        cloudAnchorManager.addLocateAnchorsCompletedListener(this::onLocateAnchorsCompleted);
        cloudAnchorManager.addSessionUpdatedListener(this::onSessionUpdated);
        cloudAnchorManager.start();
    }

    private void stopWatcher() {
        if (cloudAnchorManager != null) {
            cloudAnchorManager.stopLocating();
        }
    }

    enum DemoStep {
        Start,                          ///< the start of the demo
        CreateLocalAnchor,      ///< the session will create a local anchor
        SaveCloudAnchor,        ///< the session will save the cloud anchor
        SavingCloudAnchor,      ///< the session is in the process of saving the cloud anchor
        CreateSessionForQuery,  ///< a session will be created to query for an anchor
        LookForAnchor,          ///< the session will run the query
        LookForNearbyAnchors,   ///< the session will run a query for nearby anchors
        End,                            ///< the end of the demo
        Restart,                        ///< waiting to restart
    }
}