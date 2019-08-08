// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#include "pch.h"
#include "ViewController.h"
#include "Content/AnchorVisual.h"

#ifdef BASIC_DEMO
#define AFTER_LOCATE_STEP DemoStep::DeleteFoundAnchor
#else
#ifdef USE_ANCHOR_EXCHANGE
#undef USE_ANCHOR_EXCHANGE
#endif
#define AFTER_LOCATE_STEP DemoStep::LookForNearbyAnchors
#endif



using namespace SampleHoloLens;
using namespace concurrency;
using namespace Microsoft::WRL;
using namespace std::placeholders;
using namespace std::literals::chrono_literals;
using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Foundation::Metadata;
using namespace winrt::Windows::Foundation::Numerics;
using namespace winrt::Windows::Perception::Spatial;
using namespace winrt::Windows::UI::Input::Spatial;
using namespace winrt::Windows::Security::Authorization::AppCapabilityAccess;
using namespace winrt::Windows::System::Threading;
using namespace winrt::Microsoft::Azure::SpatialAnchors;

constexpr float4 c_DefaultColor = { 0.f, 0.f, 1.f, 1.f };
constexpr float4 c_SavedColor = { 0.f, 1.f, 0.f, 1.f };
constexpr float4 c_FoundColor = { 1.f, 1.f, 0.f, 1.f };
constexpr float4 c_FailedColor = { 1.f, 0.f, 0.f, 1.f };

// Set this string to the Spatial Anchors account ID provided for the Azure Spatial Service resource.
const std::wstring SpatialAnchorsAccountId = L"Set me";

// Set this string to the Spatial Anchors account key provided for the Azure Spatial Service resource.
const std::wstring SpatialAnchorsAccountKey = L"Set me";


std::wstring FeedbackToString(SessionUserFeedback userFeedback)
{
    std::wstring result = L"";
    if ((userFeedback & SessionUserFeedback::NotEnoughMotion) == SessionUserFeedback::NotEnoughMotion)
    {
        result += L"not enough motion";
    }
    if ((userFeedback & SessionUserFeedback::MotionTooQuick) == SessionUserFeedback::MotionTooQuick)
    {
        result += L"motion too quick";
    }
    if ((userFeedback & SessionUserFeedback::NotEnoughFeatures) == SessionUserFeedback::NotEnoughFeatures)
    {
        result += L"not enough features";
    }
    return result;
}

std::wstring FormatPercent(float value)
{
    value = value < 1000 ? value : 999;
    wchar_t buf[5] = { 0 };
    swprintf_s(buf, L"%00.0f%%", value);
    return buf;
}

std::wstring StatusToString(SessionStatus status) {
    return L"feedback:" + FeedbackToString(status.UserFeedback()) +
        L" - create ready = " + FormatPercent(status.ReadyForCreateProgress() * 100.f) +
        L", recommend = " + FormatPercent(status.RecommendedForCreateProgress() * 100.f);
}

ViewController::ViewController()
{
#ifdef USE_ANCHOR_EXCHANGE
    m_anchorExchange.WatchKeys();
#endif
    m_foundAnchors = winrt::single_threaded_observable_map<winrt::hstring, winrt::SampleHoloLens::AnchorVisual>();

    if (!SanityCheckAccessInformation())
    {
        m_titleText = L"Set Azure Spatial Anchors access information in ViewController.cpp";
#ifdef USE_ANCHOR_EXCHANGE
        m_titleText += L"\nand Set AnchorExchangeURL in ViewController.h";
#endif
    }
}

ViewController::~ViewController()
{
    RemoveEventListeners();
}

void ViewController::Update(SpatialCoordinateSystem const& currentCoordinateSystem)
{
    m_coordinateSystem = currentCoordinateSystem;
}

