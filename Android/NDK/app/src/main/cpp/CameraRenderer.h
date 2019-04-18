// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef CAMERA_RENDERER_H
#define CAMERA_RENDERER_H

#include "Util.h"

namespace AzureSpatialAnchors {

    // This class renders the passthrough camera image into the OpenGL frame.
    class CameraRenderer {

    public:
        CameraRenderer() = default;
        ~CameraRenderer() = default;

        void Initialize(AAssetManager* assetManager);
        void Draw(const ArSession* session, const ArFrame* frame);
        GLuint GetTextureId() const;

    private:
        void RenderQuad();

        GLuint m_program;
        GLuint m_textureId;

        GLuint m_texture;
        GLuint m_vertices;
        GLuint m_uvs;

        float m_transformedUvs[8];
    };
}

#endif
