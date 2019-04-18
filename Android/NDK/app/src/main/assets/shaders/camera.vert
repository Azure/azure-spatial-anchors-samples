// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

attribute vec4 aPosition;
attribute vec2 aTextureCoord;

varying vec2 vTextureCoord;

void main() {
    gl_Position = aPosition;
    vTextureCoord = aTextureCoord;
}