void ViewController::AddEventListeners()
{
    if (m_cloudSession == nullptr)
    {
        return;
    }

    m_anchorLocatedToken = m_cloudSession.AnchorLocated(winrt::auto_revoke, [this](auto&&, auto&& args)
    {
        switch (args.Status())
        {
        case LocateAnchorStatus::Located:
        {
            auto lock = std::unique_lock<std::mutex>(m_mutex);

            auto anchorVisual = winrt::make<winrt::SampleHoloLens::implementation::AnchorVisual>();
            anchorVisual.Id(args.Anchor().Identifier());
            anchorVisual.Color(c_FoundColor);
            anchorVisual.Anchor(args.Anchor().LocalAnchor());
            m_foundAnchors.Insert(args.Anchor().Identifier(), anchorVisual);
            m_titleText = L"anchor found : " + std::wstring(args.Anchor().Identifier());
#ifdef BASIC_DEMO
            m_titleText += L"\nairtap to delete anchor";
#else
            if (m_step == DemoStep::StopSessionForQuery)
            {
                m_titleText += L"\nairtap to delete anchors";
            }
            else
            {
                m_titleText += L"\nairtap to find nearby anchors";
            }
#endif

            if (args.Anchor().Identifier() == m_targetId)
            {
                m_foundAnchor = args.Anchor();
                m_localAnchor = m_foundAnchor.LocalAnchor();
                m_step = AFTER_LOCATE_STEP;
            }

            m_asyncOpInProgress = false;
        }
        break;
        case LocateAnchorStatus::AlreadyTracked:
            m_titleText = L"anchor already tracked";
            break;
        case LocateAnchorStatus::NotLocated:
            m_titleText = L"not located";
            break;
        case LocateAnchorStatus::NotLocatedAnchorDoesNotExist:
            m_titleText = L"not located : anchor does not exist";
            break;
        }
    });

    m_locateAnchorsCompletedToken = m_cloudSession.LocateAnchorsCompleted(winrt::auto_revoke, [this](auto&&, auto&& args)
    {
        if (m_step >= static_cast<DemoStep>(static_cast<uint32_t>(DemoStep::CreateForQuery) + 1))
        {
            m_locateCount += 1;
            m_logText = L"locate from completed ", std::to_wstring(m_locateCount);
        }
    });

    m_sessionUpdatedToken = m_cloudSession.SessionUpdated(winrt::auto_revoke, [this](auto&&, auto&& args)
    {
        auto status = args.Status();
        m_enoughDataForSaving = status.RecommendedForCreateProgress() >= 1.0f;
        m_statusText = StatusToString(status);
    });

    m_errorToken = m_cloudSession.Error(winrt::auto_revoke, [this](auto&&, auto&& args)
    {
        m_logText = args.ErrorMessage();
    });

    m_onLogDebugToken = m_cloudSession.OnLogDebug(winrt::auto_revoke, [this](auto&&, auto&& args)
    {
        m_logText = args.Message();
    });
}

void ViewController::RemoveEventListeners()
{
    if (m_cloudSession != nullptr)
    {
        m_anchorLocatedToken.revoke();
        m_locateAnchorsCompletedToken.revoke();
        m_sessionUpdatedToken.revoke();
        m_errorToken.revoke();
        m_onLogDebugToken.revoke();
    }
}

bool SampleHoloLens::ViewController::SanityCheckAccessInformation()
{
#ifdef USE_ANCHOR_EXCHANGE
    return !(SpatialAnchorsAccountId == L"Set me" || SpatialAnchorsAccountKey == L"Set me" || AnchorExchangeURL == L"");
#else
    return !(SpatialAnchorsAccountId == L"Set me" || SpatialAnchorsAccountKey == L"Set me");
#endif
}

winrt::fire_and_forget ViewController::SaveAnchor(DemoStep errorStep)
{
    try
    {
        m_titleText = L"cloud anchor being saved ...";
        m_asyncOpInProgress = true;
        co_await m_cloudSession.CreateAnchorAsync(m_cloudAnchor);
        m_asyncOpInProgress = false;
        auto anchorVisual = winrt::make<winrt::SampleHoloLens::implementation::AnchorVisual>();
        anchorVisual.Id(m_cloudAnchor.Identifier());
        anchorVisual.Color(c_SavedColor);
        anchorVisual.Anchor(m_cloudAnchor.LocalAnchor());
        m_foundAnchors.Insert(m_cloudAnchor.Identifier(), anchorVisual);
        // Stop showing the unsaved anchor.
        m_foundAnchors.Remove(L"");
        m_targetId = m_cloudAnchor.Identifier();
        m_titleText = L"cloud anchor saved with ID " + m_targetId;
#ifdef BASIC_DEMO
        m_titleText += L"\nairtap to stop session";
#else
        if (m_step == DemoStep::CreateForQuery)
        {
            m_titleText += L"\nairtap to stop session";
        }
        else
        {
            m_titleText += L"\nairtap to make another";
        }
#endif
#ifdef USE_ANCHOR_EXCHANGE
        m_anchorExchange.StoreAnchorKey(m_targetId);
#endif
    }
    catch (winrt::hresult_error e)
    {
        m_asyncOpInProgress = false;
        std::wstringstream ss;
        ss << L"creation failed : " << std::hex << e.code();
        ss << L" - " << e.message().c_str();
        m_titleText = ss.str();
        auto anchorVisual = winrt::make<winrt::SampleHoloLens::implementation::AnchorVisual>();
        anchorVisual.Id(L"");
        anchorVisual.Color(c_FailedColor);
        anchorVisual.Anchor(m_cloudAnchor.LocalAnchor());
        m_foundAnchors.Insert(L"", anchorVisual);
        m_step = errorStep;
    }
}

