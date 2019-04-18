// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "Util.h"

namespace AzureSpatialAnchors {
    namespace Util {
        void CheckGlError(const char* operation) {
            bool hasError = false;
            for (GLint error = glGetError(); error; error = glGetError()) {
                LOGE("operation %s() glError (0x%x)\n", operation, error);
                hasError = true;
            }
            if (hasError) {
                abort();
            }
        }

        bool LoadAsset(const char* filename, AAssetManager* assetManager, std::string* outFile) {
            AAsset* asset = AAssetManager_open(assetManager, filename, AASSET_MODE_STREAMING);

            if (asset == nullptr) {
                LOGE("Failed to open asset %s", filename);
                return false;
            }

            off_t assetSize = AAsset_getLength(asset);
            outFile->resize(assetSize);

            int bytesRead = AAsset_read(asset, &outFile->front(), assetSize);

            AAsset_close(asset);

            if (bytesRead <= 0) {
                LOGE("Failed to read file: %s", filename);
                return false;
            }

            return true;
        }

        GLuint LoadShader(GLenum shaderType, const char* shaderSource) {
            GLuint shader = glCreateShader(shaderType);
            if (!shader) {
                return shader;
            }

            glShaderSource(shader, 1, &shaderSource, nullptr);
            glCompileShader(shader);
            GLint compiled = 0;
            glGetShaderiv(shader, GL_COMPILE_STATUS, &compiled);

            if (!compiled) {
                LOGE("AzureSpatialAnchors::Util::LoadShader Could not compile shader");
                glDeleteShader(shader);
                shader = 0;
            }

            return shader;
        }

        GLuint CreateProgram(const char* vertexShaderFilename, const char* fragmentShaderFilename, AAssetManager* assetManager) {
            std::string vertexShaderContent;
            std::string fragmentShaderContent;

            if (!LoadAsset(vertexShaderFilename, assetManager, &vertexShaderContent)) {
                LOGE("Failed to load file: %s", vertexShaderFilename);
                return 0;
            }
            if (!LoadAsset(fragmentShaderFilename, assetManager, &fragmentShaderContent)) {
                LOGE("Failed to load file: %s", fragmentShaderFilename);
                return 0;
            }

            GLuint vertexShader = LoadShader(GL_VERTEX_SHADER, vertexShaderContent.c_str());
            if (!vertexShader) {
                return 0;
            }

            GLuint fragmentShader = LoadShader(GL_FRAGMENT_SHADER, fragmentShaderContent.c_str());
            if (!fragmentShader) {
                return 0;
            }

            GLuint program = glCreateProgram();

            if (program) {
                glAttachShader(program, vertexShader);
                CheckGlError("AzureSpatialAnchors::Util::CreateProgram glAttachShader");
                glAttachShader(program, fragmentShader);
                CheckGlError("AzureSpatialAnchors::Util::CreateProgram glAttachShader");

                glLinkProgram(program);
                GLint linkStatus = GL_FALSE;
                glGetProgramiv(program, GL_LINK_STATUS, &linkStatus);
                if (linkStatus != GL_TRUE) {
                    LOGE("AzureSpatialAnchors::Util::CreateProgram Could not link program");
                    glDeleteProgram(program);
                    program = 0;
                }
            }
            return program;
        }
    }
}
