// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
//
// Declaration of the AnchorVisual class.
//

#pragma once

#include "AnchorVisual.g.h"

namespace winrt::SampleHoloLens::implementation
{
    struct AnchorVisual : AnchorVisualT<AnchorVisual>
    {
        AnchorVisual() = default;

        winrt::hstring Id() { return m_id; }
        void Id(winrt::hstring value) { m_id = value; }

        winrt::Windows::Foundation::Numerics::float4 Color() { return m_color; }
        void Color(winrt::Windows::Foundation::Numerics::float4 value) { m_color = value; }

        winrt::Windows::Perception::Spatial::SpatialAnchor Anchor() { return m_anchor; }
        void Anchor(winrt::Windows::Perception::Spatial::SpatialAnchor value) { m_anchor = value; }

    private:
        winrt::hstring m_id;
        winrt::Windows::Foundation::Numerics::float4 m_color{ 0.f, 0.f, 1.f, 1.f };
        winrt::Windows::Perception::Spatial::SpatialAnchor m_anchor{ nullptr };
    };
}

namespace winrt::SampleHoloLens::factory_implementation
{
    struct AnchorVisual : AnchorVisualT<AnchorVisual, implementation::AnchorVisual>
    {
    };
}