winrt::fire_and_forget ViewController::DeleteAnchor(DemoStep errorStep)
{
#ifndef USE_ANCHOR_EXCHANGE
    try
    {
        m_titleText = L"deleting found anchor ...";
        m_asyncOpInProgress = true;
        co_await m_cloudSession.DeleteAnchorAsync(m_foundAnchor);
        m_asyncOpInProgress = false;
        m_foundAnchors.Remove(m_foundAnchor.Identifier());
        m_foundAnchors.Remove(L"");
        m_titleText = L"cloud anchor deleted\nairtap to stop session";
    }
    catch (winrt::hresult_error e)
    {
        m_asyncOpInProgress = false;
        std::wstringstream ss;
        ss << L"deletion failed : " << std::hex << e.code();
        ss << L" - " << e.message().c_str();
        m_titleText = ss.str();
        auto anchorVisual = winrt::make<winrt::SampleHoloLens::implementation::AnchorVisual>();
        anchorVisual.Id(m_foundAnchor.Identifier());
        anchorVisual.Color(c_FailedColor);
        anchorVisual.Anchor(m_foundAnchor.LocalAnchor());
        m_foundAnchors.Insert(m_foundAnchor.Identifier(), anchorVisual);
        m_step = errorStep;
    }
#endif
    co_return;
}

