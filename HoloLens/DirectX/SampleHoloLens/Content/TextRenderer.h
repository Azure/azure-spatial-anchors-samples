// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#pragma once

#include "common\DeviceResources.h"

namespace SampleHoloLens
{
    class TextRenderer
    {
    public:
        TextRenderer(const std::shared_ptr<DX::DeviceResources>& deviceResources, unsigned int const& textureWidth, unsigned int const& textureHeight);

        void RenderTextOffscreen(const std::wstring& title, const std::wstring& status, const std::wstring& msg);

        void CreateDeviceDependentResources();
        void ReleaseDeviceDependentResources();

        ID3D11ShaderResourceView* GetTexture() const { return m_shaderResourceView.Get(); };
        ID3D11SamplerState*       GetSampler() const { return m_pointSampler.Get(); };

    private:
        // Cached pointer to device resources.
        const std::shared_ptr<DX::DeviceResources> m_deviceResources;

        // Direct3D resources for rendering text to an off-screen render target.
        Microsoft::WRL::ComPtr<ID3D11Texture2D>             m_texture;
        Microsoft::WRL::ComPtr<ID3D11ShaderResourceView>    m_shaderResourceView;
        Microsoft::WRL::ComPtr<ID3D11SamplerState>          m_pointSampler;
        Microsoft::WRL::ComPtr<ID3D11RenderTargetView>      m_renderTargetView;
        Microsoft::WRL::ComPtr<ID2D1RenderTarget>           m_d2dRenderTarget;
        Microsoft::WRL::ComPtr<ID2D1SolidColorBrush>        m_whiteBrush;
        Microsoft::WRL::ComPtr<IDWriteTextFormat>           m_textFormat;

        // CPU-based variables for configuring the offscreen render target.
        const unsigned int m_textureWidth;
        const unsigned int m_textureHeight;

        std::wstring m_lastTitle;
        std::wstring m_lastMsg;
        std::wstring m_lastStatus;
    };
}