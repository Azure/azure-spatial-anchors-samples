// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
package com.microsoft.sampleandroid;

import android.os.Bundle;
import android.support.annotation.NonNull;
import android.support.annotation.Nullable;
import android.support.v4.app.Fragment;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;

public class ActionSelectionFragment extends Fragment {

    private Button deleteAnchorsButton;

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, @Nullable Bundle savedInstanceState) {
        return inflater.inflate(R.layout.coarse_reloc_action_selection, container, false);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        deleteAnchorsButton = view.findViewById(R.id.delete_nearby_anchors);
    }

    public void enableDeleteButton() {
        if (deleteAnchorsButton != null) {
            deleteAnchorsButton.setText(R.string.delete_nearby_anchors);
            deleteAnchorsButton.setEnabled(true);
        }
    }

    public void disableDeleteButton() {
        if (deleteAnchorsButton != null) {
            deleteAnchorsButton.setEnabled(false);
            deleteAnchorsButton.setText(R.string.delete_nearby_anchors_in_progress);
        }
    }

}
