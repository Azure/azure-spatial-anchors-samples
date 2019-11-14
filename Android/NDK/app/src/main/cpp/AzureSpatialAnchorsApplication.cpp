// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "AzureSpatialAnchorsApplication.h"
#include "AzureSpatialAnchorsNDK.hpp"
#include <string>
#include <vector>

namespace AzureSpatialAnchors {

const std::string SpatialAnchorsAccountId = "Set me";
const std::string SpatialAnchorsAccountKey = "Set me";

/// Whitelist of Bluetooth-LE beacons used to find anchors and improve the locatability
/// of existing anchors.
/// Add the UUIDs for your own Bluetooth beacons here to use them with Azure Spatial Anchors.
const std::vector<std::string> KnownBluetoothProximityUuids = {
    "61687109-905f-4436-91f8-e602f514c96d",
    "e1f54e02-1e23-44e0-9c3d-512eb56adec9",
    "01234567-8901-2345-6789-012345678903",
};

constexpr uint32_t NumberOfNearbyAnchors = 3;

const glm::vec3 ReadyColor = { 0, 0, 255 };
const glm::vec3 SavedColor = { 0, 255, 0 };
const glm::vec3 FailedColor = { 255, 0, 0 };
const glm::vec3 FoundColor = { 255, 255, 0 };
const glm::vec3 NoColor = { 0, 0, 0 };

AzureSpatialAnchorsApplication::AzureSpatialAnchorsApplication(AAssetManager* assetManager) : m_assetManager(assetManager) {
}

AzureSpatialAnchorsApplication::~AzureSpatialAnchorsApplication() {
    DestroyCloudSession();

    if (m_arSession != nullptr) {
        ArSession_destroy(m_arSession);
        ArFrame_destroy(m_arFrame);
    }
}

bool AzureSpatialAnchorsApplication::IsSpatialAnchorsAccountSet() {
    return SpatialAnchorsAccountId != "Set me" && SpatialAnchorsAccountKey != "Set me";
}

void AzureSpatialAnchorsApplication::OnPause() {
    LOGI("OnPause()");
    if (m_arSession != nullptr) {
        ArSession_pause(m_arSession);
    }
}

void AzureSpatialAnchorsApplication::OnResume(void* env, void* context, void* activity) {
    LOGI("OnResume()");

    if (m_arSession == nullptr) {
        ArInstallStatus arInstallStatus;
        // If install was not yet requested, that means that we are resuming the
        // activity first time because of explicit user interaction (such as
        // launching the application)
        bool userRequestedInstall = !m_installRequested;

        ArStatus error = ArCoreApk_requestInstall(env, activity, userRequestedInstall, &arInstallStatus);
        if (error != AR_SUCCESS) {
            LOGE("Failed to install ARCore");
            return;
        }

        switch (arInstallStatus) {
            case AR_INSTALL_STATUS_INSTALLED:
                break;
            case AR_INSTALL_STATUS_INSTALL_REQUESTED:
                m_installRequested = true;
                return;
        }

        error = ArSession_create(env, context, &m_arSession);
        if (error != AR_SUCCESS) {
            LOGE("Failed to create AR session");
            return;
        }

        ArFrame_create(m_arSession, &m_arFrame);
        CHECK(m_arFrame);

        ArSession_setDisplayGeometry(m_arSession, m_displayRotation, m_width, m_height);
    }

    const ArStatus status = ArSession_resume(m_arSession);
    CHECK(status == AR_SUCCESS);
}

void AzureSpatialAnchorsApplication::OnSurfaceCreated() {
    LOGI("OnSurfaceCreated()");

    m_cameraRenderer.Initialize(m_assetManager);
    m_cubeRenderer.Initialize(m_assetManager);
}

void AzureSpatialAnchorsApplication::OnSurfaceChanged(int displayRotation, int width, int height) {
    LOGI("OnSurfaceChanged(%d, %d)", width, height);
    glViewport(0, 0, width, height);
    m_displayRotation = displayRotation;
    m_width = width;
    m_height = height;
    if (m_arSession != nullptr) {
        ArSession_setDisplayGeometry(m_arSession, displayRotation, width, height);
    }
}

void AzureSpatialAnchorsApplication::OnDrawFrame() {
    // Render the scene.
    glClearColor(0.0f, 0.0f, 0.0f, 1.0f);
    glClear(GL_DEPTH_BUFFER_BIT | GL_COLOR_BUFFER_BIT);

    glEnable(GL_CULL_FACE);
    glEnable(GL_DEPTH_TEST);
    glEnable(GL_BLEND);
    glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

    if (m_arSession == nullptr) {
        return;
    }

    ArSession_setCameraTextureName(m_arSession, m_cameraRenderer.GetTextureId());

    // Update session to get current frame and render camera background.
    if (ArSession_update(m_arSession, m_arFrame) != AR_SUCCESS) {
        LOGE("AzureSpatialAnchorsApplication::OnDrawFrame ArSession_update error");
        return;
    }

    if (m_cloudSession != nullptr) {
        m_cloudSession->ProcessFrame(m_arFrame);
    }

    ArCamera* arCamera;
    ArFrame_acquireCamera(m_arSession, m_arFrame, &arCamera);

    glm::mat4 viewMat;
    glm::mat4 projectionMat;
    ArCamera_getViewMatrix(m_arSession, arCamera, glm::value_ptr(viewMat));
    ArCamera_getProjectionMatrix(m_arSession, arCamera, /*near=*/0.1f, /*far=*/100.f, glm::value_ptr(projectionMat));

    ArTrackingState cameraTrackingState;
    ArCamera_getTrackingState(m_arSession, arCamera, &cameraTrackingState);
    ArCamera_release(arCamera);

    m_cameraRenderer.Draw(m_arSession, m_arFrame);

    // If the camera isn't tracking don't bother rendering other objects.
    if (cameraTrackingState != AR_TRACKING_STATE_TRACKING) {
        return;
    }

    // Render cubes.
    for (const auto& visualPair : m_anchorVisuals) {
        const auto &visual = visualPair.second;

        if (visual.color == NoColor)
        {
            continue;
        }

        ArTrackingState trackingState = AR_TRACKING_STATE_STOPPED;
        ArAnchor_getTrackingState(m_arSession, visual.localAnchor, &trackingState);

        if (trackingState == AR_TRACKING_STATE_TRACKING) {
            // Render object only if the tracking state is AR_TRACKING_STATE_TRACKING.
            glm::mat4 modelMat(1.0f);
            Util::ScopedArPose anchorPose(m_arSession);
            ArAnchor_getPose(m_arSession, visual.localAnchor, anchorPose.AcquireArPose());
            ArPose_getMatrix(m_arSession, anchorPose.AcquireArPose(), glm::value_ptr(modelMat));

            glm::mat4 mvp_mat = projectionMat * viewMat * modelMat;
            m_cubeRenderer.Draw(mvp_mat, visual.color);
        }
    }

    // Create cloud anchor when there's enough data for saving and local anchor visual exists
    if (m_progressOnSavingData) {
        auto itr = m_anchorVisuals.find("");
        if (m_enoughDataForSaving && itr != m_anchorVisuals.end()) {
            m_progressOnSavingData = false;
            m_buttonText = "Saving to the cloud...";
            CreateCloudAnchor();
        }
    }
}

void AzureSpatialAnchorsApplication::OnTouched(float x, float y) {
    if (!m_progressOnSavingData) {
        return;
    }

    if (m_arFrame != nullptr && m_arSession != nullptr) {

        // Note that the application is responsible for releasing the anchor
        // pointer after using it. Call ArAnchor_release(anchor) to release.
        ArAnchor* anchor = nullptr;

        ArHitResult* hitResult = nullptr;
        AcquireArHitResult(x, y, &hitResult);

        if (hitResult) {
            if (ArHitResult_acquireNewAnchor(m_arSession, hitResult, &anchor) != AR_SUCCESS) {
                LOGE("AzureSpatialAnchorsApplication::OnTouched ArHitResult_acquireNewAnchor error");
                return;
            }

            ArHitResult_destroy(hitResult);
            hitResult = nullptr;
        } else {
            // Otherwise create the local anchor using the camera's current position
            Util::ScopedArPose cameraPose(m_arSession);
            ArCamera* arCamera;
            ArFrame_acquireCamera(m_arSession, m_arFrame, &arCamera);
            ArCamera_getDisplayOrientedPose(m_arSession, arCamera, cameraPose.AcquireArPose());
            ArCamera_release(arCamera);

            glm::mat4 cameraMat;
            ArPose_getMatrix(m_arSession, cameraPose.AcquireArPose(), glm::value_ptr(cameraMat));
            glm::mat4 translation = glm::mat4(1.0f);
            translation[3].z -= 1.0f; // Put it one meter in front of the camera
            glm::mat4 anchorMat = cameraMat * translation;

            float anchorPoseRaw[7] = { 0.0f, 0.0f, 0.0f, 1.0f, anchorMat[3].x, anchorMat[3].y, anchorMat[3].z };
            Util::ScopedArPose anchorPose(m_arSession, anchorPoseRaw);

            if (ArSession_acquireNewAnchor(m_arSession, anchorPose.AcquireArPose(), &anchor) != AR_SUCCESS) {
                LOGE("AzureSpatialAnchorsApplication::OnTouched ArSession_acquireNewAnchor error");
                return;
            }
        }

        ArTrackingState trackingState = AR_TRACKING_STATE_STOPPED;
        ArAnchor_getTrackingState(m_arSession, anchor, &trackingState);
        if (trackingState != AR_TRACKING_STATE_TRACKING) {
            ArAnchor_release(anchor);
            return;
        }

        AnchorVisual visual;
        visual.localAnchor = anchor;
        visual.identifier = ""; // Set local anchor id to empty.
        visual.color = ReadyColor;

        m_anchorVisuals[visual.identifier] = visual;

        m_buttonText = "Create Cloud Anchor (once at 100%)";
    }
}

void AzureSpatialAnchorsApplication::AcquireArHitResult(float x, float y, ArHitResult **hitResult) {
    // Return if a plane or an oriented point was hit.
    ArHitResultList* hitResultList = nullptr;
    ArHitResultList_create(m_arSession, &hitResultList);
    CHECK(hitResultList);
    ArFrame_hitTest(m_arSession, m_arFrame, x, y, hitResultList);

    int32_t size = 0;
    ArHitResultList_getSize(m_arSession, hitResultList, &size);

    for (int32_t i = 0; i < size; ++i) {
        ArHitResult* arHit = nullptr;
        ArHitResult_create(m_arSession, &arHit);
        ArHitResultList_getItem(m_arSession, hitResultList, i, arHit);

        if (arHit == nullptr) {
            LOGE("AzureSpatialAnchorsApplication::OnTouched ArHitResultList_getItem error");
            return;
        }

        ArTrackable *trackable = nullptr;
        ArHitResult_acquireTrackable(m_arSession, arHit, &trackable);

        ArTrackableType trackableType = AR_TRACKABLE_NOT_VALID;
        ArTrackable_getType(m_arSession, trackable, &trackableType);

        if (trackableType == AR_TRACKABLE_PLANE) {
            Util::ScopedArPose hitPose(m_arSession);
            ArHitResult_getHitPose(m_arSession, arHit, hitPose.AcquireArPose());
            int32_t inPolygon = 0;
            ArPlane* arPlane = ArAsPlane(trackable);
            ArPlane_isPoseInPolygon(m_arSession, arPlane, hitPose.AcquireArPose(), &inPolygon);

            if (inPolygon) {
                *hitResult = arHit;
                ArTrackable_release(trackable);
                break;
            }
        } else if (trackableType == AR_TRACKABLE_POINT) {
            ArPoint *arPoint = ArAsPoint(trackable);
            ArPointOrientationMode mode;
            ArPoint_getOrientationMode(m_arSession, arPoint, &mode);
            if (mode == AR_POINT_ORIENTATION_ESTIMATED_SURFACE_NORMAL) {
                *hitResult = arHit;
                ArTrackable_release(trackable);
                break;
            }
        }

        ArTrackable_release(trackable);
    }

    ArHitResultList_destroy(hitResultList);
    hitResultList = nullptr;
}

void AzureSpatialAnchorsApplication::OnBasicButtonPress() {
    m_showAdvanceButton = true;
    m_demoMode = DemoMode::Basic;
    AdvanceDemo();
}

void AzureSpatialAnchorsApplication::OnNearbyButtonPress() {
    m_showAdvanceButton = true;
    m_demoMode = DemoMode::Nearby;
    AdvanceDemo();
}

void AzureSpatialAnchorsApplication::OnCoarseRelocButtonPress() {
    m_showAdvanceButton = true;
    m_demoMode = DemoMode::CoarseReloc;
    AdvanceDemo();
}

void AzureSpatialAnchorsApplication::AdvanceDemo() {
    if (m_ignoreTaps) {
        return;
    }

    switch (m_demoStep) {
        case DemoStep::CreateCloudAnchor: {
            StartCloudSession();
            m_ignoreTaps = true;
            m_saveCount = 0;
            m_progressOnSavingData = true;
            m_enoughDataForSaving = false;
            m_buttonText = "Tap on screen to create an anchor";
            break;
        }
        case DemoStep::DestroyCloudSession: {
            DestroyCloudSession();
            m_demoStep = DemoStep::LocateCloudAnchor;
            m_buttonText = "Start to locate cloud anchor";
            break;
        }
        case DemoStep::LocateCloudAnchor: {
            m_status = "Keep moving!";
            StartCloudSession();
            if (m_demoMode == DemoMode::CoarseReloc) {
                m_buttonText = "Stop watcher";
                m_demoStep = DemoStep::StopWatcher;
                QueryAnchorsNearDevice();
            } else {
                m_ignoreTaps = true;
                m_buttonText = "Doing async locate...";
                QueryAnchor();
            }
            break;
        }
        case DemoStep::LocateNearbyAnchors: {
            m_ignoreTaps = true;
            m_buttonText = "Doing async nearby locate...";
            m_status = "Keep moving!";
            auto itr = m_anchorVisuals.find(m_anchorID);
            if (itr == m_anchorVisuals.end()) {
                m_buttonText = "Cannot locate nearby anchors, first anchor not found yet";
            }
            const auto &visual = itr->second;
            QueryNearbyAnchors(visual.cloudAnchor);
            break;
        }
        case DemoStep::DeleteCloudAnchor: {
            m_ignoreTaps = true;
            m_buttonText = "Deleting anchor...";
            DeleteCloudAnchor();
            break;
        }
        case DemoStep::StopWatcher: {
            StopWatcher();
            m_demoStep = DemoStep::StopSession;
            m_status = "";
            m_buttonText = "Stop session";
            break;
        }
        case DemoStep::StopSession: {
            DestroyCloudSession();
            m_buttonText = ""; // Return to the main menu
            m_demoStep = DemoStep::CreateCloudAnchor;
            m_showAdvanceButton = false;
            break;
        }
    }
}

void AzureSpatialAnchorsApplication::StartCloudSession() {
    m_cloudSession = std::make_shared<CloudSpatialAnchorSession>();
    m_cloudSession->Session(m_arSession);
    m_cloudSession->Configuration()->AccountId(SpatialAnchorsAccountId);
    m_cloudSession->Configuration()->AccountKey(SpatialAnchorsAccountKey);
    m_cloudSession->LogLevel(SessionLogLevel::All);

    if (m_demoMode == DemoMode::CoarseReloc) {
        m_locationProvider = std::make_shared<PlatformLocationProvider>();
        m_locationProvider->Sensors()->KnownBeaconProximityUuids(KnownBluetoothProximityUuids);
        EnableAllowedSensors();
        m_cloudSession->LocationProvider(m_locationProvider);
    }

    AddEventListeners();

    m_cloudSession->Start();
}

void AzureSpatialAnchorsApplication::AddEventListeners() {
    if (m_cloudSession == nullptr) {
        return;
    }

    m_errorToken = m_cloudSession->Error([](auto&&, auto&& args) {
        auto errorCode = args->ErrorCode();
        auto errorMessage = args->ErrorMessage();
        LOGE("%s, error code: %d", errorMessage.c_str(), errorCode);
    });

    m_onLogDebugToken = m_cloudSession->OnLogDebug([](auto&&, auto&& args) {
        auto message = args->Message();
        LOGD("%s", message.c_str());
    });

    m_sessionUpdatedToken = m_cloudSession->SessionUpdated([this](auto&&, auto&& args) {
        auto status = args->Status();
        m_enoughDataForSaving = status->RecommendedForCreateProgress() >= 1.0f;
        if (m_progressOnSavingData) {
            std::ostringstream str;
            str << "Progress is " << std::fixed << std::setw(2) << std::setprecision(0)
                << (status->RecommendedForCreateProgress() * 100) << "%";
            m_status = str.str();
        }
    });

    m_anchorLocatedToken = m_cloudSession->AnchorLocated([this](auto &&, auto &&args) {
        switch (args->Status()) {
            case LocateAnchorStatus::AlreadyTracked:
                break;
            case LocateAnchorStatus::Located: {
                AnchorVisual foundVisual;
                foundVisual.cloudAnchor = args->Anchor();
                foundVisual.localAnchor = foundVisual.cloudAnchor->LocalAnchor();
                foundVisual.identifier = foundVisual.cloudAnchor->Identifier();
                foundVisual.color = FoundColor;
                m_anchorVisuals[foundVisual.identifier] = foundVisual;

                if (m_demoMode == DemoMode::CoarseReloc) {
                    m_status = std::to_string(m_numAnchorsFound++) + " anchor(s) found";
                }
            }
                break;
            case LocateAnchorStatus::NotLocated:
                m_buttonText = "Not located";
                break;
            case LocateAnchorStatus::NotLocatedAnchorDoesNotExist:
                m_buttonText = "Does not exist";
                break;
        }
    });

    m_locateAnchorsCompletedToken = m_cloudSession->LocateAnchorsCompleted(
            [this](auto &&, auto &&args) {
                m_ignoreTaps = false;
                if (m_demoMode == DemoMode::CoarseReloc) {
                    // Ignored
                } else if (m_demoMode == DemoMode::Nearby && m_demoStep == DemoStep::LocateCloudAnchor) {
                    m_buttonText = "Locate nearby anchors";
                    m_demoStep = DemoStep::LocateNearbyAnchors;
                } else {
                    m_buttonText = "Delete found anchor(s)";
                    m_status = "";
                    m_demoStep = DemoStep::DeleteCloudAnchor;
                    StopWatcher();
                }
            });
}

void AzureSpatialAnchorsApplication::DestroyCloudSession() {
    if (m_cloudSession != nullptr) {
        m_cloudSession->SessionUpdated(m_sessionUpdatedToken);
        m_cloudSession->OnLogDebug(m_onLogDebugToken);
        m_cloudSession->Error(m_errorToken);
        m_cloudSession->AnchorLocated(m_anchorLocatedToken);
        m_cloudSession->LocateAnchorsCompleted(m_locateAnchorsCompletedToken);
        m_cloudSession->Stop();
        m_cloudSession.reset();
    }

    if (m_locationProvider != nullptr) {
        m_locationProvider.reset();
    }

    StopWatcher();

    for (const auto& visualPair : m_anchorVisuals) {
        const auto &visual = visualPair.second;
        ArAnchor_release(visual.localAnchor);
    }
    m_anchorVisuals.clear();
}

void AzureSpatialAnchorsApplication::QueryAnchor() {
    // Cannot run more than one watcher concurrently
    StopWatcher();

    auto criteria = std::make_shared<AnchorLocateCriteria>();
    criteria->Identifiers({m_anchorID});
    m_cloudSpatialAnchorWatcher = m_cloudSession->CreateWatcher(criteria);
}

void AzureSpatialAnchorsApplication::QueryNearbyAnchors(std::shared_ptr<CloudSpatialAnchor> const& sourceAnchor) {
    // Cannot run more than one watcher concurrently
    StopWatcher();

    auto criteria = std::make_shared<AnchorLocateCriteria>();
    auto nearbyCriteria = std::make_shared<NearAnchorCriteria>();
    nearbyCriteria->DistanceInMeters(10);
    nearbyCriteria->SourceAnchor(sourceAnchor);
    criteria->NearAnchor(nearbyCriteria);

    m_cloudSpatialAnchorWatcher = m_cloudSession->CreateWatcher(criteria);
}

void AzureSpatialAnchorsApplication::QueryAnchorsNearDevice() {
    // Cannot run more than one watcher concurrently
    StopWatcher();

    m_numAnchorsFound = 0;

    auto nearDevice = std::make_shared<NearDeviceCriteria>();
    nearDevice->DistanceInMeters(8.0f);
    nearDevice->MaxResultCount(25);

    auto criteria = std::make_shared<AnchorLocateCriteria>();
    criteria->NearDevice(nearDevice);
    m_cloudSpatialAnchorWatcher = m_cloudSession->CreateWatcher(criteria);
}

void AzureSpatialAnchorsApplication::StopWatcher() {
    if (m_cloudSpatialAnchorWatcher != nullptr) {
        m_cloudSpatialAnchorWatcher->Stop();
        m_cloudSpatialAnchorWatcher.reset();
    }
}

void AzureSpatialAnchorsApplication::CreateCloudAnchor() {
    auto itr = m_anchorVisuals.find("");
    if (itr == m_anchorVisuals.end()) {
        return;
    }
    auto &visual = itr->second;
    visual.cloudAnchor = std::make_shared<CloudSpatialAnchor>();
    visual.cloudAnchor->LocalAnchor(visual.localAnchor);

    // In this sample app we delete the cloud anchor explicitly, but you can also set it to expire automatically
    std::chrono::system_clock::time_point now = std::chrono::system_clock::now();
    std::chrono::system_clock::time_point oneWeekFromNow = now + std::chrono::hours(7 * 24);
    const int64_t oneWeekFromNowUnixEpochTimeMs = std::chrono::duration_cast<std::chrono::milliseconds>(oneWeekFromNow.time_since_epoch()).count();
    visual.cloudAnchor->Expiration(oneWeekFromNowUnixEpochTimeMs);

    m_cloudSession->CreateAnchorAsync(visual.cloudAnchor, [this](Status status) {
        auto itr = m_anchorVisuals.find("");
        auto &visual = itr->second;
        if (status != Status::OK) {
            visual.color = FailedColor;
            m_buttonText = "Save Failed: " + std::to_string(static_cast<uint32_t>(status));
            return;
        }
        m_saveCount++;
        visual.identifier = visual.cloudAnchor->Identifier();
        visual.color = SavedColor;
        m_anchorVisuals[visual.identifier] = visual;
        m_anchorVisuals.erase("");

        m_anchorID = visual.cloudAnchor->Identifier();

        if (m_demoMode != DemoMode::Nearby || m_saveCount == NumberOfNearbyAnchors) {
            m_ignoreTaps = false;
            m_demoStep = DemoStep::DestroyCloudSession;
            m_buttonText = "Destroy Cloud Session";
            m_status = "";
        } else { // If in Nearby mode, continue to create nearby anchors
            m_progressOnSavingData = true;
            m_buttonText = "Tap on screen to create next anchor";
        }
    });
}

void AzureSpatialAnchorsApplication::DeleteCloudAnchor() {
    for (auto &toDeleteVisualPair : m_anchorVisuals) {
        auto &visual = toDeleteVisualPair.second;
        std::string id = visual.identifier;
        visual.color = NoColor;
        m_cloudSession->DeleteAnchorAsync(visual.cloudAnchor,
                [this, id](Status status) {
                    m_saveCount--;
                    if (status != Status::OK) {
                        m_buttonText = "Delete Failed: " + std::to_string(static_cast<uint32_t>(status));
                        auto itr = m_anchorVisuals.find(id);
                        if (itr != m_anchorVisuals.end()) {
                            itr->second.color = FailedColor;
                        }
                    }

                    if (m_demoMode == DemoMode::Basic || m_saveCount == 0) {
                        DestroyCloudSession();
                        m_ignoreTaps = false;
                        m_buttonText = "";
                        m_demoStep = DemoStep::CreateCloudAnchor;
                        m_showAdvanceButton = false;
                    }
        });

    }
}

void AzureSpatialAnchorsApplication::UpdateGeoLocationPermission(bool isGranted) {
    m_haveGeoLocationPermission = isGranted;
    EnableAllowedSensors();
}

void AzureSpatialAnchorsApplication::UpdateWifiPermission(bool isGranted) {
    m_haveWifiPermission =  isGranted;
    EnableAllowedSensors();
}

void AzureSpatialAnchorsApplication::UpdateBluetoothPermission(bool isGranted) {
    m_haveBluetoothPermission = isGranted;
    EnableAllowedSensors();
}

void AzureSpatialAnchorsApplication::EnableAllowedSensors() {
    if (m_locationProvider == nullptr) {
        return;
    }

    const std::shared_ptr<SensorCapabilities>& sensors = m_locationProvider->Sensors();
    sensors->GeoLocationEnabled(m_haveGeoLocationPermission);
    sensors->WifiEnabled(m_haveWifiPermission);
    sensors->BluetoothEnabled(m_haveBluetoothPermission);
}

}
