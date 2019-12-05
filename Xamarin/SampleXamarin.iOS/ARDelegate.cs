// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ARKit;
using Foundation;
using SceneKit;
using System;
using System.Diagnostics;

namespace SampleXamarin.iOS
{
    public class ARDelegate : ARSCNViewDelegate
    {
        private readonly DemoViewControllerBase source;

        public ARDelegate(DemoViewControllerBase bvc)
        {
            this.source = bvc;
        }

        public override void DidUpdateNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
        {
            if (anchor is ARPlaneAnchor planeAnchor)
            {
                Console.WriteLine($"The (updated) extent of the anchor is [{planeAnchor.Extent.X}, {planeAnchor.Extent.Y}, {planeAnchor.Extent.Z}]");
            }
        }

        public override void WasInterrupted(ARSession session)
        {
            base.WasInterrupted(session);

            this.source.ShowLogMessage("Something went wrong. Exit and try again.", SubView.ErrorView);
        }

        public override void InterruptionEnded(ARSession session)
        {
            base.InterruptionEnded(session);
            this.source.cloudSession.Reset();
        }

        public override void DidFail(ARSession session, NSError error)
        {
            base.DidFail(session, error);
            Debug.WriteLine("ERROR : " + error);
        }

        public override void WillRenderScene(ISCNSceneRenderer renderer, SCNScene scene, double timeInSeconds)
        {
            this.source.OnUpdateScene(timeInSeconds);
        }
    }
}