// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#include "pch.h"
#include "AnchorExchanger.h"
#include "winrt/Windows.Web.Http.h"

using namespace winrt;
using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Web::Http;
using namespace winrt::Windows::System::Threading;

AnchorExchanger::AnchorExchanger(const std::wstring& redisUri)
{
    m_redisUri = redisUri;
}


AnchorExchanger::~AnchorExchanger()
{
    if (m_timer != nullptr)
    {
        m_timer.Cancel();
    }
}

std::vector<winrt::hstring> AnchorExchanger::AnchorKeys()
{
    auto lock = std::unique_lock<std::mutex>(m_mutex);
    return m_anchorKeys;
}

winrt::fire_and_forget AnchorExchanger::QueryAndAddAnchorKey(ThreadPoolTimer const&)
{
    auto currentKey = co_await RetrieveLastAnchorKey();
    if (currentKey != L"")
    {
        auto lock = std::unique_lock<std::mutex>(m_mutex);
        if (std::find(m_anchorKeys.begin(), m_anchorKeys.end(), currentKey) == m_anchorKeys.end())
        {
            m_anchorKeys.emplace_back(currentKey);
        }
    }
}

void AnchorExchanger::WatchKeys()
{
    m_timer = ThreadPoolTimer::CreatePeriodicTimer({ this, &AnchorExchanger::QueryAndAddAnchorKey }, std::chrono::milliseconds(500));
}

winrt::Windows::Foundation::IAsyncOperation<hstring> AnchorExchanger::RetrieveAnchorKey(int64_t roomId)
{
    try
    {
        Uri uri{ m_redisUri + L"/" + std::to_wstring(roomId) };
        HttpClient httpClient{};
        return co_await httpClient.GetStringAsync(uri);
    }
    catch (...)
    {
        return {};
    }
}

winrt::Windows::Foundation::IAsyncOperation<hstring> AnchorExchanger::RetrieveLastAnchorKey()
{
    try
    {
        Uri uri{ m_redisUri + L"/last" };
        HttpClient httpClient{};
        return co_await httpClient.GetStringAsync(uri);
    }
    catch (...)
    {
        return {};
    }
}

winrt::Windows::Foundation::IAsyncOperation<int64_t> AnchorExchanger::StoreAnchorKey(const hstring& anchorKey)
{
    co_await winrt::resume_background();
    if (anchorKey.empty())
    {
        return -1;
    }

    {
        auto lock = std::unique_lock<std::mutex>(m_mutex);
        m_anchorKeys.emplace_back(anchorKey);
    }

    try
    {
        HttpClient httpClient{};
        auto response{ co_await httpClient.PostAsync(Uri{ m_redisUri }, HttpStringContent{ anchorKey }) };
        if (response.IsSuccessStatusCode())
        {
            auto responseBody{ co_await response.Content().ReadAsStringAsync() };
            return std::wcstoll(responseBody.c_str(), nullptr, 0);
        }
    }
    catch (...)
    {
        return -1;
    }
}
