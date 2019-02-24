// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.Manifest;
import android.app.Activity;
import android.content.pm.PackageManager;
import android.os.Bundle;
import android.support.v4.app.ActivityCompat;
import android.support.v4.content.ContextCompat;
import android.util.Log;
import android.view.MotionEvent;
import android.view.View;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import com.google.ar.core.Anchor;
import com.google.ar.core.ArCoreApk;
import com.google.ar.core.HitResult;
import com.google.ar.core.Plane;
import com.google.ar.core.exceptions.UnavailableUserDeclinedInstallationException;

import android.app.ActivityManager;
import android.content.Context;
import android.os.Build;
import android.os.Build.VERSION_CODES;
import android.support.v7.app.AppCompatActivity;

import com.google.ar.sceneform.ArSceneView;
import com.google.ar.sceneform.Scene;
import com.google.ar.sceneform.rendering.Color;
import com.google.ar.sceneform.rendering.Material;
import com.google.ar.sceneform.rendering.MaterialFactory;
import com.google.ar.sceneform.ux.ArFragment;

import com.microsoft.azure.spatialanchors.AnchorLocateCriteria;
import com.microsoft.azure.spatialanchors.AnchorLocatedEvent;
import com.microsoft.azure.spatialanchors.CloudSpatialAnchor;
import com.microsoft.azure.spatialanchors.CloudSpatialAnchorSession;
import com.microsoft.azure.spatialanchors.CloudSpatialAnchorWatcher;
import com.microsoft.azure.spatialanchors.CloudSpatialException;
import com.microsoft.azure.spatialanchors.LocateAnchorsCompletedEvent;
import com.microsoft.azure.spatialanchors.NearAnchorCriteria;
import com.microsoft.azure.spatialanchors.SessionLogLevel;

import java.text.DecimalFormat;
import java.util.Calendar;
import java.util.Date;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.Future;

public class AzureSpatialAnchorsActivity extends AppCompatActivity
{
    public static final String TAG = "AzureSpatialAnchors";
    private static final double MIN_OPENGL_VERSION = 3.0;

    // Set this string to the account ID provided for the Azure Spatial Service resource.
    private static final String SpatialAnchorsAccountId = "Set me";

    // Set this string to the account key provided for the Azure Spatial Service resource.
    private static final String SpatialAnchorsAccountKey = "Set me";

    private static final int NumberOfNearbyAnchors = 3;
    private static Material ReadyColor;
    private static Material SavedColor;
    private static Material FailedColor;
    protected static Material FoundColor;

    protected ArFragment arFragment;
    private ArSceneView sceneView;

    private static final int CAMERA_PERMISSION_CODE = 0;
    private static final String CAMERA_PERMISSION = Manifest.permission.CAMERA;
    protected final ConcurrentHashMap<String, AnchorVisual> mAnchorVisuals = new ConcurrentHashMap<>();

    protected CloudSpatialAnchorSession mCloudSession;
    protected CloudSpatialAnchorWatcher mCloudSpatialAnchorWatcher;

    private Object progressLock = new Object();

    private TextView mTextView;
    private Button mDemoAdvanceButton;
    private Button mNearbyButton;
    private boolean mEnoughDataForSaving;
    private String mAnchorID;
    private String buttonText;
    private DemoStep currentDemoStep = DemoStep.DemoStepCreateSession;
    private String progressText;
    private int mSaveCount = 0;
    private boolean mBasicDemo = true;
    protected Object renderLock = new Object();


    // Check to see we have the necessary permissions for this app
    public static boolean hasCameraPermission(Activity activity)
    {
        return ContextCompat.checkSelfPermission(activity, CAMERA_PERMISSION)
                == PackageManager.PERMISSION_GRANTED;
    }

    // Check to see we have the necessary permissions for this app, and ask for them if we don't.
    public static void requestCameraPermission(Activity activity)
    {
        if (!hasCameraPermission(activity)) {
            ActivityCompat.requestPermissions(
                activity, new String[]{CAMERA_PERMISSION}, CAMERA_PERMISSION_CODE);
        }
    }

