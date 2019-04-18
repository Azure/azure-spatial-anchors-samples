// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

attribute vec4 aPosition;
attribute vec3 aColor;

uniform mat4 uModelViewProjection;

varying vec3 vColor;

void main() {
    gl_Position = uModelViewProjection * aPosition;
    vColor = aColor;
}
