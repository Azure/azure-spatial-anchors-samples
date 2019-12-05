// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.os.Bundle;
import android.support.annotation.NonNull;
import android.support.annotation.Nullable;
import android.support.v4.app.Fragment;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;

import com.microsoft.azure.spatialanchors.AnchorLocateCriteria;
import com.microsoft.azure.spatialanchors.AnchorLocatedEvent;
import com.microsoft.azure.spatialanchors.CloudSpatialAnchorWatcher;
import com.microsoft.azure.spatialanchors.LocateAnchorStatus;
import com.microsoft.azure.spatialanchors.LocateAnchorsCompletedEvent;
import com.microsoft.azure.spatialanchors.NearDeviceCriteria;

public class WatcherFragment extends Fragment {
    private AzureSpatialAnchorsManager cloudAnchorManager;
    private AnchorDiscoveryListener listener;
    private CloudSpatialAnchorWatcher watcher;
    private Button stopWatcherButton;

    public void setCloudAnchorManager(AzureSpatialAnchorsManager cloudAnchorManager) {
        this.cloudAnchorManager = cloudAnchorManager;
    }

    public void setListener(AnchorDiscoveryListener listener) {
        this.listener = listener;
    }

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, @Nullable Bundle savedInstanceState) {
        return inflater.inflate(R.layout.coarse_reloc_watcher, container, false);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);

        stopWatcherButton = (Button)view.findViewById(R.id.stop_watcher);
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

        cloudAnchorManager.addAnchorLocatedListener(this::onAnchorLocated);
        cloudAnchorManager.addLocateAnchorsCompletedListener(this::onLocateAnchorsCompleted);
        startWatcher();
        stopWatcherButton.setOnClickListener(this::onStopWatcherClicked);
    }

    @Override
    public void onStop() {
        stopWatcherButton.setOnClickListener(null);
        stopWatcher();
        cloudAnchorManager.removeLocateAnchorsCompletedListener(null);
        cloudAnchorManager.removeAnchorLocatedListener(this::onAnchorLocated);

        super.onStop();
    }

    private void startWatcher() {
        if (cloudAnchorManager.isLocating()) {
            FragmentHelper.backToPreviousFragment(getActivity());
        }

        AnchorLocateCriteria criteria = new AnchorLocateCriteria();
        NearDeviceCriteria nearDevice = new NearDeviceCriteria();
        nearDevice.setDistanceInMeters(8.0f);
        nearDevice.setMaxResultCount(25);
        criteria.setNearDevice(nearDevice);

        watcher = cloudAnchorManager.startLocating(criteria);
    }

    private void stopWatcher() {
        if (watcher != null) {
            watcher.stop();
            watcher = null;
        }
    }

    private void onAnchorLocated(AnchorLocatedEvent anchorLocatedEvent) {
        if (listener == null) {
            return;
        }

        if (anchorLocatedEvent.getStatus() == LocateAnchorStatus.Located) {
            MainThreadContext.runOnUiThread(() ->
                listener.onAnchorDiscovered(anchorLocatedEvent.getAnchor()));
        }
    }

    private void onLocateAnchorsCompleted(LocateAnchorsCompletedEvent locateAnchorsCompletedEvent) {
        FragmentHelper.backToPreviousFragment(getActivity());
    }

    private void onStopWatcherClicked(View view) {
        FragmentHelper.backToPreviousFragment(getActivity());
    }
}
