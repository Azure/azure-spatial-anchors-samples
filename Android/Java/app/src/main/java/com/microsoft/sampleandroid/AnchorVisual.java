// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import com.google.ar.core.Anchor;
import com.google.ar.sceneform.AnchorNode;
import com.google.ar.sceneform.math.Vector3;
import com.google.ar.sceneform.rendering.Material;
import com.google.ar.sceneform.rendering.Renderable;
import com.google.ar.sceneform.rendering.ShapeFactory;
import com.google.ar.sceneform.ux.ArFragment;
import com.google.ar.sceneform.ux.TransformableNode;
import com.microsoft.azure.spatialanchors.CloudSpatialAnchor;

class AnchorVisual {
    private final AnchorNode anchorNode;
    private CloudSpatialAnchor cloudAnchor;
    private Material color;
    private Renderable nodeRenderable;

    public AnchorVisual(Anchor localAnchor) {
        anchorNode = new AnchorNode(localAnchor);
    }

    public AnchorNode getAnchorNode() {
        return this.anchorNode;
    }

    public CloudSpatialAnchor getCloudAnchor() {
        return this.cloudAnchor;
    }

    public Anchor getLocalAnchor() {
        return this.anchorNode.getAnchor();
    }

    public void render(ArFragment arFragment) {
        MainThreadContext.runOnUiThread(() -> {
            nodeRenderable = ShapeFactory.makeSphere(0.1f, new Vector3(0.0f, 0.15f, 0.0f), color);
            anchorNode.setRenderable(nodeRenderable);
            anchorNode.setParent(arFragment.getArSceneView().getScene());

            TransformableNode sphere = new TransformableNode(arFragment.getTransformationSystem());
            sphere.setParent(this.anchorNode);
            sphere.setRenderable(this.nodeRenderable);
            sphere.select();
        });
    }

    public void setCloudAnchor(CloudSpatialAnchor cloudAnchor) {
        this.cloudAnchor = cloudAnchor;
    }

    public void setColor(Material material) {
        color = material;

        MainThreadContext.runOnUiThread(() -> {
            anchorNode.setRenderable(null);
            nodeRenderable = ShapeFactory.makeSphere(0.1f, new Vector3(0.0f, 0.15f, 0.0f), color);
            anchorNode.setRenderable(nodeRenderable);
        });
    }

    public void destroy() {
        MainThreadContext.runOnUiThread(() -> {
            anchorNode.setRenderable(null);
            anchorNode.setParent(null);
        });
    }
}
