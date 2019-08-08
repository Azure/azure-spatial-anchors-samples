// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ARKit;
using Microsoft.Azure.SpatialAnchors;
using SceneKit;

namespace SampleXamarin.iOS
{
    public class AnchorVisual
    {
        public SCNNode node { get; set; }
        public string identifier { get; set; }
        public CloudSpatialAnchor cloudAnchor { get; set; }
        public ARAnchor localAnchor { get; set; }
    }
}