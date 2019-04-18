// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef UTIL_H
#define UTIL_H

#include <android/asset_manager.h>
#include <android/log.h>
#include <cstdlib>
#include <GLES2/gl2.h>
#include <GLES2/gl2ext.h>
#include <string>

#include "arcore_c_api.h"

#define GLM_FORCE_RADIANS 1
#include "glm.hpp"
#include "gtc/matrix_transform.hpp"
#include "gtc/type_ptr.hpp"
#include "gtx/quaternion.hpp"

#define LOG_TAG "AzureSpatialAnchorsApplication"

#ifndef LOGI
#define LOGI(...) \
  __android_log_print(ANDROID_LOG_INFO, LOG_TAG, __VA_ARGS__)
#endif  // LOGI

#ifndef LOGD
#define LOGD(...) \
  __android_log_print(ANDROID_LOG_DEBUG, LOG_TAG, __VA_ARGS__)
#endif  // LOGD

#ifndef LOGE
#define LOGE(...) \
  __android_log_print(ANDROID_LOG_ERROR, LOG_TAG, __VA_ARGS__)
#endif  // LOGE

#ifndef CHECK
#define CHECK(condition)                                               \
  if (!(condition)) {                                                  \
    LOGE("CHECK FAILED at %s:%d: %s", __FILE__, __LINE__, #condition); \
    abort();                                                           \
  }
#endif  // CHECK

namespace AzureSpatialAnchors {
    namespace Util {

        // Provides a scoped allocated instance of ArPose.
        class ScopedArPose {
        public:
            explicit ScopedArPose(const ArSession* session) {
                ArPose_create(session, nullptr, &m_pose);
            }
            ScopedArPose(const ArSession* session, const float* pose_raw) {
                ArPose_create(session, pose_raw, &m_pose);
            }
            ~ScopedArPose() { ArPose_destroy(m_pose); }

            ArPose* AcquireArPose() { return m_pose; }

            // Delete copy constructors.
            ScopedArPose(const ScopedArPose&) = delete;
            void operator=(const ScopedArPose&) = delete;

        private:
            ArPose* m_pose;
        };

        // Check GL error, and abort if an error is encountered.
        void CheckGlError(const char* operation);

        // Create a shader program.
        GLuint CreateProgram(const char* vertexShaderFilename, const char* fragmentShaderFilename, AAssetManager* assetManager);

        // Load asset.
        bool LoadAsset(const char* filename, AAssetManager* assetManager, std::string* outFile);
    }
}

#endif
