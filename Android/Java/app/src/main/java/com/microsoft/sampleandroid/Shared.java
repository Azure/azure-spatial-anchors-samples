// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.os.Bundle;
import android.util.Log;
import android.os.Handler;
import android.view.View;
import android.widget.EditText;
import android.widget.TextView;
import android.widget.Toast;
import android.widget.Button;
import android.view.MotionEvent;
import com.google.ar.core.HitResult;
import com.google.ar.core.Plane;
import com.microsoft.azure.spatialanchors.AnchorLocateCriteria;
import com.microsoft.azure.spatialanchors.AnchorLocatedEvent;
import com.microsoft.azure.spatialanchors.CloudSpatialAnchor;
import com.microsoft.azure.spatialanchors.CloudSpatialAnchorSession;
import com.microsoft.azure.spatialanchors.LocateAnchorsCompletedEvent;

import java.text.DecimalFormat;
import java.util.concurrent.Future;

public class Shared extends AzureSpatialAnchorsActivity
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

    private TextView mTextView;
    private Button mLocateButton;
    private Button mCreateButton;
    private TextView mEditTextInfo;
    private EditText mAnchorNumInput;
    private DecimalFormat mDecimalFormat = new DecimalFormat("00");
    private String mFeedbackText;
    private DemoStep currentStep = DemoStep.DemoStepChoosing;

    private String mAnchorId = "";
    private final Handler mHandler = new Handler();

    @Override
    protected void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);
    }

    @Override
    protected void SetContentView()
    {
        setContentView(R.layout.activity_shared);
    }

    @Override
    protected void ConfigureUI()
    {
        if (SharingAnchorsServiceUrl == "") {
            Toast.makeText(this, "Set the SharingAnchorsServiceUrl in Shared.java", Toast.LENGTH_LONG)
                    .show();

            finish();
        }

        mTextView = findViewById(R.id.textView);
        mTextView.setVisibility(View.VISIBLE);
        mLocateButton = findViewById(R.id.locateButton);
        mCreateButton = findViewById(R.id.createButton);
        mAnchorNumInput = findViewById(R.id.anchorNumText);
        mEditTextInfo = findViewById(R.id.editTextInfo);
        EnableCorrectUIControls();
    }

    private void EnableCorrectUIControls()
    {
        switch (currentStep) {
            case DemoStepChoosing:
                mTextView.setVisibility(View.VISIBLE);
                mLocateButton.setVisibility(View.VISIBLE);
                mCreateButton.setVisibility(View.VISIBLE);
                mAnchorNumInput.setVisibility(View.GONE);
                mEditTextInfo.setVisibility(View.GONE);
                break;
            case DemoStepCreating:
                mTextView.setVisibility(View.VISIBLE);
                mLocateButton.setVisibility(View.GONE);
                mCreateButton.setVisibility(View.GONE);
                mAnchorNumInput.setVisibility(View.GONE);
                mEditTextInfo.setVisibility(View.GONE);
                break;
            case DemoStepLocating:
                mTextView.setVisibility(View.VISIBLE);
                mLocateButton.setVisibility(View.GONE);
                mCreateButton.setVisibility(View.GONE);
                mAnchorNumInput.setVisibility(View.GONE);
                mEditTextInfo.setVisibility(View.GONE);
                break;
            case DemoStepSaving:
                mTextView.setVisibility(View.VISIBLE);
                mLocateButton.setVisibility(View.GONE);
                mCreateButton.setVisibility(View.GONE);
                mAnchorNumInput.setVisibility(View.GONE);
                mEditTextInfo.setVisibility(View.GONE);
                break;
            case DemoStepEnteringAnchorNumber:
                mTextView.setVisibility(View.VISIBLE);
                mLocateButton.setVisibility(View.VISIBLE);
                mCreateButton.setVisibility(View.GONE);
                mAnchorNumInput.setVisibility(View.VISIBLE);
                mEditTextInfo.setVisibility(View.VISIBLE);
                break;
        }
    }

    @Override
    protected void UpdateStatic()
    {
        new android.os.Handler().postDelayed(() -> {
                    switch (currentStep) {
                        case DemoStepChoosing:
                            break;
                        case DemoStepCreating:
                            mTextView.setText(mFeedbackText);
                            break;
                        case DemoStepLocating:
                            mTextView.setText("searching for\n"+mAnchorId);
                            break;
                        case DemoStepSaving:
                            mTextView.setText("saving...");
                            break;
                        case DemoStepEnteringAnchorNumber:

                            break;
                    }

                    UpdateStatic();
                },
                500);
    }

    public void LocateButtonClicked(View source)
    {
        if (currentStep == DemoStep.DemoStepChoosing) {
            currentStep = DemoStep.DemoStepEnteringAnchorNumber;
            mTextView.setText("Enter an anchor number and press locate");
            EnableCorrectUIControls();
        } else {
            String inputVal = mAnchorNumInput.getText().toString();
            if (inputVal != "") {

                AnchorGetter anchorExchanger = new AnchorGetter(SharingAnchorsServiceUrl, this);
                anchorExchanger.execute(inputVal);

                currentStep = DemoStep.DemoStepLocating;
                EnableCorrectUIControls();
            }
        }
    }

    public void CreateButtonClicked(View source)
    {
        mTextView.setText("Scan your environment and place an anchor");
        mCloudSession = new CloudSpatialAnchorSession();
        configureSession();

        mCloudSession.addSessionUpdatedListener(args -> {
            if (currentStep == DemoStep.DemoStepCreating) {
                float progress = args.getStatus().getRecommendedForCreateProgress();
                if (progress >= 1.0) {
                    AnchorVisual visual = mAnchorVisuals.get("");
                    if (visual != null) {
                        //Transition to saving...
                        TransitionToSaving(visual);
                    } else {
                        mFeedbackText = "Tap somewhere to place an anchor.";
                    }
                } else {
                    mFeedbackText = "Progress is " + mDecimalFormat.format(progress * 100) + "%";
                }
            }
        });

        mCloudSession.start();
        currentStep = DemoStep.DemoStepCreating;
        EnableCorrectUIControls();
    }

    public void ExitDemoClicked(View v)
    {
        synchronized (renderLock) {
            destroySession();

            finish();
        }
    }

    @Override
    protected void HandleTap(HitResult hitResult, Plane plane, MotionEvent motionEvent)
    {
        if (currentStep == DemoStep.DemoStepCreating) {
            AnchorVisual visual = mAnchorVisuals.get("");
            if (visual == null) {
                createAnchor(hitResult);
            }
        }
    }

    @Override
    protected void CreateAnchorCustomCompletion(CloudSpatialAnchor anchor)
    {
        Log.d("ASADemo", "recording anchor with web service");
        AnchorPoster poster = new AnchorPoster(SharingAnchorsServiceUrl, this);
        String anchorId = anchor.getIdentifier();
        Log.d("ASADemo", "anchorId: "+anchorId);
        poster.execute(anchor.getIdentifier());
    }

    @Override
    protected void CreateAnchorExceptionCompletion(String message)
    {
        mTextView.setText(message);
        currentStep = DemoStep.DemoStepChoosing;
        mCloudSession.stop();
        mCloudSession = null;
        EnableCorrectUIControls();
    }

    private void TransitionToSaving(AnchorVisual visual)
    {
        Log.d("ASADemo:", "transition to saving");
        currentStep = DemoStep.DemoStepSaving;
        EnableCorrectUIControls();
        mHandler.post(() ->
        {
            Log.d("ASADemo", "creating anchor");
            visual.cloudAnchor = new CloudSpatialAnchor();
            visual.cloudAnchor.setLocalAnchor(visual.getLocalAnchor());
            Future createAnchorFuture = mCloudSession.createAnchorAsync(visual.cloudAnchor);
            CheckForCompletion(createAnchorFuture);
        });
    }

    public void AnchorPosted(String anchorNumber)
    {
        mTextView.setText("Anchor Number: " + anchorNumber);
        currentStep = DemoStep.DemoStepChoosing;
        mCloudSession.stop();
        mCloudSession = null;
        mAnchorVisuals.clear();
        EnableCorrectUIControls();
    }

    public void AnchorLookedUp(String anchorId)
    {
        Log.d("ASADemo", "anchor "+anchorId);
        mAnchorId = anchorId;
        mCloudSession = new CloudSpatialAnchorSession();
        configureSession();

        mCloudSession.addAnchorLocatedListener((AnchorLocatedEvent event) -> {
            runOnUiThread(() -> {
                switch (event.getStatus()) {
                    case AlreadyTracked:
                    case Located:
                        AnchorVisual foundVisual = new AnchorVisual();
                        foundVisual.cloudAnchor = event.getAnchor();
                        foundVisual.setLocalAnchor(foundVisual.cloudAnchor.getLocalAnchor());
                        foundVisual.anchorNode.setParent(arFragment.getArSceneView().getScene());
                        foundVisual.identifier = foundVisual.cloudAnchor.getIdentifier();
                        foundVisual.setColor(FoundColor);
                        foundVisual.render(arFragment);
                        mAnchorVisuals.put(foundVisual.identifier, foundVisual);
                        break;
                    case NotLocatedAnchorDoesNotExist:
                        break;
                }
            });
        });

        mCloudSession.addLocateAnchorsCompletedListener((LocateAnchorsCompletedEvent event) -> {
            mHandler.post(() ->
            {
                mTextView.setText("Anchor located!");
                currentStep = DemoStep.DemoStepChoosing;
                EnableCorrectUIControls();
            });
        });

        mCloudSession.start();
        AnchorLocateCriteria criteria = new AnchorLocateCriteria();
        criteria.setIdentifiers(new String[]{anchorId});
        mCloudSpatialAnchorWatcher = mCloudSession.createWatcher(criteria);
    }
}
