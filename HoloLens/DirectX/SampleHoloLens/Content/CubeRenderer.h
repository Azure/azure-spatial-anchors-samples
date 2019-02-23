// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#pragma once

#include "..\Common\DeviceResources.h"
#include "..\Common\StepTimer.h"
#include "ShaderStructures.h"

namespace SampleHoloLens
{
    // This sample renderer instantiates a basic rendering pipeline.
    class CubeRenderer
    {
    public:
        CubeRenderer(std::shared_ptr<DX::DeviceResources> const& deviceResources);
        void CreateDeviceDependentResources();
        void ReleaseDeviceDependentResources();
        void Update(DX::StepTimer const& timer);
        void Render();

        // Locates the sample hologram at the origin + offset of the supplied coordinateSystem
        void PositionHologram(winrt::Windows::Perception::Spatial::SpatialCoordinateSystem const& coordinateSystem);

        // Repositions the sample hologram for the current frame
        void UpdateHologramPosition(winrt::Windows::Perception::Spatial::SpatialCoordinateSystem const& currentCoordinateSystem);

        // Property accessors.
        void SetOffset(winrt::Windows::Foundation::Numerics::float3 const& value) { m_offset = value;  }
        winrt::Windows::Foundation::Numerics::float3 const& GetOffset()           { return m_offset; }

        void SetColor(winrt::Windows::Foundation::Numerics::float4 const& value) { m_color = value; }
        winrt::Windows::Foundation::Numerics::float4 const& GetColor()           { return m_color; }

    private:
        // Cached pointer to device resources.
        std::shared_ptr<DX::DeviceResources>            m_deviceResources;

        // Direct3D resources for cube geometry.
        Microsoft::WRL::ComPtr<ID3D11InputLayout>       m_inputLayout;
        Microsoft::WRL::ComPtr<ID3D11Buffer>            m_vertexBuffer;
        Microsoft::WRL::ComPtr<ID3D11Buffer>            m_indexBuffer;
        Microsoft::WRL::ComPtr<ID3D11VertexShader>      m_vertexShader;
        Microsoft::WRL::ComPtr<ID3D11GeometryShader>    m_geometryShader;
        Microsoft::WRL::ComPtr<ID3D11PixelShader>       m_pixelShader;
        Microsoft::WRL::ComPtr<ID3D11Buffer>            m_modelConstantBuffer;

        // System resources for cube geometry.
        ModelConstantBuffer                             m_modelConstantBufferData;
        uint32_t                                        m_indexCount = 0;

        // Variables used with the rendering loop.
        bool                                            m_loadingComplete = false;
        float                                           m_degreesPerSecond = 0.f;
        winrt::Windows::Foundation::Numerics::float4    m_color = { 1.f, 1.f, 0.f, 1.f };
        winrt::Windows::Foundation::Numerics::float3    m_offset = { 0.f, 0.f, 0.f };
        winrt::Windows::Foundation::Numerics::float4x4  m_currentLocation;
        bool                                            m_located = false;
        winrt::Windows::Perception::Spatial::SpatialCoordinateSystem m_coordinateSystem = nullptr;

        // If the current D3D Device supports VPRT, we can avoid using a geometry
        // shader just to set the render target array index.
        bool                                            m_usingVprtShaders = false;
    };
}
