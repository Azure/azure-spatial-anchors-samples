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

public class AnchorVisual {

    private Renderable nodeRenderable;
    private Anchor localAnchor;
    public AnchorNode anchorNode;
    public CloudSpatialAnchor cloudAnchor;
    public String identifier;
    private Material color;

    public AnchorVisual() {
        anchorNode = new AnchorNode();
    }

    public Anchor getLocalAnchor() {
        return localAnchor;
    }

    public void setLocalAnchor(Anchor value) {
        localAnchor = value;
        anchorNode.setAnchor(value);
    }

    public void render(ArFragment arFragment){

        nodeRenderable = ShapeFactory.makeSphere(0.1f, new Vector3(0.0f, 0.15f, 0.0f), color);
        anchorNode.setRenderable(nodeRenderable);
        anchorNode.setParent(arFragment.getArSceneView().getScene());

        TransformableNode sphere = new TransformableNode(arFragment.getTransformationSystem());
        sphere.setParent(this.anchorNode);
        sphere.setRenderable(this.nodeRenderable);
        sphere.select();
    }

    public void setColor(Material material){
        color = material;
        anchorNode.setRenderable(null);
        nodeRenderable = ShapeFactory.makeSphere(0.1f, new Vector3(0.0f, 0.15f, 0.0f), color);
        anchorNode.setRenderable(nodeRenderable);
    }

    public void destroy()
    {
        anchorNode.setRenderable(null);
        anchorNode.setParent(null);
    }
}
