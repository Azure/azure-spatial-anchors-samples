// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.using System;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class Export
{
    /// <summary>
    /// Exports the unity package for Azure Spatial Anchors.
    /// </summary>
    public static void ExportAzureSpatialAnchorsUnityPackage()
    {
        string[] assetPaths = new string[]
        {
            $"Assets{Path.DirectorySeparatorChar}android-logos",
            $"Assets{Path.DirectorySeparatorChar}AzureSpatialAnchors.Examples",
            $"Assets{Path.DirectorySeparatorChar}AzureSpatialAnchors.SDK",
            $"Assets{Path.DirectorySeparatorChar}logos",
            $"Assets{Path.DirectorySeparatorChar}Plugins"
        };

        foreach (string path in assetPaths)
        {
            if (!System.IO.Directory.Exists(path))
            {
                throw new InvalidOperationException($"The path does not exist: {path}");
            }
        }

        AssetDatabase.ExportPackage(assetPaths, "AzureSpatialAnchors.unitypackage", ExportPackageOptions.Recurse);
    }

    /*
    /// <summary>
    /// Adds a menu item under Assets to export Azure Spatial Anchors package
    /// </summary>
    [MenuItem("Assets/Export Azure Spatial Anchors")]
    public static void DoExport()
    {
        ExportAzureSpatialAnchorsUnityPackage();
        Debug.Log("Azure Spatial Anchor unity package successfully generated.");
    }
    */
}
