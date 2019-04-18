// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#extension GL_OES_EGL_image_external : require

precision mediump float;

uniform samplerExternalOES uTexture;
varying vec2 vTextureCoord;

void main() {
    gl_FragColor = texture2D(uTexture, vTextureCoord);
}
