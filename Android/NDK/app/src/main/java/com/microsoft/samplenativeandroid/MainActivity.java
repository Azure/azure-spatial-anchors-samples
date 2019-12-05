// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

package com.microsoft.samplenativeandroid;

import android.Manifest;
import android.app.Activity;
import android.content.pm.PackageManager;
import android.opengl.GLSurfaceView;
import android.os.Bundle;
import android.os.Handler;
import android.support.v4.app.ActivityCompat;
import android.support.v4.content.ContextCompat;
import android.support.v7.app.AppCompatActivity;
import android.view.MotionEvent;
import android.view.View;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;
import com.microsoft.CloudServices;
import javax.microedition.khronos.egl.EGLConfig;
import javax.microedition.khronos.opengles.GL10;

public class MainActivity extends AppCompatActivity implements GLSurfaceView.Renderer {
    public static final String TAG = "AzureSpatialAnchors";

    private GLSurfaceView mSurfaceView;
    private TextView mTextView;
    private Button mDemoAdvanceButton;
    private Button mBasicButton;
    private Button mNearbyButton;
    private Button mCoarseRelocButton;

    private boolean mViewportChanged = false;
    private int mViewportWidth;
    private int mViewportHeight;

    private Handler mStatusUpdateHandler;

    private static final int CAMERA_PERMISSION_CODE = 0;
    private static final String CAMERA_PERMISSION = Manifest.permission.CAMERA;

    // Check to see we have the necessary permissions for this app
    public static boolean hasCameraPermission(Activity activity) {
        return ContextCompat.checkSelfPermission(activity, CAMERA_PERMISSION)
                == PackageManager.PERMISSION_GRANTED;
    }

    // Check to see we have the necessary permissions for this app, and ask for them if we don't.
    public static void requestCameraPermission(Activity activity) {
        if (!hasCameraPermission(activity)) {
            ActivityCompat.requestPermissions(
                    activity, new String[]{CAMERA_PERMISSION}, CAMERA_PERMISSION_CODE);
        }
    }

    @Override
    public void onRequestPermissionsResult(int requestCode, String[] permissions, int[] results) {
        if (requestCode == CAMERA_PERMISSION_CODE) {
            if (!hasCameraPermission(this)) {
                Toast.makeText(
                        this,
                        "Camera permission is needed to run this application",
                        Toast.LENGTH_LONG)
                        .show();
                finish();
            }
        } else {
            SensorPermissionsHelper.PermissionsResult outcome =
                    SensorPermissionsHelper.onRequestPermissionsResult(this, requestCode);
            if (outcome == SensorPermissionsHelper.PermissionsResult.Denied) {
                Toast.makeText(
                        this,
                        "Location permission is needed to run this demo",
                        Toast.LENGTH_LONG)
                        .show();
                finish();
            }
        }
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        ConfigureSurfaceView();
        ConfigureUI();

        JniInterface.onCreate(getAssets());
        CloudServices.initialize(this);

        mStatusUpdateHandler = new Handler();

        CheckSpatialAnchorsAccount();
    }

    private void ConfigureSurfaceView() {
        mSurfaceView = findViewById(R.id.surfaceview);
        mSurfaceView.setPreserveEGLContextOnPause(true);
        mSurfaceView.setEGLContextClientVersion(2);
        mSurfaceView.setRenderer(this);
        mSurfaceView.setOnTouchListener((view, event) -> {
            if (event.getAction() == MotionEvent.ACTION_UP) {
                mSurfaceView.queueEvent(
                        () -> JniInterface.onTouched(event.getX(), event.getY()));
            }
            return true;
        });
    }

    private void ConfigureUI() {
        mTextView = findViewById(R.id.textView);
        mBasicButton = findViewById(R.id.basicDemo);
        mNearbyButton = findViewById(R.id.nearbyDemo);
        mCoarseRelocButton = findViewById(R.id.coarseRelocDemo);
        mDemoAdvanceButton = findViewById(R.id.demoAdvance);
        mBasicButton.setOnClickListener((View v) -> BasicButtonPress());
        mNearbyButton.setOnClickListener((View v) -> NearbyButtonPress());
        mCoarseRelocButton.setOnClickListener((View v) -> CoarseRelocButtonPress());
        mDemoAdvanceButton.setOnClickListener((View v) -> AdvanceDemo());
    }

    private void CheckSpatialAnchorsAccount() {
        if (!JniInterface.isSpatialAnchorsAccountSet()) {
            Toast.makeText(this, "Set SpatialAnchorsAccountId and SpatialAnchorsAccountKey in AzureSpatialAnchorsApplication.cpp", Toast.LENGTH_LONG)
                    .show();

            finish();
        }
    }

    private void BasicButtonPress() {
        JniInterface.onBasicButtonPress();
    }

    private void NearbyButtonPress() {
        JniInterface.onNearbyButtonPress();
    }

    private void CoarseRelocButtonPress() {
        JniInterface.onCoarseRelocButtonPress();
        SensorPermissionsHelper.requestMissingPermissions(this);
    }

    private void AdvanceDemo() {
        // Can modify session state or destroy it. Synchronized to avoid racing onDrawFrame.
        synchronized (this) {
            JniInterface.advanceDemo();
        }
    }

    @Override
    protected void onResume() {
        super.onResume();

        if (!hasCameraPermission(this))
        {
            requestCameraPermission(this);
            return;
        }

        JniInterface.onResume(getApplicationContext(), this);
        mSurfaceView.onResume();

        updateStatus();
    }

    private void updateStatus() {
        mStatusUpdateHandler.postDelayed(() -> {
            String text = JniInterface.getStatusText();
            mTextView.setText(text);
            text = JniInterface.getButtonText();
            mDemoAdvanceButton.setText(text);
            boolean showAdvance = JniInterface.showAdvanceButton();
            mDemoAdvanceButton.setVisibility(showAdvance ? View.VISIBLE : View.GONE);
            mBasicButton.setVisibility(showAdvance ? View.GONE : View.VISIBLE);
            mNearbyButton.setVisibility(showAdvance ? View.GONE : View.VISIBLE);
            mCoarseRelocButton.setVisibility(showAdvance ? View.GONE : View.VISIBLE);
            mTextView.setVisibility(showAdvance ? View.VISIBLE : View.GONE);

            updateStatus();
        }, 500);
    }

    @Override
    protected void onPause() {
        super.onPause();
        mSurfaceView.onPause();
        JniInterface.onPause();
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();

        // Synchronized to avoid racing onDrawFrame.
        synchronized (this) {
            JniInterface.onDestroy();
        }
    }

    @Override
    public void onSurfaceCreated(GL10 gl, EGLConfig config) {
        JniInterface.onSurfaceCreated();
    }

    @Override
    public void onSurfaceChanged(GL10 gl, int width, int height) {
        int displayRotation = getWindowManager().getDefaultDisplay().getRotation();
        JniInterface.onSurfaceChanged(displayRotation, width, height);
    }

    @Override
    public void onDrawFrame(GL10 gl) {
        // Synchronized to avoid racing onDestroy.
        synchronized (this) {
            JniInterface.onDrawFrame();
        }
    }
}