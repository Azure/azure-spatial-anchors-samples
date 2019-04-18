// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

precision mediump float;

varying vec3 vColor;

void main() {
    gl_FragColor = vec4(vColor, 1.0);
}