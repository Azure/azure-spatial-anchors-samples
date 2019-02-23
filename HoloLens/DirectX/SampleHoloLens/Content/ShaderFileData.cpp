// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#include "pch.h"
#include "ShaderFileData.h"
#include "Common\DirectXHelper.h"

using namespace SampleHoloLens;

std::once_flag initialized;
ShaderFileData ShaderFileData::s_shaderFileData;

const ShaderFileData& ShaderFileData::GetInstance()
{
    std::call_once(initialized, []() { s_shaderFileData.LoadShaderFileData(); });
    return s_shaderFileData;
}

// Workaround for co_await code generation with optimizations enabled
#pragma optimize( "", off )
winrt::fire_and_forget ShaderFileData::LoadShaderFileData()
{
    m_vprtVertex = co_await DX::ReadDataAsync(L"ms-appx:///VprtVertexShader.cso");
    m_vertex = co_await DX::ReadDataAsync(L"ms-appx:///VertexShader.cso");
    m_pixel = co_await DX::ReadDataAsync(L"ms-appx:///PixelShader.cso");
    m_geometry = co_await DX::ReadDataAsync(L"ms-appx:///GeometryShader.cso");
    m_texturePixel = co_await DX::ReadDataAsync(L"ms-appx:///TexturePixelShader.cso");
    m_loadingComplete = true;
};
#pragma optimize( "", on )