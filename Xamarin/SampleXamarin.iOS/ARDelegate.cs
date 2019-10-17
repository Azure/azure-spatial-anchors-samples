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
        private readonly DemoControllerBase source;

        public ARDelegate(DemoControllerBase bvc)
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
            // Note: Always a super-tricky thing in ARKit : must get rid of the managed reference to the Frame object ASAP.
            using (ARFrame frame = this.source.scnView?.Session?.CurrentFrame)
            {
                if (frame == null)
                {
                    return;
                }

                if (this.source.cloudSession == null)
                {
                    return;
                }

                this.source.cloudSession.ProcessFrame(frame);

                if (this.source.currentlyPlacingAnchor && this.source.enoughDataForSaving && this.source.localAnchor != null)
                {
                    this.source.CreateCloudAnchor();
                }
            }
        }
    }
}