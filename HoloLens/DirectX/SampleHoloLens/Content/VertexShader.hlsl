// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// Per-vertex data passed to the geometry shader.
struct VertexShaderOutput
{
    min16float4 pos      : SV_POSITION;
    min16float3 color    : COLOR0;
    min16float2 texCoord : TEXCOORD1;

    // The render target array index will be set by the geometry shader.
    uint        viewId   : TEXCOORD0;
};

#include "VertexShaderShared.hlsl"
