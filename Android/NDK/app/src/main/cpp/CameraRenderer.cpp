// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "CameraRenderer.h"

namespace AzureSpatialAnchors {

const GLfloat QuadVertices[] = {
        -1.0f, -1.0f, 0.0f,
        +1.0f, -1.0f, 0.0f,
        -1.0f, +1.0f, 0.0f,
        +1.0f, +1.0f, 0.0f
};
const GLfloat QuadUvs[] {
        0.0f, 1.0f,
        1.0f, 1.0f,
        0.0f, 0.0f,
        1.0f, 0.0f
};

void CameraRenderer::Initialize(AAssetManager* assetManager) {
    m_program = Util::CreateProgram("shaders/camera.vert", "shaders/camera.frag", assetManager);
    if (!m_program) {
        LOGE("Failed to create program.");
    }

    m_texture = static_cast<GLuint>(glGetUniformLocation(m_program, "uTexture"));
    m_vertices = static_cast<GLuint>(glGetAttribLocation(m_program, "aPosition"));
    m_uvs = static_cast<GLuint>(glGetAttribLocation(m_program, "aTextureCoord"));

    glGenTextures(1, &m_textureId);
    glBindTexture(GL_TEXTURE_EXTERNAL_OES, m_textureId);
    glTexParameteri(GL_TEXTURE_EXTERNAL_OES, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_EXTERNAL_OES, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
}

void CameraRenderer::Draw(const ArSession* session, const ArFrame* frame) {
    int32_t displayGeometryChanged = 0;
    ArFrame_getDisplayGeometryChanged(session, frame, &displayGeometryChanged);
    if (displayGeometryChanged != 0) {
        // Transform the given texture coordinates to correctly show the background image.
        ArFrame_transformDisplayUvCoords(session, frame, 8, QuadUvs, m_transformedUvs);
    }

    int64_t frameTimestamp;
    ArFrame_getTimestamp(session, frame, &frameTimestamp);
    if (frameTimestamp == 0) {
        // Skip rendering if no frame is produced.
        return;
    }

    RenderQuad();
}

void CameraRenderer::RenderQuad() {
    if (!m_program) {
        LOGE("program is null.");
        return;
    }

    glUseProgram(m_program);
    glDepthMask(GL_FALSE);

    glUniform1i(m_texture, 1);
    glActiveTexture(GL_TEXTURE1);

    glEnableVertexAttribArray(m_vertices);
    glVertexAttribPointer(m_vertices, 3, GL_FLOAT, GL_FALSE, 0, QuadVertices);

    glEnableVertexAttribArray(m_uvs);
    glVertexAttribPointer(m_uvs, 2, GL_FLOAT, GL_FALSE, 0, m_transformedUvs);

    glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);

    glUseProgram(0);
    glDepthMask(GL_TRUE);
    Util::CheckGlError("CameraRenderer::RenderQuad() error");
}

GLuint CameraRenderer::GetTextureId() const {
    return m_textureId;
}

}
