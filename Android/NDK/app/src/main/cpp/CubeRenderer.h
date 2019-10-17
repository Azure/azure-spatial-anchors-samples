// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef CUBE_RENDERER_H
#define CUBE_RENDERER_H

#include "Util.h"

namespace AzureSpatialAnchors {
    class CubeRenderer {
    public:
        CubeRenderer() = default;

        ~CubeRenderer() = default;

        void Initialize(AAssetManager* assetManager);
        void Draw(const glm::mat4 &modelViewProjection, const glm::vec3& color) const;

    private:
        GLuint m_program;
        GLint m_vertices;
        GLint m_mvp;
        GLint u_color;
    };

    const GLfloat CubeVertices[] = {
            -1.0f, -1.0f, -1.0f,
            1.0f, -1.0f, -1.0f,
            1.0f, 1.0f, -1.0f,
            -1.0f, 1.0f, -1.0f,
            -1.0f, -1.0f, 1.0f,
            1.0f, -1.0f, 1.0f,
            1.0f, 1.0f, 1.0f,
            -1.0f, 1.0f, 1.0f
    };

    const GLbyte CubeIndices[] = {
            0, 4, 5, 0, 5, 1,
            1, 5, 6, 1, 6, 2,
            2, 6, 7, 2, 7, 3,
            3, 7, 4, 3, 4, 0,
            4, 7, 6, 4, 6, 5,
            3, 0, 1, 3, 1, 2
    };
}

#endif
