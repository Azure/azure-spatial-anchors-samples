// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.os.Bundle;
import android.support.v7.app.AppCompatActivity;
import android.util.Log;
import android.view.View;
import android.widget.EditText;
import android.widget.TextView;
import android.widget.Toast;
import android.widget.Button;
import android.view.MotionEvent;

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
import com.microsoft.azure.spatialanchors.LocateAnchorsCompletedEvent;

import java.text.DecimalFormat;
import java.util.concurrent.ConcurrentHashMap;

public class SharedActivity extends AppCompatActivity
{
    enum DemoStep
    {
        DemoStepChoosing, // Choosing to create or locate
        DemoStepCreating, // Creating an anchor
        DemoStepSaving,   // Saving an anchor to the cloud
        DemoStepEnteringAnchorNumber, // Picking an anchor to find
        DemoStepLocating  // Looking for an anchor
    }

    // Set this string to the URL created when publishing your Shared anchor service in the Sharing sample.
    private static final String SharingAnchorsServiceUrl = "";

    private String anchorId = "";
    private final ConcurrentHashMap<String, AnchorVisual> anchorVisuals = new ConcurrentHashMap<>();
    private AzureSpatialAnchorsManager cloudAnchorManager;
    private DemoStep currentStep = DemoStep.DemoStepChoosing;
    private static final DecimalFormat decimalFormat = new DecimalFormat("00");
    private String feedbackText;
    private final Object renderLock = new Object();

    // Materials
    private static Material failedColor;
    private static Material savedColor;
    private static Material readyColor;
    private static Material foundColor;

    // UI Elements
    private EditText anchorNumInput;
    private ArFragment arFragment;
    private Button createButton;
    private TextView editTextInfo;
    private Button locateButton;
    private ArSceneView sceneView;
    private TextView textView;

    public void createButtonClicked(View source) {
        textView.setText("Scan your environment and place an anchor");
        destroySession();

        cloudAnchorManager = new AzureSpatialAnchorsManager(sceneView.getSession());

        cloudAnchorManager.addSessionUpdatedListener(args -> {
            if (currentStep == DemoStep.DemoStepCreating) {
                float progress = args.getStatus().getRecommendedForCreateProgress();
                if (progress >= 1.0) {
                    AnchorVisual visual = anchorVisuals.get("");
                    if (visual != null) {
                        //Transition to saving...
                        transitionToSaving(visual);
                    } else {
                        feedbackText = "Tap somewhere to place an anchor.";
                    }
                } else {
                    feedbackText = "Progress is " + decimalFormat.format(progress * 100) + "%";
                }
            }
        });

        cloudAnchorManager.start();
        currentStep = DemoStep.DemoStepCreating;
        enableCorrectUIControls();
    }

    public void exitDemoClicked(View v) {
        synchronized (renderLock) {
            destroySession();

            finish();
        }
    }

    public void locateButtonClicked(View source) {
        if (currentStep == DemoStep.DemoStepChoosing) {
            currentStep = DemoStep.DemoStepEnteringAnchorNumber;
            textView.setText("Enter an anchor number and press locate");
            enableCorrectUIControls();
        } else {
            String inputVal = anchorNumInput.getText().toString();
            if (inputVal != null && !inputVal.isEmpty()) {

                AnchorGetter anchorExchanger = new AnchorGetter(SharingAnchorsServiceUrl, this::anchorLookedUp);
                anchorExchanger.execute(inputVal);

                currentStep = DemoStep.DemoStepLocating;
                enableCorrectUIControls();
            }
        }
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_shared);

        arFragment = (ArFragment)getSupportFragmentManager().findFragmentById(R.id.ux_fragment);
        arFragment.setOnTapArPlaneListener(this::onTapArPlaneListener);

        sceneView = arFragment.getArSceneView();

        textView = findViewById(R.id.textView);
        textView.setVisibility(View.VISIBLE);
        locateButton = findViewById(R.id.locateButton);
        createButton = findViewById(R.id.createButton);
        anchorNumInput = findViewById(R.id.anchorNumText);
        editTextInfo = findViewById(R.id.editTextInfo);
        enableCorrectUIControls();

        Scene scene = sceneView.getScene();
        scene.addOnUpdateListener(frameTime -> {
            if (cloudAnchorManager != null) {
                // Pass frames to Spatial Anchors for processing.
                cloudAnchorManager.update(sceneView.getArFrame());
            }
        });

