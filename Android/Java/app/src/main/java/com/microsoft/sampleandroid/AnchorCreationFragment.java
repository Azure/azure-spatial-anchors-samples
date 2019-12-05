// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.app.Activity;
import android.content.Context;
import android.os.Bundle;
import android.support.annotation.NonNull;
import android.support.annotation.Nullable;
import android.support.v4.app.Fragment;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.ProgressBar;

import com.microsoft.azure.spatialanchors.CloudSpatialAnchor;
import com.microsoft.azure.spatialanchors.CloudSpatialException;
import com.microsoft.azure.spatialanchors.SessionUpdatedEvent;

public class AnchorCreationFragment extends Fragment {
    private AzureSpatialAnchorsManager cloudAnchorManager;
    private AnchorCreationListener listener;
    private boolean isCreatingAnchor = false;
    private AnchorVisual placedVisual;

    private Button createAnchorButton;
    private ProgressBar requiredScanProgress;
    private ProgressBar recommendedScanProgress;

    public void setCloudAnchorManager(AzureSpatialAnchorsManager cloudAnchorManager) {
        this.cloudAnchorManager = cloudAnchorManager;
    }

    public void setPlacement(AnchorVisual placedVisual) {
        this.placedVisual = placedVisual;
    }

    public void setListener(AnchorCreationListener listener) {
        this.listener = listener;
    }

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, @Nullable Bundle savedInstanceState) {
        return inflater.inflate(R.layout.coarse_reloc_anchor_creation, container, false);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        createAnchorButton = (Button)view.findViewById(R.id.create_anchor);
        requiredScanProgress = (ProgressBar)view.findViewById(R.id.required_scan_progress);
        recommendedScanProgress = (ProgressBar)view.findViewById(R.id.recommended_scan_progress);
    }

    @Override
    public void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (cloudAnchorManager == null) {
            FragmentHelper.backToPreviousFragment(getActivity());
        }
    }

    @Override
    public void onStart() {
        super.onStart();

        createAnchorButton.setEnabled(false);
        createAnchorButton.setOnClickListener(this::onCreateAnchorClicked);
        cloudAnchorManager.addSessionUpdatedListener(this::onSessionUpdated);
    }

    @Override
    public void onStop() {
        if (cloudAnchorManager != null) {
            cloudAnchorManager.removeSessionUpdatedListener(this::onSessionUpdated);
        }

        if (placedVisual != null) {
            placedVisual.destroy();
            placedVisual = null;
        }

        super.onStop();
    }

    private boolean canCreateAnchor() {
        return !isCreatingAnchor && placedVisual != null;
    }

    private void onSessionUpdated(SessionUpdatedEvent sessionUpdatedEvent) {
        float requiredForCreateProgress = sessionUpdatedEvent.getStatus().getReadyForCreateProgress();
        float recommendedForCreateProgress = sessionUpdatedEvent.getStatus().getRecommendedForCreateProgress();
        MainThreadContext.runOnUiThread(() -> {
            requiredScanProgress.setProgress((int)(100 * requiredForCreateProgress));
            recommendedScanProgress.setProgress((int)(100 * recommendedForCreateProgress));
            boolean allowCreation = canCreateAnchor() && requiredForCreateProgress >= 1.0f;
            createAnchorButton.setEnabled(allowCreation);
        });
    }

    private void onCreateAnchorClicked(View view) {
        if (!canCreateAnchor()) {
            return;
        }

        createAnchorButton.setEnabled(false);
        CloudSpatialAnchor cloudAnchor = new CloudSpatialAnchor();
        cloudAnchor.setLocalAnchor(placedVisual.getLocalAnchor());
        cloudAnchor.getAppProperties().put("Shape", placedVisual.getShape().toString());

        isCreatingAnchor = true;
        cloudAnchorManager
            .createAnchorAsync(cloudAnchor)
            .whenComplete((anchor, thrown) -> {
                MainThreadContext.runOnUiThread(() -> {
                    isCreatingAnchor = false;
                    if (placedVisual == null) {
                        return;
                    }

                    if (thrown != null) {
                        if (listener != null) {
                            String errorMessage = getErrorMessageFromThrowable(thrown);
                            listener.onAnchorCreationFailed(placedVisual, errorMessage);
                        }
                        return;
                    }

                    AnchorVisual createdAnchor = placedVisual;
                    placedVisual = null;
                    if (listener != null) {
                        listener.onAnchorCreated(createdAnchor);
                    }
                });
            });
    }

    private String getErrorMessageFromThrowable(Throwable thrown) {
        Throwable originalException = thrown;
        while (originalException != null && !(originalException instanceof CloudSpatialException)) {
            originalException = originalException.getCause();
        }
        if (originalException != null) {
            CloudSpatialException azureSpatialAnchorsException = (CloudSpatialException) originalException;
            return azureSpatialAnchorsException.getErrorCode().toString();
        } else {
            return thrown.toString();
        }
    }
}
