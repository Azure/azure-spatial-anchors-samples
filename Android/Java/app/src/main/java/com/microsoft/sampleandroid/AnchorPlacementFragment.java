// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.content.Context;
import android.os.Bundle;
import android.support.annotation.NonNull;
import android.support.annotation.Nullable;
import android.support.v4.app.Fragment;
import android.support.v4.app.FragmentActivity;
import android.view.LayoutInflater;
import android.view.MotionEvent;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.RadioGroup;
import android.widget.TextView;

import com.google.ar.core.Anchor;
import com.google.ar.core.HitResult;
import com.google.ar.core.Plane;
import com.google.ar.sceneform.ux.ArFragment;

public class AnchorPlacementFragment extends Fragment {
    private ArFragment arFragment;
    private AnchorVisual visual;
    private AnchorPlacementListener listener;

    private TextView hintText;
    private Button confirmPlacementButton;
    private RadioGroup shapeSelection;

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, @Nullable Bundle savedInstanceState) {
        return inflater.inflate(R.layout.coarse_reloc_anchor_placement, container, false);
    }

    @Override
    public void onAttach(Context context) {
        super.onAttach(context);

        FragmentActivity activity = (FragmentActivity)context;
        arFragment = (ArFragment)activity.getSupportFragmentManager().findFragmentById(R.id.ar_fragment);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);

        confirmPlacementButton = view.findViewById(R.id.confirm_placement);
        hintText = view.findViewById(R.id.hint_text);
        shapeSelection = view.findViewById(R.id.shape_selection);
    }

    @Override
    public void onStart() {
        super.onStart();

        confirmPlacementButton.setEnabled(false);
        arFragment.setOnTapArPlaneListener(this::onTapArPlaneListener);
        confirmPlacementButton.setOnClickListener(this::onConfirmPlacementClicked);
        shapeSelection.setOnCheckedChangeListener(this::onShapeSelected);
    }

    public void setListener(AnchorPlacementListener listener) {
        this.listener = listener;
    }

    @Override
    public void onStop() {
        arFragment.setOnTapArPlaneListener(null);

        if (visual != null) {
            visual.destroy();
            visual = null;
        }

        super.onStop();
    }

    private void onTapArPlaneListener(HitResult hitResult, Plane plane, MotionEvent motionEvent) {
        if (visual != null) {
            visual.destroy();
            visual = null;
        }

        Anchor localAnchor = hitResult.createAnchor();
        visual = new AnchorVisual(arFragment, localAnchor);
        visual.setMovable(true);
        visual.setShape(getSelectedShape());
        visual.setColor(arFragment.getContext(), android.graphics.Color.YELLOW);
        visual.render(arFragment);

        hintText.setText(R.string.hint_adjust_anchor);

        confirmPlacementButton.setEnabled(true);
    }

    private void onShapeSelected(RadioGroup radioGroup, int selectedId) {
        if (visual == null) {
            return;
        }

        visual.setShape(getSelectedShape());
    }

    private AnchorVisual.Shape getSelectedShape() {
        int selectedId = shapeSelection.getCheckedRadioButtonId();
        if (selectedId == R.id.sphere_shape) {
            return AnchorVisual.Shape.Sphere;
        } else if (selectedId == R.id.cylinder_shape) {
            return AnchorVisual.Shape.Cylinder;
        } else if (selectedId == R.id.cube_shape) {
            return AnchorVisual.Shape.Cube;
        }

        throw new IllegalStateException("Invalid selected shape");
    }

    private void onConfirmPlacementClicked(View view) {
        if (visual != null) {
            AnchorVisual placedAnchor = visual;
            visual = null;
            placedAnchor.setMovable(false);
            if (listener != null) {
                listener.onAnchorPlaced(placedAnchor);
            }
        }
    }
}
