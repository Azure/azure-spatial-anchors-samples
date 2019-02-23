// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// Per-pixel color data passed through the pixel shader.
struct PixelShaderInput
{
    min16float4 pos      : SV_POSITION;
    min16float3 color    : COLOR0;
    min16float2 texCoord : TEXCOORD1;
};

// The pixel shader passes through the color data. The color data from 
// is interpolated and assigned to a pixel at the rasterization step.
min16float4 main(PixelShaderInput input) : SV_TARGET
{
    return min16float4(input.color, 1.0f);
}
