// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

enum DemoStep: uint {
    case prepare                // prepare to start
    case createCloudAnchor      // the session will create a cloud anchor
    case lookForAnchor          // the session will look for an anchor
    case lookForNearbyAnchors   // the session will look for nearby anchors
    case deleteFoundAnchors     // the session will delete found anchors
    case stopSession            // the session will stop and be cleaned up
}
