// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

attribute vec4 aPosition;

uniform mat4 uModelViewProjection;

void main() {
    gl_Position = uModelViewProjection * aPosition;
}