void ViewController::InputReceived(SpatialPointerPose const& pose)
{
    if (!SanityCheckAccessInformation() || m_asyncOpInProgress)
    {
        return;
    }

    switch (m_step)
    {
        case DemoStep::CreateFactory:
        {
            winrt::com_ptr<::IUnknown> unk;
            winrt::check_hresult(ASACreateFactory(unk.put()));
            m_sscfactory = unk.as<SpatialAnchorsFactory>();
            m_titleText = L"factory created\nairtap to create session";
        }
#ifdef BASIC_DEMO
        break;
#endif
        case DemoStep::CreateSession:
        {
            m_enoughDataForSaving = false;
            m_cloudSession = m_sscfactory.CreateCloudSpatialAnchorSession();
            m_titleText = L"session created, not configured\nairtap to configure session";
        }
#ifdef BASIC_DEMO
        break;
#endif
        case DemoStep::ConfigSession:
        {
            auto configuration = m_cloudSession.Configuration();
            configuration.AccountId(SpatialAnchorsAccountId);
            configuration.AccountKey(SpatialAnchorsAccountKey);

            m_cloudSession.LogLevel(SessionLogLevel::All);
            AddEventListeners();
            m_titleText = L"session configured, not started\nairtap to start session";
        }
#ifdef BASIC_DEMO
        break;
#endif
        case DemoStep::StartSession:
        {
            m_cloudSession.Start();
            m_sessionStarted = true;
            m_titleText = L"session started\nairtap to place anchor";
#ifndef BASIC_DEMO
            m_titleText = L"airtap to place first anchor";
            m_step = DemoStep::StartSession; // The fall throughs above didn't move the step counter.
#endif
        }
        break;
#ifdef BASIC_DEMO
        case DemoStep::CreateLocalAnchor:
        {
            auto lock = std::unique_lock<std::mutex>(m_mutex);
            m_localAnchor = nullptr;
            auto anchorVisual = winrt::make<winrt::SampleHoloLens::implementation::AnchorVisual>();
            anchorVisual.Id(L"");
            anchorVisual.Color(c_DefaultColor);
            // Create an anchor in front of the camera
            if (pose != nullptr)
            {
                // Get the gaze direction relative to the given coordinate system.
                const float3 headPosition = pose.Head().Position();
                const float3 headDirection = pose.Head().ForwardDirection();

                // The anchor is positioned one meter(s) along the user's gaze direction.
                constexpr float distanceFromUser = 2.0f; // meters
                const float3 gazeAtTwoMeters = headPosition + (distanceFromUser * headDirection);

                m_localAnchor = SpatialAnchor::TryCreateRelativeTo(m_coordinateSystem, gazeAtTwoMeters);
            }

            if (m_localAnchor == nullptr)
            {
                m_titleText = L"unable to create local anchor";
                return;
            }
            else
            {
                m_titleText = L"local anchor created\nairtap to prepare a cloud anchor";
            }
            anchorVisual.Anchor(m_localAnchor);
            m_foundAnchors.Insert(L"", anchorVisual);
        }
        break;
        case DemoStep::CreateCloudAnchor:
        {
            auto lock = std::unique_lock<std::mutex>(m_mutex);
            m_cloudAnchor = m_sscfactory.CreateCloudSpatialAnchor();
            m_cloudAnchor.LocalAnchor(m_localAnchor);
            m_titleText = L"cloud anchor created, not saved\nairtap to set expiration date";
        }
        break;
        case DemoStep::SetAnchorExpiration:
        {
            // In this sample app we delete the anchor explicitly, but you can also set it to expire automatically
            auto lock = std::unique_lock<std::mutex>(m_mutex);
            const int64_t oneWeekFromNowInHours = 7 * 24;
            const winrt::Windows::Foundation::DateTime oneWeekFromNow = winrt::Windows::Foundation::DateTime::clock::now() + std::chrono::hours(oneWeekFromNowInHours);
            m_cloudAnchor.Expiration(oneWeekFromNow);
            m_titleText = L"cloud anchor expiration date set to one week from now, not saved\nairtap to save anchor to the cloud";
        }
        break;
        case DemoStep::SaveCloudAnchor:
        {
            if (!m_enoughDataForSaving)
            {
                m_titleText = L"cannot save yet, not enough data\nlook around and airtap to try again";
                return;
            }

            SaveAnchor(DemoStep::SaveCloudAnchor);
        }
        break;
        case DemoStep::StopSession:
        {
            m_cloudSession.Stop();
            m_titleText = L"session stopped\nairtap to cleanup session";
        }
        break;
        case DemoStep::DestroySession:
        {
            auto lock = std::unique_lock<std::mutex>(m_mutex);
            m_titleText = L"session released\nairtap to create a new session for query";
            RemoveEventListeners();
            m_foundAnchors.Clear();
            m_localAnchor = nullptr;
            m_foundAnchor = nullptr;
            m_cloudAnchor = nullptr;
            m_sessionStarted = false;
            m_statusText = {};
            m_cloudSession = nullptr;
            if (m_targetId.empty())
            {
                m_step = DemoStep::CreateSession;
                return;
            }
        }
        break;
        case DemoStep::CreateForQuery:
        {
            m_enoughDataForSaving = false;
            m_cloudSession = m_sscfactory.CreateCloudSpatialAnchorSession();

            auto configuration = m_cloudSession.Configuration();
            configuration.AccountId(SpatialAnchorsAccountId);
            configuration.AccountKey(SpatialAnchorsAccountKey);

            m_cloudSession.LogLevel(SessionLogLevel::All);
            AddEventListeners();
            m_titleText = L"session configured for querying, not active\nairtap to start session and start locating";
        }
        break;
        case DemoStep::StartForQueryAndCreateWatcher:
        {
            m_cloudSession.Start();
            m_sessionStarted = true;

            AnchorLocateCriteria criteria = m_sscfactory.CreateAnchorLocateCriteria();

#ifdef USE_ANCHOR_EXCHANGE
            criteria.Identifiers(m_anchorExchange.AnchorKeys());
#else
            criteria.Identifiers({ m_targetId });
#endif
            m_cloudSession.CreateWatcher(criteria);
            m_titleText = L"criteria created, locating ...";
            m_asyncOpInProgress = true;
            return;
        }
        break;
        case DemoStep::DeleteFoundAnchor:
        {
            if (m_foundAnchor == nullptr) {
                m_titleText = L"cannot delete, found anchor not found yet";
                return;
            }
            DeleteAnchor(DemoStep::DeleteFoundAnchor);
        }
        break;
#else
        case DemoStep::CreateNearbyOne:
        case DemoStep::CreateNearbyTwo:
        case DemoStep::CreateNearbyThree:
        {
            if (!m_enoughDataForSaving)
            {
                m_titleText = L"cannot create anchor yet, not enough data";
                return;
            }
            {
                auto lock = std::unique_lock<std::mutex>(m_mutex);
                m_localAnchor = nullptr;
                auto anchorVisual = winrt::make<winrt::SampleHoloLens::implementation::AnchorVisual>();
                anchorVisual.Id(L"");
                anchorVisual.Color(c_DefaultColor);
                // Create an anchor in front of the camera
                if (pose != nullptr)
                {
                    // Get the gaze direction relative to the given coordinate system.
                    const float3 headPosition = pose.Head().Position();
                    const float3 headDirection = pose.Head().ForwardDirection();

                    // The anchor is positioned one meter(s) along the user's gaze direction.
                    constexpr float distanceFromUser = 2.0f; // meters
                    const float3 gazeAtTwoMeters = headPosition + (distanceFromUser * headDirection);

                    m_localAnchor = SpatialAnchor::TryCreateRelativeTo(m_coordinateSystem, gazeAtTwoMeters);
                }

                if (m_localAnchor == nullptr)
                {
                    m_titleText = L"unable to create local anchor";
                    return;
                }
                anchorVisual.Anchor(m_localAnchor);
                m_foundAnchors.Insert(L"", anchorVisual);
                m_cloudAnchor = m_sscfactory.CreateCloudSpatialAnchor();
                m_cloudAnchor.LocalAnchor(m_localAnchor);
            }

            SaveAnchor(m_step);
        }
        break;
        case DemoStep::CreateForQuery:
        {
            m_cloudSession.Stop();
            auto lock = std::unique_lock<std::mutex>(m_mutex);
            RemoveEventListeners();
            m_foundAnchors.Clear();
            m_localAnchor = nullptr;
            m_foundAnchor = nullptr;
            m_cloudAnchor = nullptr;
            m_sessionStarted = false;
            m_statusText = {};
            m_cloudSession = nullptr;
            if (m_targetId.empty())
            {
                m_step = DemoStep::CreateSession;
                return;
            }

            m_enoughDataForSaving = false;
            m_cloudSession = m_sscfactory.CreateCloudSpatialAnchorSession();

            auto configuration = m_cloudSession.Configuration();
            configuration.AccountId(SpatialAnchorsAccountId);
            configuration.AccountKey(SpatialAnchorsAccountKey);

            m_cloudSession.LogLevel(SessionLogLevel::All);
            AddEventListeners();
            m_cloudSession.Start();
            m_sessionStarted = true;
            m_titleText = L"session active for querying\nairtap to look for first anchor";
        }
        break;
        case DemoStep::LookForAnchor:
        {
            AnchorLocateCriteria criteria = m_sscfactory.CreateAnchorLocateCriteria();
            criteria.Identifiers({ m_targetId });
            m_cloudSession.CreateWatcher(criteria);
            m_titleText = L"criteria created, locating ...";
            return;
        }
        break;
        case DemoStep::LookForNearbyAnchors:
        {
            if (m_foundAnchor == nullptr) {
                m_titleText = L"still looking for first anchor.";
                return;
            }

            AnchorLocateCriteria criteria = m_sscfactory.CreateAnchorLocateCriteria();
            auto nearAnchor = m_sscfactory.CreateNearAnchorCriteria();
            nearAnchor.DistanceInMeters(10);
            nearAnchor.SourceAnchor(m_foundAnchor);
            criteria.NearAnchor(nearAnchor);

            m_cloudSession.CreateWatcher(criteria);
            m_titleText = L"nearby criteria created, locating ...";
        }
        break;
#endif
        case DemoStep::StopSessionForQuery:
        {
            auto lock = std::unique_lock<std::mutex>(m_mutex);
            RemoveEventListeners();
            m_foundAnchors.Clear();
            m_localAnchor = nullptr;
            m_cloudAnchor = nullptr;
            m_foundAnchor = nullptr;
            m_sessionStarted = false;
            m_statusText = {};
            m_cloudSession = nullptr;
            m_titleText = L"session stopped\nairtap to start again";
            m_step = DemoStep::CreateSession;
            return;
        }
        default:
        {
            assert(false);
            m_step = DemoStep::CreateSession;;
            return;
        }
    }
    m_step = static_cast<DemoStep>(static_cast<uint32_t>(m_step) + 1);
}
