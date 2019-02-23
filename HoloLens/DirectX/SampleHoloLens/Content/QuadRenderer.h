// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#pragma once

#include "..\Common\DeviceResources.h"
#include "..\Common\StepTimer.h"
#include "ShaderStructures.h"

namespace SampleHoloLens
{
    // This sample renderer instantiates a basic rendering pipeline.
    class QuadRenderer
    {
    public:
        QuadRenderer(const std::shared_ptr<DX::DeviceResources>& deviceResources, bool tagAlong);
        void CreateDeviceDependentResources();
        void ReleaseDeviceDependentResources();
        void Update(const DX::StepTimer& timer);
        void Render(ID3D11ShaderResourceView* texture);
        void Render();

        void StartFadeIn();
        void StartFadeOut();

        // Locates the sample hologram at the origin + offset of the supplied coordinateSystem
        void PositionHologram(winrt::Windows::Perception::Spatial::SpatialCoordinateSystem const& coordinateSystem);

        // Repositions the sample hologram.
        void UpdateHologramPosition(winrt::Windows::Perception::Spatial::SpatialCoordinateSystem const& currentCoordinateSystem, winrt::Windows::UI::Input::Spatial::SpatialPointerPose const& pointerPose, const DX::StepTimer& timer);

        // Property accessors.
        void ResetPosition(winrt::Windows::Foundation::Numerics::float3 pos) {
            m_lastPosition = pos;
            m_position = pos;
        }
        const winrt::Windows::Foundation::Numerics::float3& GetPosition() const { return m_position; }
        const winrt::Windows::Foundation::Numerics::float3& GetVelocity() const { return m_velocity; }

    private:
        // Cached pointer to device resources.
        std::shared_ptr<DX::DeviceResources>                m_deviceResources;

        // Direct3D resources for quad geometry.
        Microsoft::WRL::ComPtr<ID3D11InputLayout>           m_inputLayout;
        Microsoft::WRL::ComPtr<ID3D11Buffer>                m_vertexBuffer;
        Microsoft::WRL::ComPtr<ID3D11Buffer>                m_indexBuffer;
        Microsoft::WRL::ComPtr<ID3D11VertexShader>          m_vertexShader;
        Microsoft::WRL::ComPtr<ID3D11GeometryShader>        m_geometryShader;
        Microsoft::WRL::ComPtr<ID3D11PixelShader>           m_pixelShader;
        Microsoft::WRL::ComPtr<ID3D11Buffer>                m_modelConstantBuffer;

        // Direct3D resources for the default texture.
        Microsoft::WRL::ComPtr<ID3D11Resource>              m_quadTexture;
        Microsoft::WRL::ComPtr<ID3D11ShaderResourceView>    m_quadTextureView;
        Microsoft::WRL::ComPtr<ID3D11SamplerState>          m_quadTextureSamplerState;

        // System resources for quad geometry.
        ModelConstantBuffer                                 m_modelConstantBufferData;
        uint32_t                                            m_indexCount = 0;

        // Variables used with the rendering loop.
        bool                                                m_loadingComplete = false;
        float                                               m_degreesPerSecond = 45.f;
        winrt::Windows::Foundation::Numerics::float3        m_position = { 0.f, 0.f, -2.f };
        winrt::Windows::Foundation::Numerics::float3        m_lastPosition = { 0.f, 0.f, -2.f };
        winrt::Windows::Foundation::Numerics::float3        m_velocity = { 0.f, 0.f,  0.f };

        winrt::Windows::Perception::Spatial::SpatialCoordinateSystem m_coordinateSystem = nullptr;

        // If the current D3D Device supports VPRT, we can avoid using a geometry
        // shader just to set the render target array index.
        bool                                                m_usingVprtShaders = false;

        // This is the rate at which the hologram position is interpolated (LERPed) to the current location.
        const float                                         c_lerpRate = 1.5f;

        // Number of seconds it takes to fade the hologram in, or out.
        const float                                         c_maxFadeTime = 1.f;

        // Timer used to fade the hologram in, or out.
        float                                               m_fadeTime = 0.f;

        // Whether or not the hologram is fading in, or out.
        bool                                                m_fadingIn = false;

        bool                                                m_located = false;
        bool const                                          m_tagAlong;
    };
}
