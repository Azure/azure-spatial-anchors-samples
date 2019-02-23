// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#include "pch.h"
#include "LabeledCubeRenderer.h"
#include "Common\DirectXHelper.h"

using namespace SampleHoloLens;
using namespace DirectX;
namespace winrt
{
    using namespace Windows::Foundation;
}
using namespace winrt::Windows::Foundation::Numerics;
using namespace winrt::Windows::UI::Input::Spatial;
using namespace winrt::Windows::Perception::Spatial;

// Loads vertex and pixel shaders from files and instantiates the cube geometry.
LabeledCubeRenderer::LabeledCubeRenderer(
    std::shared_ptr<DX::DeviceResources> const& deviceResources,
    winrt::Windows::Perception::Spatial::SpatialCoordinateSystem const& position,
    winrt::hstring const& label,
    winrt::Windows::Foundation::Numerics::float4 const& color)
    : m_label(label)
    , m_color(color)
{
    // Initialize the sample hologram.
    m_cubeRenderer = std::make_unique<CubeRenderer>(deviceResources);
    // Initialize the text renderer.
    constexpr unsigned int offscreenRenderTargetWidth = 2048;
    m_textRenderer = std::make_unique<TextRenderer>(deviceResources, offscreenRenderTargetWidth, offscreenRenderTargetWidth);
    // Initialize the text hologram.
    m_quadRenderer = std::make_unique<QuadRenderer>(deviceResources, false);

    m_cubeRenderer->PositionHologram(position);
    m_quadRenderer->PositionHologram(position);

    m_textRenderer->RenderTextOffscreen(m_label, {}, {});
    m_cubeRenderer->SetColor(m_color);
}

void LabeledCubeRenderer::CreateDeviceDependentResources()
{
    m_cubeRenderer->CreateDeviceDependentResources();
    m_textRenderer->CreateDeviceDependentResources();
    m_quadRenderer->CreateDeviceDependentResources();
}

void LabeledCubeRenderer::ReleaseDeviceDependentResources()
{
    m_cubeRenderer->ReleaseDeviceDependentResources();
    m_textRenderer->ReleaseDeviceDependentResources();
    m_quadRenderer->ReleaseDeviceDependentResources();
}

// Called once per frame. Rotates the cube, and calculates and sets the model matrix
// relative to the position transform indicated by hologramPositionTransform.
void LabeledCubeRenderer::Update(
    DX::StepTimer const& timer,
    winrt::Windows::Perception::Spatial::SpatialCoordinateSystem const& currentCoordinateSystem)
{
    m_cubeRenderer->UpdateHologramPosition(currentCoordinateSystem);
    m_quadRenderer->UpdateHologramPosition(currentCoordinateSystem, nullptr, timer);

    m_cubeRenderer->Update(timer);
    m_quadRenderer->Update(timer);
}

void LabeledCubeRenderer::Render()
{
    m_cubeRenderer->Render();
    m_quadRenderer->Render(m_textRenderer->GetTexture());
}