// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#pragma once

#include "Common\DeviceResources.h"
#include "Common\StepTimer.h"
#include "ShaderStructures.h"
#include "QuadRenderer.h"
#include "CubeRenderer.h"
#include "TextRenderer.h"
#include <memory>
#include <string>

namespace SampleHoloLens
{
    // This sample renderer instantiates a basic rendering pipeline.
    class LabeledCubeRenderer
    {
    public:
        LabeledCubeRenderer(const std::shared_ptr<DX::DeviceResources>& deviceResources,
            winrt::Windows::Perception::Spatial::SpatialCoordinateSystem const& position,
            winrt::hstring const& label,
            winrt::Windows::Foundation::Numerics::float4 const& color);
        void CreateDeviceDependentResources();
        void ReleaseDeviceDependentResources();
        void Update(
            DX::StepTimer const& timer,
            winrt::Windows::Perception::Spatial::SpatialCoordinateSystem const& currentCoordinateSystem);
        void Render();

    private:
        // Sample content is used to demonstrate located anchor.
        std::unique_ptr<CubeRenderer>                              m_cubeRenderer;

        // Renders text off-screen. Used to create a texture to render on the quad.
        std::unique_ptr<TextRenderer>                              m_textRenderer;
        std::unique_ptr<QuadRenderer>                              m_quadRenderer;

        std::wstring m_label;
        winrt::Windows::Foundation::Numerics::float4               m_color = { 1.f, 1.f, 0.f, 1.f };
    };
}