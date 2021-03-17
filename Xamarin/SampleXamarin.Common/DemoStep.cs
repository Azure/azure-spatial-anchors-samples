// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace SampleXamarin
{
    public enum DemoStep : uint
    {
        Start,               // prepare to start the demo
        End,                 // the end of the demo
        Restart,             // waiting to restart the demo
        CreateAnchor,        // the session will create a cloud anchor
        SaveAnchor,          // the session will save an anchor
        SavingAnchor,        // the session is in process of saving an anchor
        LocateAnchor,        // the session will look for an anchor
        LocateNearbyAnchors, // the session will look for nearby anchors
        DeleteLocatedAnchors,// the session will delete found anchors
        StopSession,         // the session will stop and be cleaned up
        StopWatcher,         // the watcher will stop looking for anchors
        EnterAnchorNumber,   // sharing: enter an anchor to find
    }
}
