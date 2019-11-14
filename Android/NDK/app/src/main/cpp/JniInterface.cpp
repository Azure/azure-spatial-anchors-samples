// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include <android/asset_manager.h>
#include <android/asset_manager_jni.h>
#include <jni.h>
#include "AzureSpatialAnchorsApplication.h"

#define JNI_METHOD(returnType, methodName) JNIEXPORT returnType JNICALL Java_com_microsoft_samplenativeandroid_JniInterface_##methodName

#ifdef __cplusplus
extern "C" {
#endif

AzureSpatialAnchors::AzureSpatialAnchorsApplication *gNativeApplication;

JNI_METHOD(void, onCreate)
(JNIEnv *env, jclass, jobject jAssetManager) {
    if (gNativeApplication != nullptr) {
        return;
    }
    AAssetManager *assetManager = AAssetManager_fromJava(env, jAssetManager);
    gNativeApplication = new AzureSpatialAnchors::AzureSpatialAnchorsApplication(assetManager);
}

JNI_METHOD(void, onResume)
(JNIEnv *env, jclass, jobject context, jobject activity) {
    if (gNativeApplication == nullptr) {
        return;
    }
    gNativeApplication->OnResume(env, context, activity);
}

JNI_METHOD(void, onPause)
(JNIEnv *, jclass) {
    if (gNativeApplication == nullptr) {
        return;
    }
    gNativeApplication->OnPause();
}

JNI_METHOD(void, onDestroy)
(JNIEnv *, jclass) {
    if (gNativeApplication == nullptr) {
        return;
    }
    delete gNativeApplication;
}

JNI_METHOD(void, onSurfaceCreated)
(JNIEnv *, jclass) {
    if (gNativeApplication == nullptr) {
        return;
    }
    gNativeApplication->OnSurfaceCreated();
}

JNI_METHOD(void, onSurfaceChanged)
(JNIEnv *, jclass, jint display_rotation, jint width, jint height) {
    if (gNativeApplication == nullptr) {
        return;
    }
    gNativeApplication->OnSurfaceChanged(display_rotation, width, height);
}

JNI_METHOD(void, onDrawFrame)
(JNIEnv *, jclass) {
    if (gNativeApplication == nullptr) {
        return;
    }
    gNativeApplication->OnDrawFrame();
}

JNI_METHOD(void, onTouched)
(JNIEnv *, jclass, jfloat x, jfloat y) {
    if (gNativeApplication == nullptr) {
        return;
    }
    gNativeApplication->OnTouched(x, y);
}

JNI_METHOD(void, onBasicButtonPress)
(JNIEnv *, jclass) {
    if (gNativeApplication == nullptr) {
        return;
    }
    gNativeApplication->OnBasicButtonPress();
}

JNI_METHOD(void, onNearbyButtonPress)
(JNIEnv *, jclass) {
    if (gNativeApplication == nullptr) {
        return;
    }
    gNativeApplication->OnNearbyButtonPress();
}

JNI_METHOD(void, onCoarseRelocButtonPress)
(JNIEnv *, jclass) {
    if (gNativeApplication == nullptr) {
        return;
    }
    gNativeApplication->OnCoarseRelocButtonPress();
}

JNI_METHOD(void, advanceDemo)
(JNIEnv *, jclass) {
    if (gNativeApplication == nullptr) {
        return;
    }
    gNativeApplication->AdvanceDemo();
}


JNI_METHOD(jstring, getStatusText)
(JNIEnv * env, jclass) {
    if (gNativeApplication == nullptr) {
        return nullptr;
    }
    auto text = gNativeApplication->GetStatusText();
    return env->NewStringUTF(text);
}

JNI_METHOD(jstring, getButtonText)
(JNIEnv * env, jclass) {
    if (gNativeApplication == nullptr) {
        return nullptr;
    }
    auto text = gNativeApplication->GetButtonText();
    return env->NewStringUTF(text);
}

JNI_METHOD(jboolean, showAdvanceButton)
(JNIEnv *, jclass) {
    if (gNativeApplication == nullptr) {
        return JNI_FALSE;
    }
    return static_cast<jboolean>(gNativeApplication->ShowAdvanceButton() ? JNI_TRUE : JNI_FALSE);
}

JNI_METHOD(jboolean, isSpatialAnchorsAccountSet)
(JNIEnv *, jclass) {
    if (gNativeApplication == nullptr) {
        return JNI_FALSE;
    }
    return static_cast<jboolean>(gNativeApplication->IsSpatialAnchorsAccountSet() ? JNI_TRUE : JNI_FALSE);
}

JNI_METHOD(void, updateGeoLocationPermission)
(JNIEnv *, jclass, jboolean isGranted) {
    if (gNativeApplication == nullptr) {
        return;
    }
    return gNativeApplication->UpdateGeoLocationPermission(isGranted);
}

JNI_METHOD(void, updateWifiPermission)
(JNIEnv *, jclass, jboolean isGranted) {
    if (gNativeApplication == nullptr) {
        return;
    }
    return gNativeApplication->UpdateWifiPermission(isGranted);
}

JNI_METHOD(void, updateBluetoothPermission)
(JNIEnv *, jclass, jboolean isGranted) {
    if (gNativeApplication == nullptr) {
        return;
    }
    return gNativeApplication->UpdateBluetoothPermission(isGranted);
}

#ifdef  __cplusplus
}
#endif