    @Override
    public void onRequestPermissionsResult(int requestCode, String[] permissions, int[] results)
    {
        if (!hasCameraPermission(this)) {
            Toast.makeText(this, "Camera permission is needed to run this application", Toast.LENGTH_LONG)
                    .show();

            finish();
        }
    }

    public static boolean checkIsSupportedDeviceOrFinish(final Activity activity) {
        if (Build.VERSION.SDK_INT < VERSION_CODES.N) {
            Log.e(TAG, "Sceneform requires Android N or later");
            Toast.makeText(activity, "Sceneform requires Android N or later", Toast.LENGTH_LONG).show();
            activity.finish();
            return false;
        }
        String openGlVersionString =
                ((ActivityManager) activity.getSystemService(Context.ACTIVITY_SERVICE))
                        .getDeviceConfigurationInfo()
                        .getGlEsVersion();
        if (Double.parseDouble(openGlVersionString) < MIN_OPENGL_VERSION) {
            Log.e(TAG, "Sceneform requires OpenGL ES 3.0 later");
            Toast.makeText(activity, "Sceneform requires OpenGL ES 3.0 or later", Toast.LENGTH_LONG)
                    .show();
            activity.finish();
            return false;
        }
        return true;
    }

    @Override
    @SuppressWarnings({"AndroidApiChecker", "FutureReturnValueIgnored"})
    protected void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);
        SetContentView();
        requestCameraPermission(this);

        arFragment = (ArFragment) getSupportFragmentManager().findFragmentById(R.id.ux_fragment);
        sceneView = arFragment.getArSceneView();

        Scene scene = sceneView.getScene();

        scene.addOnUpdateListener(frameTime -> {
            if (mCloudSession != null) {
                mCloudSession.processFrame(sceneView.getArFrame());
            }
        });

        arFragment.setOnTapArPlaneListener(this::HandleTap);

        ConfigureUI();

        if (!checkIsSupportedDeviceOrFinish(this)) {
            return;
        }

        MaterialFactory.makeOpaqueWithColor(this, new Color(android.graphics.Color.RED))
                .thenAccept(
                        material -> {
                            FailedColor = material;
                        });

        MaterialFactory.makeOpaqueWithColor(this, new Color(android.graphics.Color.GREEN))
                .thenAccept(
                        material -> {
                            SavedColor = material;
                        });

        MaterialFactory.makeOpaqueWithColor(this, new Color(android.graphics.Color.YELLOW))
                .thenAccept(
                        material -> {
                            ReadyColor = material;
                            FoundColor = material;
                        });
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        destroySession();
    }

    protected void SetContentView()
    {
        setContentView(R.layout.activity_anchors);
    }

    protected void ConfigureUI()
    {
        mTextView = findViewById(R.id.textView);
        mDemoAdvanceButton = findViewById(R.id.demoAdvance);
        mNearbyButton = findViewById(R.id.nearbyDemo);
        buttonText = "Basic Demo";

        mDemoAdvanceButton.setOnClickListener((View v) -> AdvanceDemo());
        mNearbyButton.setOnClickListener((View v) -> NearbyButtonPress());
    }

    private void NearbyButtonPress()
    {
        mBasicDemo = false;
        AdvanceDemo();
    }

    public void ExitDemoClicked(View v)
    {
        synchronized (renderLock) {
            destroySession();
            finish();
        }
    }

    @Override
    protected void onResume()
    {
        super.onResume();
        try {
            switch (ArCoreApk.getInstance().requestInstall(this, true)) {

                case INSTALLED:
                    break;
                case INSTALL_REQUESTED:
                    break;
            }
        } catch (UnavailableUserDeclinedInstallationException e) {
            ;
        } catch (Exception e) {
            ;
        }

        if (SpatialAnchorsAccountId == "Set me" || SpatialAnchorsAccountKey == "Set me") {
            Toast.makeText(this, "\"Set SpatialAnchorsAccountId and SpatialAnchorsAccountKey in AzureSpatialAnchorsActivity.java\"", Toast.LENGTH_LONG)
                    .show();

            finish();
            return;
        }

        UpdateStatic();
    }

    protected void UpdateStatic()
    {
        new android.os.Handler().postDelayed(() -> {
                    mTextView.setText(progressText);
                    mDemoAdvanceButton.setText(buttonText);
                    UpdateStatic();
                },
                500);
    }

    @Override
    public void onPause()
    {
        super.onPause();
    }

    protected void HandleTap(HitResult hitResult, Plane plane, MotionEvent motionEvent) {
        if (currentDemoStep == DemoStep.DemoStepCreateLocalAnchor) {
            createAnchor(hitResult);
        }
    }

    public Anchor createAnchor(HitResult hitResult) {

        AnchorVisual visual = new AnchorVisual();
        visual.setLocalAnchor(hitResult.createAnchor());
        visual.identifier = "";
        visual.setColor(ReadyColor);
        visual.render(arFragment);
        mAnchorVisuals.put(visual.identifier, visual);

        buttonText = ("Create Cloud Anchor");
        currentDemoStep = DemoStep.DemoStepCreateCloudAnchor;

        return visual.getLocalAnchor();
    }

    protected void CreateAnchorCustomCompletion(CloudSpatialAnchor anchor)
    {
        currentDemoStep = DemoStep.DemoStepStopSession;
        progressText = "";
        buttonText = "Stop Session";
        if (!mBasicDemo) {
            if (mSaveCount < NumberOfNearbyAnchors) {
                currentDemoStep = DemoStep.DemoStepCreateLocalAnchor;
                buttonText = "Tap on grid to create next Local Anchor";
            }
        }

        mAnchorID = anchor.getIdentifier();
    }

    protected void CreateAnchorExceptionCompletion(String message)
    {
        progressText = message;
    }

    protected void CheckForCompletion(Future createAnchorFuture) {
        new android.os.Handler().postDelayed(() -> {
                if (createAnchorFuture.isDone()) {
                    try {
                        createAnchorFuture.get();
                        mSaveCount++;
                        AnchorVisual visual = mAnchorVisuals.get("");

                        Log.d("ASADemo:", "created anchor: "+visual.cloudAnchor.getIdentifier());
                        visual.identifier = visual.cloudAnchor.getIdentifier();
                        visual.setColor(SavedColor);
                        CloudSpatialAnchor csa = visual.cloudAnchor;
                        CreateAnchorCustomCompletion(csa);
                        mAnchorVisuals.put(visual.identifier, visual);
                        mAnchorVisuals.remove("");
                    } catch (InterruptedException e) {
                        e.printStackTrace();
                        CreateAnchorExceptionCompletion(e.getMessage());
                        AnchorVisual visual = mAnchorVisuals.get("");
                        visual.setColor(FailedColor);
                    } catch (ExecutionException e) {
                        e.printStackTrace();
                        Throwable t = e.getCause();
                        String exceptionMessage;
                        if (t instanceof CloudSpatialException) {
                            exceptionMessage = (((CloudSpatialException) t).getErrorCode().toString());
                        } else {
                            exceptionMessage = e.toString();
                        }
                        CreateAnchorExceptionCompletion(exceptionMessage);
                        AnchorVisual visual = mAnchorVisuals.get("");
                        visual.setColor(FailedColor);
                    }
                } else {
                    CheckForCompletion(createAnchorFuture);
                }
            },
            500);
    }

    private void AdvanceDemo() {
        switch (currentDemoStep) {
            case DemoStepCreateSession:
                mNearbyButton.setVisibility(View.GONE);
                mTextView.setVisibility(View.VISIBLE);
                mSaveCount = 0;
                mCloudSession = new CloudSpatialAnchorSession();

                buttonText = "Config Session";
                currentDemoStep = DemoStep.DemoStepConfigSession;

                if (mBasicDemo) {
                    break;
                }
            case DemoStepConfigSession:
                configureSession();

                mCloudSession.addSessionUpdatedListener(args -> {
                    float progress = args.getStatus().getRecommendedForCreateProgress();
                    mEnoughDataForSaving = progress >= 1.0;
                    synchronized (progressLock) {
                        if (currentDemoStep != DemoStep.DemoStepSavingCloudAnchor) {
                            DecimalFormat decimalFormat = new DecimalFormat("00");
                            progressText = "Progress is " + decimalFormat.format(progress * 100) + "%";
                        }
                    }
                });

                buttonText = ("Start Session");
                currentDemoStep = DemoStep.DemoStepStartSession;

                if (mBasicDemo) {
                    break;
                }
            case DemoStepStartSession:
                mCloudSession.start();


                buttonText = ("Tap on grid to create a Local Anchor");
                currentDemoStep = DemoStep.DemoStepCreateLocalAnchor;

                break;
            case DemoStepCreateLocalAnchor: {
                AnchorVisual visual = mAnchorVisuals.get("");
                if (visual == null) {
                    return;
                }
            }
            case DemoStepCreateCloudAnchor: {
                AnchorVisual visual = mAnchorVisuals.get("");
                visual.cloudAnchor = new CloudSpatialAnchor();
                visual.cloudAnchor.setLocalAnchor(visual.getLocalAnchor());

                buttonText = ("Set Cloud Anchor expiration date");
                currentDemoStep = DemoStep.DemoStepSetCloudAnchorExpirationDate;

                if (mBasicDemo) {
                    break;
                }
            }
            case DemoStepSetCloudAnchorExpirationDate: {
                // In this sample app we delete the cloud anchor explicitly, but you can also set it to expire automatically
                Date now = new Date();
                Calendar cal = Calendar.getInstance();
                cal.setTime(now);
                cal.add(Calendar.DATE, 7);
                Date oneWeekFromNow = cal.getTime();
                mAnchorVisuals.get("").cloudAnchor.setExpiration(oneWeekFromNow);

                buttonText = ("Save Cloud Anchor (once at 100%)");
                currentDemoStep = DemoStep.DemoStepSaveCloudAnchor;

                if (mBasicDemo) {
                    break;
                }
            }
            case DemoStepSaveCloudAnchor:
                if (!mEnoughDataForSaving) {
                    return;
                }

                Future createAnchorFuture = mCloudSession.createAnchorAsync(mAnchorVisuals.get("").cloudAnchor);
                synchronized (progressLock) {
                    buttonText = ("Wait...");
                    progressText = ("Saving cloud anchor...");
                    currentDemoStep = DemoStep.DemoStepSavingCloudAnchor;
                }
                CheckForCompletion(createAnchorFuture);

                break;
            case DemoStepStopSession:
                mCloudSession.stop();
                mCloudSession = null;

                buttonText = ("destroy Session");
                currentDemoStep = DemoStep.DemoStepDestroySession;

                if (mBasicDemo) {
                    break;
                }
            case DemoStepDestroySession:
                destroySession();

                buttonText = ("Create for query");
                currentDemoStep = DemoStep.DemoStepCreateSessionForQuery;

                if (mBasicDemo) {
                    break;
                }
            case DemoStepCreateSessionForQuery:
                mCloudSession = new CloudSpatialAnchorSession();
                configureSession();
                progressText = "Keep moving!";

                mCloudSession.addAnchorLocatedListener((AnchorLocatedEvent event) -> {
                    runOnUI(() -> {
                        switch (event.getStatus()) {
                            case AlreadyTracked:
                                break;
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
                                progressText = "Anchor does not exist";
                                break;
                        }
                    });
                });

                mCloudSession.addLocateAnchorsCompletedListener((LocateAnchorsCompletedEvent event) -> {
                    progressText = "Locate completed!";
                    if (!mBasicDemo && currentDemoStep == DemoStep.DemoStepLookForAnchor) {
                        buttonText = "Look for nearby anchors";
                        currentDemoStep = DemoStep.DemoStepLookForNearbyAnchors;
                    } else {
                        buttonText = "Delete found anchor(s)";
                        currentDemoStep = DemoStep.DemoStepDeleteFoundAnchor;
                        stopWatcher();
                    }
                });

                buttonText = "Start for query";
                currentDemoStep = DemoStep.DemoStepStartSessionForQuery;

                if (mBasicDemo) {
                    break;
                }
            case DemoStepStartSessionForQuery:
                mCloudSession.start();
                buttonText = "Look for anchor";
                currentDemoStep = DemoStep.DemoStepLookForAnchor;

                break;
            case DemoStepLookForAnchor: {
                    AnchorLocateCriteria criteria = new AnchorLocateCriteria();
                    criteria.setIdentifiers(new String[]{mAnchorID});
                    // Cannot run more than one watcher concurrently
                    stopWatcher();
                    mCloudSpatialAnchorWatcher = mCloudSession.createWatcher(criteria);
                    buttonText = "Wait...";
                    progressText = "Doing async locate...";
                }
                break;
            case DemoStepLookForNearbyAnchors:
                if (mAnchorVisuals.size() < 1) {
                    progressText = "Cannot locate nearby, first anchor not found yet";
                }
                else {
                    AnchorLocateCriteria nearbyLocatecriteria = new AnchorLocateCriteria();
                    NearAnchorCriteria nearAnchorCriteria = new NearAnchorCriteria();
                    nearAnchorCriteria.setDistanceInMeters(50);
                    nearAnchorCriteria.setSourceAnchor(mAnchorVisuals.get(mAnchorID).cloudAnchor);
                    nearbyLocatecriteria.setNearAnchor(nearAnchorCriteria);
                    // Cannot run more than one watcher concurrently
                    stopWatcher();
                    mCloudSpatialAnchorWatcher = mCloudSession.createWatcher(nearbyLocatecriteria);
                    progressText = "Doing async nearby locate...";
                }
                break;
            case DemoStepDeleteFoundAnchor:
                for (AnchorVisual toDeleteVisual : mAnchorVisuals.values()) {
                    mCloudSession.deleteAnchorAsync(toDeleteVisual.cloudAnchor);
                }

                buttonText = "Stop Session for query";
                currentDemoStep = DemoStep.DemoStepStopSessionForQuery;

                if (!mBasicDemo) {
                    break;
                }
            case DemoStepStopSessionForQuery:
                destroySession();

                buttonText = "Basic Demo";
                currentDemoStep = DemoStep.DemoStepCreateSession;

                mBasicDemo = true;
                mNearbyButton.setVisibility(View.VISIBLE);
                mTextView.setVisibility(View.GONE);

                break;
        }
    }

    protected void configureSession() {
        mCloudSession.setSession(sceneView.getSession());
        mCloudSession.addOnLogDebugListener(args -> Log.d("ASACloud", args.getMessage()));
        mCloudSession.setLogLevel(SessionLogLevel.All);
        mCloudSession.addErrorListener(args-> Log.d("ASAError: ", args.getErrorMessage()));
        mCloudSession.getConfiguration().setAccountId(SpatialAnchorsAccountId);
        mCloudSession.getConfiguration().setAccountKey(SpatialAnchorsAccountKey);
    }

    protected void destroySession() {
        if (mCloudSession != null) {
            mCloudSession.stop();
            mCloudSession = null;
        }

        stopWatcher();

        for (AnchorVisual visual : mAnchorVisuals.values()) {
            visual.destroy();
        }

        mAnchorVisuals.clear();
    }

    protected void stopWatcher() {
        if (mCloudSpatialAnchorWatcher != null) {
            mCloudSpatialAnchorWatcher.stop();
            mCloudSpatialAnchorWatcher = null;
        }
    }

    private void runOnUI(Runnable runnable) {
        this.mTextView.post(runnable);
    }

    enum DemoStep {
        DemoStepCreateSession,          ///< a session object will be created
        DemoStepConfigSession,          ///< the session will be configured
        DemoStepStartSession,           ///< the session will be started
        DemoStepCreateLocalAnchor,      ///< the session will create a local anchor
        DemoStepCreateCloudAnchor,      ///< the session will create an unsaved cloud anchor
        DemoStepSetCloudAnchorExpirationDate, ///< the session will set the expiration date of the cloud anchor
        DemoStepSaveCloudAnchor,        ///< the session will save the cloud anchor
        DemoStepSavingCloudAnchor,      ///< the session is in the process of saving the cloud anchor
        DemoStepStopSession,            ///< the session will be stopped
        DemoStepDestroySession,         ///< the session will be destroyed
        DemoStepCreateSessionForQuery,  ///< a session will be created to query for an anchor
        DemoStepStartSessionForQuery,   ///< the session will be started to query for an anchor
        DemoStepLookForAnchor,          ///< the session will run the query
        DemoStepLookForNearbyAnchors,   ///< the session will run a query for nearby anchors
        DemoStepDeleteFoundAnchor,      ///< the session will delete the query
        DemoStepStopSessionForQuery     ///< the session will be stopped
    }
}