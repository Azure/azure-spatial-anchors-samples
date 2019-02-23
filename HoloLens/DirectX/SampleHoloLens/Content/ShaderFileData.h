// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#pragma once

namespace SampleHoloLens
{
    class ShaderFileData
    {
    public:
        static const ShaderFileData& GetInstance();

        const std::vector<byte>& GetVprtVertex() const { return m_vprtVertex; }
        const std::vector<byte>& GetVertex() const { return m_vertex; }
        const std::vector<byte>& GetPixel() const { return m_pixel; }
        const std::vector<byte>& GetGeometry() const { return m_geometry; }
        const std::vector<byte>& GetTexturePixel() const { return m_texturePixel; }

        bool LoadingComplete() const { return m_loadingComplete; }

    private:
        static ShaderFileData s_shaderFileData;

        ShaderFileData() = default;

        winrt::fire_and_forget LoadShaderFileData();

        std::vector<byte> m_vprtVertex;
        std::vector<byte> m_vertex;
        std::vector<byte> m_pixel;
        std::vector<byte> m_geometry;
        std::vector<byte> m_texturePixel;
        bool m_loadingComplete = false;
    };
}

