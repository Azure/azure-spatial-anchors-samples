// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "CubeRenderer.h"

namespace AzureSpatialAnchors {

void CubeRenderer::Initialize(AAssetManager* assetManager) {
    m_program = Util::CreateProgram("shaders/cube.vert", "shaders/cube.frag", assetManager);

    if (!m_program) {
        LOGE("Could not create program.");
    }

    m_vertices = glGetAttribLocation(m_program, "aPosition");
    m_mvp = glGetUniformLocation(m_program, "uModelViewProjection");

    u_color = glGetUniformLocation(m_program, "uColor");
}

void CubeRenderer::Draw(const glm::mat4& modelViewProjection, const glm::vec3& color) const {
    if (!m_program) {
        LOGE("program is null.");
        return;
    }

    glUseProgram(m_program);

    glUniform4f(u_color, color.x, color.y, color.z, 255.0f);

    glEnableVertexAttribArray(m_vertices);
    glVertexAttribPointer(m_vertices, 3, GL_FLOAT, GL_FALSE, 0, CubeVertices);

    glm::mat4 scaledMvp = glm::scale(modelViewProjection, glm::vec3(0.1f, 0.1f, 0.1f));
    glUniformMatrix4fv(m_mvp, 1, GL_FALSE, glm::value_ptr(scaledMvp));

    glDrawElements(GL_TRIANGLES, 36, GL_UNSIGNED_BYTE, CubeIndices);

    glUseProgram(0);
    Util::CheckGlError("CubeRenderer::Draw() error");
}

}
