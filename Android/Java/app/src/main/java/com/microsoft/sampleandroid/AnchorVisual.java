// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.content.Context;

import com.google.ar.core.Anchor;
import com.google.ar.sceneform.AnchorNode;
import com.google.ar.sceneform.math.Vector3;
import com.google.ar.sceneform.rendering.Color;
import com.google.ar.sceneform.rendering.Material;
import com.google.ar.sceneform.rendering.MaterialFactory;
import com.google.ar.sceneform.rendering.Renderable;
import com.google.ar.sceneform.rendering.ShapeFactory;
import com.google.ar.sceneform.ux.ArFragment;
import com.google.ar.sceneform.ux.TransformableNode;
import com.microsoft.azure.spatialanchors.CloudSpatialAnchor;

import java.util.HashMap;
import java.util.concurrent.CompletableFuture;

class AnchorVisual {
    enum Shape {
        Sphere,
        Cube,
        Cylinder,
    }

    private final AnchorNode anchorNode;
    private TransformableNode transformableNode;
    private CloudSpatialAnchor cloudAnchor;
    private Shape shape = Shape.Sphere;
    private Material material;

    private static HashMap<Integer, CompletableFuture<Material>> solidColorMaterialCache = new HashMap<>();

    public AnchorVisual(ArFragment arFragment, Anchor localAnchor) {
        anchorNode = new AnchorNode(localAnchor);

        transformableNode = new TransformableNode(arFragment.getTransformationSystem());
        transformableNode.getScaleController().setEnabled(false);
        transformableNode.getTranslationController().setEnabled(false);
        transformableNode.getRotationController().setEnabled(false);
        transformableNode.setParent(this.anchorNode);
    }

    public AnchorVisual(ArFragment arFragment, CloudSpatialAnchor cloudAnchor) {
        this(arFragment, cloudAnchor.getLocalAnchor());
        setCloudAnchor(cloudAnchor);
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
            recreateRenderableOnUiThread();
            anchorNode.setParent(arFragment.getArSceneView().getScene());
        });
    }

    public void setCloudAnchor(CloudSpatialAnchor cloudAnchor) {
        this.cloudAnchor = cloudAnchor;
    }

    public synchronized void setColor(Context context, int rgb) {
        CompletableFuture<Material> loadMaterial =
                solidColorMaterialCache.computeIfAbsent(rgb,
                    color -> MaterialFactory.makeOpaqueWithColor(context, new Color(rgb)));
        loadMaterial.thenAccept(this::setMaterial);
    }

    public void setMaterial(Material material) {
        if (this.material != material) {
            this.material = material;
            MainThreadContext.runOnUiThread(this::recreateRenderableOnUiThread);
        }
    }

    public void setShape(Shape shape) {
        if (this.shape != shape) {
            this.shape = shape;
            MainThreadContext.runOnUiThread(this::recreateRenderableOnUiThread);
        }
    }
    public Shape getShape() {
        return shape;
    }

    public void setMovable(boolean movable) {
        MainThreadContext.runOnUiThread(() -> {
            transformableNode.getTranslationController().setEnabled(movable);
            transformableNode.getRotationController().setEnabled(movable);
        });
    }

    public void destroy() {
        MainThreadContext.runOnUiThread(() -> {
            anchorNode.setRenderable(null);
            anchorNode.setParent(null);
            Anchor localAnchor =  anchorNode.getAnchor();
            if (localAnchor != null) {
                anchorNode.setAnchor(null);
                localAnchor.detach();
            }
        });
    }

    private void recreateRenderableOnUiThread() {
        if (material != null) {
            Renderable renderable;
            switch (shape) {
                case Sphere:
                    renderable = ShapeFactory.makeSphere(
                            0.1f,
                            new Vector3(0.0f, 0.1f, 0.0f),
                            material);
                    break;
                case Cube:
                    renderable = ShapeFactory.makeCube(
                            new Vector3(0.161f, 0.161f, 0.161f),
                            new Vector3(0.0f, 0.0805f, 0.0f),
                            material);
                    break;
                case Cylinder:
                    renderable = ShapeFactory.makeCylinder(
                            0.0874f,
                            0.175f,
                            new Vector3(0.0f, 0.0875f, 0.0f),
                            material);
                    break;
                default:
                    throw new IllegalStateException("Invalid shape");
            }
            transformableNode.setRenderable(renderable);
        }
    }
}
