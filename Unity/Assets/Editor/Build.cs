// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.using System.Linq;
using UnityEditor;
using System.Linq;

namespace Microsoft.Azure.SpatialAnchors.Unity
{
    public static class Build
    {
        /// <summary>
        /// Generates a Player solution using the default configuration.
        /// </summary>
        public static void GenerateHoloLensPlayerSolution()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WSA, BuildTarget.WSAPlayer);
            EditorUserBuildSettings.SetPlatformSettings("WindowsStoreApps", "CopyReferences", "true");
            EditorUserBuildSettings.SetPlatformSettings("WindowsStoreApps", "CopyPDBFiles", "false");

            EditorUserBuildSettings.wsaUWPVisualStudioVersion = "Visual Studio 2017";
            EditorUserBuildSettings.wsaUWPSDK = "10.0.18362.0";
            EditorUserBuildSettings.wsaSubtarget = WSASubtarget.HoloLens;

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.WSA, ScriptingImplementation.IL2CPP);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
            {
                locationPathName = "UWP",
                target = BuildTarget.WSAPlayer,
                targetGroup = BuildTargetGroup.WSA,
                options = BuildOptions.None,
                scenes = EditorBuildSettings.scenes
                         .Where(scene => scene.enabled)
                         .Select(scene => scene.path)
                         .ToArray(),
            };

            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }
    }
}