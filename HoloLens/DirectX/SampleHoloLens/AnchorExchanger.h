// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#pragma once
#include <winrt/base.h>
#include "winrt/Windows.System.Threading.h"

class AnchorExchanger
{
public:
    AnchorExchanger(const std::wstring& redisUri);
    ~AnchorExchanger();

    void WatchKeys();
    std::vector<winrt::hstring> AnchorKeys();
    winrt::Windows::Foundation::IAsyncOperation<winrt::hstring> RetrieveAnchorKey(int64_t roomId);
    winrt::Windows::Foundation::IAsyncOperation<winrt::hstring> RetrieveLastAnchorKey();
    winrt::Windows::Foundation::IAsyncOperation<int64_t> StoreAnchorKey(const winrt::hstring& anchorKey);

private:
    winrt::fire_and_forget QueryAndAddAnchorKey(winrt::Windows::System::Threading::ThreadPoolTimer const& timer);

    std::wstring m_redisUri;
    std::vector<winrt::hstring> m_anchorKeys;
    winrt::Windows::System::Threading::ThreadPoolTimer m_timer{ nullptr };

    std::mutex m_mutex;
};

