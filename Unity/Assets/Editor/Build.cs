// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
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

        /// <summary>
        /// Generates a Player solution using the default configuration.
        /// </summary>
        public static void GenerateAndroidPlayerSolution()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.androidBuildType = AndroidBuildType.Release;
            EditorUserBuildSettings.androidETC2Fallback = AndroidETC2Fallback.Quality32Bit;

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
            {
                locationPathName = "Android\\UnityAndroidSample.apk",
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
                scenes = EditorBuildSettings.scenes
                         .Where(scene => scene.enabled)
                         .Select(scene => scene.path)
                         .ToArray(),
            };

            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }

        /// <summary>
        /// Generates a Player solution using the default configuration.
        /// </summary>
        public static void GenerateIOsPlayerSolution()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.Mono2x);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            EditorUserBuildSettings.iOSBuildConfigType = iOSBuildType.Release;

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
            {
                locationPathName = "iOS\\Xcode",
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
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