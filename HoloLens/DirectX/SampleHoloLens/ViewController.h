// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#pragma once

#include <AzureSpatialAnchorsWinRT.h>
#include "AnchorExchanger.h"
#include "winrt/SampleHoloLens.h"

// Comment out the next line to use the look for nearby demo.
#define BASIC_DEMO
// Uncomment to use anchor exchange service (only applies to basic demo)
//#define USE_ANCHOR_EXCHANGE

#ifdef USE_ANCHOR_EXCHANGE
// Set this to the url for the service created in the 'SharingService' sample
const std::wstring AnchorExchangeURL = L"";
#endif

namespace SampleHoloLens
{
    class ViewController
    {
    public:
        ViewController();
        ~ViewController();

        // Controller update called once per frame.
        void Update(winrt::Windows::Perception::Spatial::SpatialCoordinateSystem const& currentCoordinateSystem);

        void InputReceived(winrt::Windows::UI::Input::Spatial::SpatialPointerPose const& pose);

        const std::wstring& GetTitleText() { return m_titleText; }
        const std::wstring& GetStatusText() { return m_statusText; }
        const std::wstring& GetLogText() { return m_logText; }

        winrt::Windows::Foundation::Collections::IObservableMap<winrt::hstring, winrt::SampleHoloLens::AnchorVisual> GetFoundAnchors()
        {
            return m_foundAnchors;
        }

    private:
        enum class DemoStep : uint32_t {
            CreateFactory = 0,  ///< a factory object will be created
            CreateSession,      ///< a session object will be created
            ConfigSession,      ///< the session will be configured
            StartSession,       ///< the session will be started
#ifdef BASIC_DEMO
            CreateLocalAnchor,  ///< the session will create a local anchor
            CreateCloudAnchor,  ///< the session will create an unsaved cloud anchor
            SetAnchorExpiration, ///< the session will set the expiration date of the cloud anchor
            SaveCloudAnchor,    ///< the session will save the cloud anchor
            StopSession,        ///< the session will stop
            DestroySession,     ///< the session will be destroyed
            CreateForQuery,     ///< a session will be created to query for an anchor
            StartForQueryAndCreateWatcher, ///< the session will be started to query for an anchor
            DeleteFoundAnchor,  ///< the session will delete the query
#else // NEARBY_DEMO
            CreateNearbyOne,      ///< the session will create and save a cloud anchor
            CreateNearbyTwo,      ///< the session will create and save a second cloud anchor
            CreateNearbyThree,    ///< the session will create and save a third cloud anchor
            CreateForQuery,       ///< a session will be created to query for anchors
            LookForAnchor,        ///< the session will run the query to first the last saved anchor
            LookForNearbyAnchors, ///< the session will run the query for nearby anchors, the other two
#endif
            StopSessionForQuery
        };

        void AddEventListeners();
        void RemoveEventListeners();
        bool SanityCheckAccessInformation();
        winrt::fire_and_forget SaveAnchor(DemoStep errorStep);
        winrt::fire_and_forget DeleteAnchor(DemoStep errorStep);

        winrt::Windows::Perception::Spatial::SpatialCoordinateSystem m_coordinateSystem{ nullptr };

        winrt::Windows::Perception::Spatial::SpatialAnchor m_localAnchor{ nullptr };

        winrt::Windows::Foundation::Collections::IObservableMap<winrt::hstring, winrt::SampleHoloLens::AnchorVisual> m_foundAnchors;

        bool m_enoughDataForSaving{ false };
        bool m_asyncOpInProgress{ false };
        bool m_sessionStarted{ false };

        winrt::hstring m_targetId;
        uint32_t m_locateCount{ 0 };

        DemoStep m_step{ DemoStep::CreateFactory };

        std::wstring m_titleText = L"airtap to begin";
        std::wstring m_statusText;
        std::wstring m_logText;

        winrt::Microsoft::Azure::SpatialAnchors::SpatialAnchorsFactory m_sscfactory{ nullptr };
        winrt::Microsoft::Azure::SpatialAnchors::CloudSpatialAnchorSession m_cloudSession{ nullptr };
        winrt::Microsoft::Azure::SpatialAnchors::CloudSpatialAnchor m_cloudAnchor{ nullptr };
        winrt::Microsoft::Azure::SpatialAnchors::CloudSpatialAnchor m_foundAnchor{ nullptr };

        winrt::event_revoker<winrt::Microsoft::Azure::SpatialAnchors::ICloudSpatialAnchorSession> m_anchorLocatedToken;
        winrt::event_revoker<winrt::Microsoft::Azure::SpatialAnchors::ICloudSpatialAnchorSession> m_locateAnchorsCompletedToken;
        winrt::event_revoker<winrt::Microsoft::Azure::SpatialAnchors::ICloudSpatialAnchorSession> m_sessionUpdatedToken;
        winrt::event_revoker<winrt::Microsoft::Azure::SpatialAnchors::ICloudSpatialAnchorSession> m_errorToken;
        winrt::event_revoker<winrt::Microsoft::Azure::SpatialAnchors::ICloudSpatialAnchorSession> m_onLogDebugToken;

        std::mutex m_mutex;
#ifdef USE_ANCHOR_EXCHANGE
        AnchorExchanger m_anchorExchange{ AnchorExchangeURL };
#endif

        winrt::fire_and_forget RequestCapabilityAccess(const winrt::hstring &capabilityName);
    };
}