        MaterialFactory.makeOpaqueWithColor(this, new Color(android.graphics.Color.RED))
                .thenAccept(material -> failedColor = material);

        MaterialFactory.makeOpaqueWithColor(this, new Color(android.graphics.Color.GREEN))
                .thenAccept(material -> savedColor = material);

        MaterialFactory.makeOpaqueWithColor(this, new Color(android.graphics.Color.YELLOW))
                .thenAccept(material -> {
                    readyColor = material;
                    foundColor = material;
                });
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
            SceneformHelper.setupSessionForSceneView(this, sceneView);
        }

        if ((AzureSpatialAnchorsManager.SpatialAnchorsAccountId == null || AzureSpatialAnchorsManager.SpatialAnchorsAccountId.equals("Set me"))
                || (AzureSpatialAnchorsManager.SpatialAnchorsAccountKey == null|| AzureSpatialAnchorsManager.SpatialAnchorsAccountKey.equals("Set me"))) {
            Toast.makeText(this, "\"Set SpatialAnchorsAccountId and SpatialAnchorsAccountKey in AzureSpatialAnchorsManager.java\"", Toast.LENGTH_LONG)
                    .show();

            finish();
            return;
        }

        if (SharingAnchorsServiceUrl == null || SharingAnchorsServiceUrl.isEmpty()) {
            Toast.makeText(this, "Set the SharingAnchorsServiceUrl in SharedActivity.java", Toast.LENGTH_LONG)
                    .show();

            finish();
        }

        updateStatic();
    }

    private void anchorLookedUp(String anchorId) {
        Log.d("ASADemo", "anchor " + anchorId);
        this.anchorId = anchorId;
        destroySession();

        cloudAnchorManager = new AzureSpatialAnchorsManager(sceneView.getSession());
        cloudAnchorManager.addAnchorLocatedListener((AnchorLocatedEvent event) ->
                runOnUiThread(() -> {
                    CloudSpatialAnchor anchor = event.getAnchor();
                    switch (event.getStatus()) {
                        case AlreadyTracked:
                        case Located:
                            AnchorVisual foundVisual = new AnchorVisual(anchor.getLocalAnchor());
                            foundVisual.setCloudAnchor(anchor);
                            foundVisual.getAnchorNode().setParent(arFragment.getArSceneView().getScene());
                            String cloudAnchorIdentifier = foundVisual.getCloudAnchor().getIdentifier();
                            foundVisual.setColor(foundColor);
                            foundVisual.render(arFragment);
                            anchorVisuals.put(cloudAnchorIdentifier, foundVisual);
                            break;
                        case NotLocatedAnchorDoesNotExist:
                            break;
                    }
                }));

        cloudAnchorManager.addLocateAnchorsCompletedListener((LocateAnchorsCompletedEvent event) -> {
            currentStep = DemoStep.DemoStepChoosing;

            runOnUiThread(() -> {
                textView.setText("Anchor located!");
                enableCorrectUIControls();
            });
        });
        cloudAnchorManager.start();
        AnchorLocateCriteria criteria = new AnchorLocateCriteria();
        criteria.setIdentifiers(new String[]{anchorId});
        cloudAnchorManager.startLocating(criteria);
    }

    private void anchorPosted(String anchorNumber) {
        textView.setText("Anchor Number: " + anchorNumber);
        currentStep = DemoStep.DemoStepChoosing;
        cloudAnchorManager.stop();
        cloudAnchorManager = null;
        clearVisuals();
        enableCorrectUIControls();
    }

    private Anchor createAnchor(HitResult hitResult) {

        AnchorVisual visual = new AnchorVisual(hitResult.createAnchor());
        visual.setColor(readyColor);
        visual.render(arFragment);
        anchorVisuals.put("", visual);

        return visual.getLocalAnchor();
    }

    private void clearVisuals() {
        for (AnchorVisual visual : anchorVisuals.values()) {
            visual.destroy();
        }

        anchorVisuals.clear();
    }

    private void createAnchorExceptionCompletion(String message) {
        textView.setText(message);
        currentStep = DemoStep.DemoStepChoosing;
        cloudAnchorManager.stop();
        cloudAnchorManager = null;
        enableCorrectUIControls();
    }

    private void destroySession() {
        if (cloudAnchorManager != null) {
            cloudAnchorManager.stop();
            cloudAnchorManager = null;
        }

        stopWatcher();

        clearVisuals();
    }

    private void enableCorrectUIControls() {
        switch (currentStep) {
            case DemoStepChoosing:
                textView.setVisibility(View.VISIBLE);
                locateButton.setVisibility(View.VISIBLE);
                createButton.setVisibility(View.VISIBLE);
                anchorNumInput.setVisibility(View.GONE);
                editTextInfo.setVisibility(View.GONE);
                break;
            case DemoStepCreating:
                textView.setVisibility(View.VISIBLE);
                locateButton.setVisibility(View.GONE);
                createButton.setVisibility(View.GONE);
                anchorNumInput.setVisibility(View.GONE);
                editTextInfo.setVisibility(View.GONE);
                break;
            case DemoStepLocating:
                textView.setVisibility(View.VISIBLE);
                locateButton.setVisibility(View.GONE);
                createButton.setVisibility(View.GONE);
                anchorNumInput.setVisibility(View.GONE);
                editTextInfo.setVisibility(View.GONE);
                break;
            case DemoStepSaving:
                textView.setVisibility(View.VISIBLE);
                locateButton.setVisibility(View.GONE);
                createButton.setVisibility(View.GONE);
                anchorNumInput.setVisibility(View.GONE);
                editTextInfo.setVisibility(View.GONE);
                break;
            case DemoStepEnteringAnchorNumber:
                textView.setVisibility(View.VISIBLE);
                locateButton.setVisibility(View.VISIBLE);
                createButton.setVisibility(View.GONE);
                anchorNumInput.setVisibility(View.VISIBLE);
                editTextInfo.setVisibility(View.VISIBLE);
                break;
        }
    }

    private void onTapArPlaneListener(HitResult hitResult, Plane plane, MotionEvent motionEvent) {
        if (currentStep == DemoStep.DemoStepCreating) {
            AnchorVisual visual = anchorVisuals.get("");
            if (visual == null) {
                createAnchor(hitResult);
            }
        }
    }

    private void stopWatcher() {
        if (cloudAnchorManager != null) {
            cloudAnchorManager.stopLocating();
        }
    }

    private void transitionToSaving(AnchorVisual visual) {
        Log.d("ASADemo:", "transition to saving");
        currentStep = DemoStep.DemoStepSaving;
        enableCorrectUIControls();
        Log.d("ASADemo", "creating anchor");
        CloudSpatialAnchor cloudAnchor = new CloudSpatialAnchor();
        visual.setCloudAnchor(cloudAnchor);
        cloudAnchor.setLocalAnchor(visual.getLocalAnchor());
        cloudAnchorManager.createAnchorAsync(cloudAnchor)
                .thenAccept(anchor -> {
                    String anchorId = anchor.getIdentifier();
                    Log.d("ASADemo:", "created anchor: " + anchorId);
                    visual.setColor(savedColor);
                    anchorVisuals.put(anchorId, visual);
                    anchorVisuals.remove("");

                    Log.d("ASADemo", "recording anchor with web service");
                    AnchorPoster poster = new AnchorPoster(SharingAnchorsServiceUrl, this::anchorPosted);
                    Log.d("ASADemo", "anchorId: " + anchorId);
                    poster.execute(anchorId);
                }).exceptionally(thrown -> {
                    thrown.printStackTrace();
                    String exceptionMessage = thrown.toString();
                    Throwable t = thrown.getCause();
                    if (t instanceof CloudSpatialException) {
                        exceptionMessage = (((CloudSpatialException) t).getErrorCode().toString());
                    }
                    createAnchorExceptionCompletion(exceptionMessage);
                    visual.setColor(failedColor);
                    return null;
        });
    }

    private void updateStatic() {
        new android.os.Handler().postDelayed(() -> {
            switch (currentStep) {
                case DemoStepChoosing:
                    break;
                case DemoStepCreating:
                    textView.setText(feedbackText);
                    break;
                case DemoStepLocating:
                    textView.setText("searching for\n" + anchorId);
                    break;
                case DemoStepSaving:
                    textView.setText("saving...");
                    break;
                case DemoStepEnteringAnchorNumber:

                    break;
            }

            updateStatic();
        }, 500);
    }
}
