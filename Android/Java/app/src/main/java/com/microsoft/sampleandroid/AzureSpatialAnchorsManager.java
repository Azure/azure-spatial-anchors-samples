// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.util.Log;

import com.google.ar.core.Frame;
import com.google.ar.core.Session;
import com.microsoft.azure.spatialanchors.AnchorLocateCriteria;
import com.microsoft.azure.spatialanchors.AnchorLocatedListener;
import com.microsoft.azure.spatialanchors.CloudSpatialAnchor;
import com.microsoft.azure.spatialanchors.CloudSpatialAnchorSession;
import com.microsoft.azure.spatialanchors.CloudSpatialAnchorWatcher;
import com.microsoft.azure.spatialanchors.PlatformLocationProvider;
import com.microsoft.azure.spatialanchors.LocateAnchorsCompletedListener;
import com.microsoft.azure.spatialanchors.OnLogDebugEvent;
import com.microsoft.azure.spatialanchors.SessionErrorEvent;
import com.microsoft.azure.spatialanchors.SessionLogLevel;
import com.microsoft.azure.spatialanchors.SessionUpdatedListener;

import java.util.List;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;

class AzureSpatialAnchorsManager {
    // Set this string to the account ID provided for the Azure Spatial Service resource.
    public static final String SpatialAnchorsAccountId = "Set me";

    // Set this string to the account key provided for the Azure Spatial Service resource.
    public static final String SpatialAnchorsAccountKey = "Set me";

    // Log message tag
    private static final String TAG = "ASACloud";

    private final ExecutorService executorService = Executors.newFixedThreadPool(2);

    private boolean running = false;

    private final CloudSpatialAnchorSession spatialAnchorsSession;

    public AzureSpatialAnchorsManager(Session arCoreSession) {
        if (arCoreSession == null) {
            throw new IllegalArgumentException("The arCoreSession may not be null.");
        }

        spatialAnchorsSession = new CloudSpatialAnchorSession();
        spatialAnchorsSession.getConfiguration().setAccountId(SpatialAnchorsAccountId);
        spatialAnchorsSession.getConfiguration().setAccountKey(SpatialAnchorsAccountKey);
        spatialAnchorsSession.setSession(arCoreSession);
        spatialAnchorsSession.setLogLevel(SessionLogLevel.All);

        spatialAnchorsSession.addOnLogDebugListener(this::onLogDebugListener);
        spatialAnchorsSession.addErrorListener(this::onErrorListener);
    }

    //region Listener Handling

    public void addSessionUpdatedListener(SessionUpdatedListener listener) {
        this.spatialAnchorsSession.addSessionUpdatedListener(listener);
    }

    public void removeSessionUpdatedListener(SessionUpdatedListener listener) {
        this.spatialAnchorsSession.removeSessionUpdatedListener(listener);
    }

    public void addAnchorLocatedListener(AnchorLocatedListener listener) {
        this.spatialAnchorsSession.addAnchorLocatedListener(listener);
    }

    public void removeAnchorLocatedListener(AnchorLocatedListener listener) {
        this.spatialAnchorsSession.removeAnchorLocatedListener(listener);
    }

    public void addLocateAnchorsCompletedListener(LocateAnchorsCompletedListener listener) {
        this.spatialAnchorsSession.addLocateAnchorsCompletedListener(listener);
    }

    public void removeLocateAnchorsCompletedListener(LocateAnchorsCompletedListener listener) {
        this.spatialAnchorsSession.removeLocateAnchorsCompletedListener(listener);
    }

    //endregion

    public void setLocationProvider(PlatformLocationProvider locationProvider) {
        spatialAnchorsSession.setLocationProvider(locationProvider);
    }

    public CompletableFuture<CloudSpatialAnchor> createAnchorAsync(CloudSpatialAnchor anchor) {
        //noinspection unchecked,unchecked
        return this.toEmptyCompletableFuture(spatialAnchorsSession.createAnchorAsync(anchor))
                .thenApply((ignore) -> anchor);
    }

    public CompletableFuture deleteAnchorAsync(CloudSpatialAnchor anchor) {
        return this.toEmptyCompletableFuture(spatialAnchorsSession.deleteAnchorAsync(anchor));
    }

    public boolean isRunning() {
        return this.running;
    }

    public void reset() {
        stopLocating();
        spatialAnchorsSession.reset();
    }

    public void start() {
        spatialAnchorsSession.start();
        this.running = true;
    }

    public CloudSpatialAnchorWatcher startLocating(AnchorLocateCriteria criteria) {
        // Only 1 active watcher at a time is permitted.
        stopLocating();

        return spatialAnchorsSession.createWatcher(criteria);
    }

    public boolean isLocating() {
        return !spatialAnchorsSession.getActiveWatchers().isEmpty();
    }

    public void stopLocating() {
        List<CloudSpatialAnchorWatcher> watchers = spatialAnchorsSession.getActiveWatchers();

        if (watchers.isEmpty()) {
            return;
        }

        // Only 1 watcher is at a time is currently permitted.
        CloudSpatialAnchorWatcher watcher = watchers.get(0);

        watcher.stop();
    }

    public void stop() {
        spatialAnchorsSession.stop();
        stopLocating();
        this.running = false;
    }

    public void update(Frame frame) {
        spatialAnchorsSession.processFrame(frame);
    }

    private <T> CompletableFuture<T> toCompletableFuture(Future<T> future) {
        return CompletableFuture.supplyAsync(() -> {
            try {
                return future.get();
            } catch (InterruptedException|ExecutionException e) {
                e.printStackTrace();
                throw new RuntimeException(e);
            }
        }, executorService);
    }

    private CompletableFuture toEmptyCompletableFuture(Future future) {
        return CompletableFuture.runAsync(() -> {
            try {
                future.get();
            } catch (InterruptedException|ExecutionException e) {
                e.printStackTrace();
                throw new RuntimeException(e);
            }
        }, executorService);
    }

    private void onErrorListener(SessionErrorEvent event) {
        Log.e(TAG, event.getErrorMessage());
    }

    private void onLogDebugListener(OnLogDebugEvent args) {
        Log.d(TAG, args.getMessage());
    }
}
