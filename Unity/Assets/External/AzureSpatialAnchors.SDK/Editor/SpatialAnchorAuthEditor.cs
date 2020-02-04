// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity
{
    public abstract class SpatialAnchorAuthEditor : Editor
    {
        protected abstract AuthenticationMode GetAuthMode();

        public override void OnInspectorGUI()
        {
            // Start the update
            serializedObject.Update();

            // Draw based on auth mode
            switch (GetAuthMode())
            {
                case AuthenticationMode.ApiKey:
                    DrawPropertiesExcluding(serializedObject, new string[] { "clientId", "tenantId" });
                    break;
                case AuthenticationMode.AAD:
                    DrawPropertiesExcluding(serializedObject, new string[] { "spatialAnchorsAccountId", "spatialAnchorsAccountKey" });
                    EditorGUILayout.HelpBox("IMPORTANT: Extra steps are required to enable AAD. Please see AzureSpatialAnchorsUnityPluginReadme in the SDK folder for more information.", MessageType.Warning);
                    break;
                default:
                    break;
            }

            // Apply modifications
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(SpatialAnchorConfig))]
    public class SpatialAnchorConfigEditor : SpatialAnchorAuthEditor
    {
        protected override AuthenticationMode GetAuthMode()
        {
            return ((SpatialAnchorConfig)target).AuthenticationMode;
        }
    }
    [CustomEditor(typeof(SpatialAnchorManager))]
    public class SpatialAnchorManagerEditor : SpatialAnchorAuthEditor
    {
        protected override AuthenticationMode GetAuthMode()
        {
            return ((SpatialAnchorManager)target).AuthenticationMode;
        }
    }
}
