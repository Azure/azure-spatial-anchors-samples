// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef SPATIAL_SERVICES_APPLICATION_H
#define SPATIAL_SERVICES_APPLICATION_H

#include <unordered_map>
#include <iomanip>

#include "CameraRenderer.h"
#include "CubeRenderer.h"
#include "Util.h"

#include "AzureSpatialAnchorsNDK.h"

using namespace Microsoft::Azure::SpatialAnchors;

namespace AzureSpatialAnchors {

    class AzureSpatialAnchorsApplication {

    public:
        AzureSpatialAnchorsApplication(AAssetManager* assetManager);
        ~AzureSpatialAnchorsApplication();

        void OnResume(void* env, void* context, void* activity);
        void OnPause();
        void OnSurfaceCreated();
        void OnSurfaceChanged(int displayRotation, int width, int height);
        void OnDrawFrame();
        void OnTouched(float x, float y);
        void AdvanceDemo();
        void OnBasicButtonPress();
        void OnNearbyButtonPress();
        void OnCoarseRelocButtonPress();

        void UpdateGeoLocationPermission(bool isGranted);
        void UpdateWifiPermission(bool isGranted);
        void UpdateBluetoothPermission(bool isGranted);

        bool IsSpatialAnchorsAccountSet();
        bool ShowAdvanceButton() const { return m_showAdvanceButton; }
        const char* GetStatusText() const { return m_status.c_str(); }
        const char* GetButtonText() const { return m_buttonText.c_str(); }

     private:
        void AcquireArHitResult(float x, float y, ArHitResult **hitResult);
        void EnableAllowedSensors();

        enum class DemoMode : uint32_t {
            Basic = 0,
            Nearby,
            CoarseReloc
        };

        enum class DemoStep : uint32_t {
            CreateCloudAnchor = 0,
            DestroyCloudSession,
            LocateCloudAnchor,
            LocateNearbyAnchors,
            DeleteCloudAnchor,
            StopWatcher,
            StopSession
        };

        struct AnchorVisual
        {
            ArAnchor* localAnchor;
            std::shared_ptr<Microsoft::Azure::SpatialAnchors::CloudSpatialAnchor> cloudAnchor;
            std::string identifier;
            glm::vec3 color;
        };

        ArSession* m_arSession = nullptr;
        ArFrame* m_arFrame = nullptr;

        bool m_installRequested = false;
        int m_width = 1;
        int m_height = 1;
        int m_displayRotation = 0;

        std::string m_anchorID;
        std::string m_status;
        std::string m_buttonText;

        std::shared_ptr<Microsoft::Azure::SpatialAnchors::CloudSpatialAnchorSession> m_cloudSession;
        std::shared_ptr<Microsoft::Azure::SpatialAnchors::CloudSpatialAnchorWatcher> m_cloudSpatialAnchorWatcher;
        std::shared_ptr<Microsoft::Azure::SpatialAnchors::PlatformLocationProvider> m_locationProvider;
        std::unordered_map<std::string, AnchorVisual> m_anchorVisuals;

        AAssetManager* const m_assetManager;

        bool m_showAdvanceButton { false };
        bool m_enoughDataForSaving { false };
        bool m_progressOnSavingData { false };

        DemoMode m_demoMode { DemoMode::Basic };
        DemoStep m_demoStep { DemoStep::CreateCloudAnchor };
        bool m_ignoreTaps = false;
        uint32_t m_saveCount = 0;
        uint32_t m_numAnchorsFound = 0;

        bool m_haveGeoLocationPermission = false;
        bool m_haveWifiPermission = false;
        bool m_haveBluetoothPermission = false;

        Microsoft::Azure::SpatialAnchors::event_token m_anchorLocatedToken;
        Microsoft::Azure::SpatialAnchors::event_token m_locateAnchorsCompletedToken;
        Microsoft::Azure::SpatialAnchors::event_token m_sessionUpdatedToken;
        Microsoft::Azure::SpatialAnchors::event_token m_errorToken;
        Microsoft::Azure::SpatialAnchors::event_token m_onLogDebugToken;

        CameraRenderer m_cameraRenderer;
        CubeRenderer m_cubeRenderer;

        void StartCloudSession();
        void AddEventListeners();
        void DestroyCloudSession();
        void StopWatcher();
        void CreateCloudAnchor();
        void DeleteCloudAnchor();
        void QueryAnchor();
        void QueryNearbyAnchors(std::shared_ptr<CloudSpatialAnchor> const& sourceAnchor);
        void QueryAnchorsNearDevice();
    };
}

#endif
