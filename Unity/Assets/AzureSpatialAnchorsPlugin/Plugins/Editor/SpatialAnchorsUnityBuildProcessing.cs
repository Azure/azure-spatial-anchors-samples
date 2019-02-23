// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Manages build processing to enable Azure Spatial Anchors to build as easily as possible.
/// </summary>
public class SpatialAnchorsUnityBuildProcessing : IActiveBuildTargetChanged, IPostprocessBuildWithReport
{
    private const string ARCorePluginFolder = "Assets\\GoogleARCore";
    private const string ARKitPluginFolder = "Assets\\UnityARKitPlugin";
    private const string AzureSpatialAnchorsPackage = "Microsoft.Azure.SpatialAnchors.WinCPP";
    private const string AzureSpatialAnchorsRedistPackage = "Microsoft.Azure.SpatialAnchors.WinCPP.Redist";
    private const string UnityRelativePodFilePath = "Assets/AzureSpatialAnchorsPlugin/Plugins/iOS/Podfile";
    private const string UnityRelativeNuGetConfigFilePath = @"Assets\\AzureSpatialAnchorsPlugin\\Plugins\\HoloLens\\NuGet.Config";
    private const string UnityRelativePackageVersionFilePath = @"Assets\\AzureSpatialAnchorsPlugin\\Plugins\\HoloLens\\version.txt";

    public int callbackOrder
    {
        get
        {
            return 1;
        }
    }

    /// <summary>
    /// Currently the ARCore Plugin does not build when the build target is set to UWP.
    /// This script will hide the ARCore plugin so long as the current build target is not Android
    /// </summary>
    /// <param name="previousTarget"></param>
    /// <param name="newTarget"></param>
    public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
    {
        string ARCoreAssetsDir = Path.Combine(Directory.GetCurrentDirectory(), ARCorePluginFolder);
        string ARKitAssetsDir = Path.Combine(Directory.GetCurrentDirectory(), ARKitPluginFolder);
        bool NeedRefresh = false;
        if (newTarget == BuildTarget.Android)
        {
            if (Directory.Exists(ARCoreAssetsDir) == false)
            {
                Debug.LogError($"Please put the ARCore 1.5 plugin in {ARCoreAssetsDir}");
            }
            else
            {
                Debug.Log("Enabling the ARCore SDK");
                ClearHiddenAttributeOnFileOrFolder(ARCoreAssetsDir);
                NeedRefresh = true;
            }
        }
        else if (Directory.Exists(ARCoreAssetsDir))
        {
            Debug.Log("Disabling the ARCore SDK");
            SetHiddenAttributeOnFileOrFolder(ARCoreAssetsDir);
            NeedRefresh |= (previousTarget == BuildTarget.Android);
        }

        if (newTarget == BuildTarget.iOS)
        {
            if (Directory.Exists(ARKitAssetsDir) == false)
            {
                Debug.LogError($"Please put the ARKit plugin in {ARKitAssetsDir}");
            }
            else
            {
                Debug.Log("Enabling the ARKit SDK");
                ClearHiddenAttributeOnFileOrFolder(ARKitAssetsDir);
                NeedRefresh = true;
            }
        }
        else if (Directory.Exists(ARKitAssetsDir))
        {
            Debug.Log("Disabling the ARKit SDK");
            SetHiddenAttributeOnFileOrFolder(ARKitAssetsDir);
            NeedRefresh |= (previousTarget == BuildTarget.iOS);
        }

        if (NeedRefresh)
        {
            Debug.Log("Rescanning scripts, errors before this line may be benign");
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// Called after a build completes.
    /// </summary>
    /// <param name="report">A BuildReport containing information about the build, such as the target platform and output path.</param>
    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.result == BuildResult.Failed
            || report.summary.result == BuildResult.Cancelled)
        {
            return;
        }

        if (report.summary.platform == BuildTarget.iOS)
        {
            Debug.Log($"Configuring iOS project at '{report.summary.outputPath}'.");
            ConfigureIOSProject(report.summary.outputPath);
        }
        else if (report.summary.platform == BuildTarget.WSAPlayer
            && (report.summary.platformGroup == BuildTargetGroup.WSA))
        {
            Debug.Log($"Configuring HoloLens project at '{report.summary.outputPath}'.");
            ConfigureHoloLensProject(report.summary.outputPath);
        }
        else
        {
            Debug.Log("No additional configuration necessary for Spatial Anchors.");
        }
    }

    /// <summary>
    /// Configures a HoloLens project for Spatial Anchors.
    /// </summary>
    /// <param name="buildOutputPath">The build output path.</param>
    private static void ConfigureHoloLensProject(string buildOutputPath)
    {
        if (!Directory.Exists(buildOutputPath))
        {
            Debug.LogWarning($"Unable to configure the HoloLens project. Output path does not exist: '{buildOutputPath}'.");
            return;
        }

        // Copy the NuGet.config file to the output path.
        CopyNuGetConfigToOutputPath(buildOutputPath);

        // Inject a NuGet package dependency on Microsoft.Azure.SpatialAnchors.WinCPP into the project.
        HoloLensProjectInfo projectInfo = HoloLensProjectInfo.Create(Application.productName, buildOutputPath);
        InjectHoloLensPackageDependency(projectInfo);
    }

    /// <summary>
    /// Configures an iOS Project for Spatial Anchors.
    /// </summary>
    /// <param name="buildOutputPath">Build output path.</param>
    private static void ConfigureIOSProject(string buildOutputPath)
    {
        if (!Directory.Exists(buildOutputPath))
        {
            Debug.LogWarning($"Unable to configure the iOS project. Output path does not exist: '{buildOutputPath}'.");
            return;
        }

        string podFileName = Path.GetFileName(UnityRelativePodFilePath);

        string outputPodFilePath = Path.Combine(buildOutputPath, podFileName);

        if (!File.Exists(outputPodFilePath))
        {
            string inputPodFilePath = Path.Combine(Directory.GetCurrentDirectory(), UnityRelativePodFilePath);
            File.Copy(inputPodFilePath, outputPodFilePath);
            Debug.Log($"Spatial Anchors pod file copied to project path: '{outputPodFilePath}'.");
        }
        else
        {
            Debug.Log($"Spatial Anchors pod file already exists.");
        }
    }

    /// <summary>
    /// Copies the NuGet.Config file to the output path for HoloLens.
    /// </summary>
    /// <param name="buildOutputPath">The build output path.</param>
    private static void CopyNuGetConfigToOutputPath(string buildOutputPath)
    {
        string nugetConfigFileName = Path.GetFileName(UnityRelativeNuGetConfigFilePath);

        string outputNugetConfigFilePath = Path.Combine(buildOutputPath, nugetConfigFileName);

        if (!File.Exists(outputNugetConfigFilePath))
        {
            string inputNugetFilePath = Path.Combine(Directory.GetCurrentDirectory(), UnityRelativeNuGetConfigFilePath);

            if (!File.Exists(inputNugetFilePath))
            {
                Debug.LogWarning($"Spatial Anchors NuGet.config file could not be found at {inputNugetFilePath}. Package restoration may not work.");
            }

            File.Copy(inputNugetFilePath, outputNugetConfigFilePath);
            Debug.Log($"Spatial Anchors NuGet.Config file copied to project path: '{outputNugetConfigFilePath}'.");
        }
        else
        {
            Debug.Log("Spatial Anchors NuGet.Config file already exists.");
        }
    }

    /// <summary>
    /// Injects a NuGet package dependency on Microsoft.Azure.SpatialAnchors.WinCPP into the project.
    /// </summary>
    /// <param name="projectInfo">The project information.</param>
    private static void InjectHoloLensPackageDependency(HoloLensProjectInfo projectInfo)
    {
        if (projectInfo.SpatialAnchorsSdkVersion == null)
        {
            Debug.LogWarning("Unable to determine Spatial Anchors SDK version.");
            return;
        }

        switch (projectInfo.ProjectType)
        {
            case MsBuildProjectType.CSProj:
                InjectHoloLensCSProjPackageDependency(projectInfo);
                break;

            case MsBuildProjectType.VCXProj:
                InjectHoloLensVCXProjPackageDependency(projectInfo);
                break;

            default:
                Debug.LogWarning($"Unsupported HoloLens project type. Unable to add a package reference for {AzureSpatialAnchorsPackage}.");
                return;
        }
    }

    private static void InjectHoloLensCSProjPackageDependency(HoloLensProjectInfo projectInfo)
    {
        string projectJsonFile = Path.Combine(projectInfo.BuildOutputPath, projectInfo.ProjectName, "project.json");

        if (!File.Exists(projectJsonFile))
        {
            Debug.LogWarning($"Unable to locate the project.json file to patch at: {projectJsonFile}");
            return;
        }

        Debug.Log($"Patching {projectJsonFile} with {AzureSpatialAnchorsPackage} {projectInfo.SpatialAnchorsSdkVersion}");
        PatchProjectJsonFile(projectJsonFile, projectInfo);
    }

    private static void InjectHoloLensVCXProjPackageDependency(HoloLensProjectInfo projectInfo)
    {
        if (!TryCreatePackagesConfig(projectInfo))
        {
            // We assume we've already injected the necessary dependencies and an update\append build is being performed.
            return;
        }

        PatchVCXProjFile(projectInfo);

        PatchVCXProjFiltersFile(projectInfo);
    }

    private static void PatchProjectJsonFile(string projectJsonFilePath, HoloLensProjectInfo projectInfo)
    {
        if (!File.Exists(projectJsonFilePath))
        {
            Debug.LogWarning($"Can't find the specified project.json file: '{projectJsonFilePath}'.");
            return;
        }

        string[] lines = File.ReadAllLines(projectJsonFilePath);

        // Looks for:         "dependencies": {
        Regex dependenciesStart = new Regex(@"\s*""dependencies"":\s*\{\s*", RegexOptions.Compiled);

        // Looks for:         },
        Regex dependenciesStop = new Regex(@"\s*\}\s*,\s*", RegexOptions.Compiled);

        // Looks for:         "Package.Name": "0.0.0",
        Regex dependencyLine = new Regex(@"""(?<dep>.+)""\s*:\s*""(?<version>.+)""\s*,?\s*", RegexOptions.Compiled);

        Dictionary<string, string> dependencies = new Dictionary<string, string>(StringComparer.Ordinal);
        bool trackingDependencies = false;
        foreach (string line in lines)
        {
            if (trackingDependencies)
            {
                if (dependenciesStop.IsMatch(line))
                {
                    break;
                }

                var match = dependencyLine.Match(line);

                if (!match.Success)
                {
                    Debug.LogWarning($"Unable to understand the project.json file: {line}");
                    return;
                }

                dependencies.Add(match.Groups["dep"].Value, match.Groups["version"].Value);
            }

            if (dependenciesStart.IsMatch(line))
            {
                trackingDependencies = true;
            }
        }

        if (dependencies.Count == 0)
        {
            Debug.LogWarning("Unable to understand the project.json file. No depdendencies were found.");
            return;
        }

        if (dependencies.ContainsKey(AzureSpatialAnchorsPackage))
        {
            // The package is already present. Does it need updating?
            string existingPackageVersion = dependencies[AzureSpatialAnchorsPackage];

            if (existingPackageVersion == projectInfo.SpatialAnchorsSdkVersion)
            {
                // Nothing to do since we're up to date.
                return;
            }
            else
            {
                // Update the version in the file.
                Debug.Log($"Updating the version of {AzureSpatialAnchorsPackage} in the project.json file.");
                dependencies[AzureSpatialAnchorsPackage] = projectInfo.SpatialAnchorsSdkVersion;
            }
        }
        else
        {
            // Add the full dependency.
            Debug.Log($"Adding the {AzureSpatialAnchorsPackage} {projectInfo.SpatialAnchorsSdkVersion} dependency to the project.json file.");
            dependencies.Add(AzureSpatialAnchorsPackage, projectInfo.SpatialAnchorsSdkVersion);
        }

        trackingDependencies = false;
        List<string> newLines = new List<string>();
        foreach (string line in lines)
        {
            if (trackingDependencies)
            {
                if (dependenciesStop.IsMatch(line))
                {
                    newLines.Add(line);
                    trackingDependencies = false;
                }
            }
            else if (dependenciesStart.IsMatch(line))
            {
                newLines.Add(line);
                foreach (var dependency in dependencies.OrderBy(d => d.Key))
                {
                    newLines.Add($"    \"{dependency.Key}\": \"{dependency.Value}\",");
                }
                trackingDependencies = true;
            }
            else
            {
                newLines.Add(line);
            }
        }

        File.WriteAllLines(projectJsonFilePath, newLines);
    }

    private static bool TryCreatePackagesConfig(HoloLensProjectInfo projectInfo)
    {
        string packagesConfigFilePath = Path.Combine(projectInfo.BuildOutputPath, projectInfo.ProjectName, "packages.config");

        if (File.Exists(packagesConfigFilePath))
        {
            // It already exists. No need to re-create it.
            Debug.Log("Spatial Anchors packages.config file already exists.");
            return false;
        }

        Debug.Log($"Creating Spatial Anchors packages.config file: '{packagesConfigFilePath}'.");

        using (FileStream fileStream = new FileStream(packagesConfigFilePath, FileMode.Create))
        using (XmlWriter xmlWriter = XmlWriter.Create(fileStream, new XmlWriterSettings { Encoding = System.Text.Encoding.UTF8, Indent = true }))
        {
            xmlWriter.WriteStartDocument();

            // Start packages element
            xmlWriter.WriteStartElement("packages");

            xmlWriter.WriteStartElement("package");
            xmlWriter.WriteAttributeString("id", AzureSpatialAnchorsPackage);
            xmlWriter.WriteAttributeString("version", projectInfo.SpatialAnchorsSdkVersion);
            xmlWriter.WriteAttributeString("targetFramework", "native");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("package");
            xmlWriter.WriteAttributeString("id", AzureSpatialAnchorsRedistPackage);
            xmlWriter.WriteAttributeString("version", projectInfo.SpatialAnchorsSdkVersion);
            xmlWriter.WriteAttributeString("targetFramework", "native");
            xmlWriter.WriteEndElement();

            // End packages element
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndDocument();
        }

        return true;
    }

    private static void PatchVCXProjFile(HoloLensProjectInfo projectInfo)
    {
        string vcxProjFilePath = Path.Combine(projectInfo.BuildOutputPath, projectInfo.ProjectName, $"{projectInfo.ProjectName}.vcxproj");

        Debug.Log($"Patching {vcxProjFilePath} with {AzureSpatialAnchorsPackage} {projectInfo.SpatialAnchorsSdkVersion}");

        if (!File.Exists(vcxProjFilePath))
        {
            Debug.LogWarning($"The project is not a native MSBuild project: {projectInfo.BuildOutputPath}");
            return;
        }

        XDocument vcxProjDoc = XDocument.Load(vcxProjFilePath);
        string ns = vcxProjDoc.Root.Name.NamespaceName;
        XElement fileIncludeItemGroup = vcxProjDoc.Root.Elements()
            .Where(el => el.Name.LocalName == "ItemGroup")
            .FirstOrDefault(el => el.Elements().Any(el2 => el2.Name.LocalName == "None"));

        if (fileIncludeItemGroup == null)
        {
            Debug.LogWarning($"Invalid vcxproj file: {vcxProjFilePath}");
            return;
        }

        // Add the packages.config file reference.
        XElement packagesConfigRefElement = new XElement(XName.Get("None", ns));
        packagesConfigRefElement.Add(new XAttribute("Include", "packages.config"));
        fileIncludeItemGroup.Add(packagesConfigRefElement);

        XElement importGroupExtensionTargetsElement = vcxProjDoc.Root.Elements()
            .Where(el => el.Name.LocalName == "ImportGroup")
            .FirstOrDefault(el => el.Attributes().Any(a => a.Name.LocalName == "Label" && a.Value == "ExtensionTargets"));

        if (importGroupExtensionTargetsElement == null)
        {
            Debug.LogWarning($"Invalid vcxproj file: {vcxProjFilePath}");
            return;
        }

        // Add the targets imports for the packages.
        string[] packages = new[] { AzureSpatialAnchorsRedistPackage, AzureSpatialAnchorsPackage };
        foreach (string packageName in packages)
        {
            XElement importElement = new XElement(XName.Get("Import", ns));
            string targetPath = $@"..\packages\{packageName}.{projectInfo.SpatialAnchorsSdkVersion}\build\native\{packageName}.targets";
            importElement.Add(new XAttribute("Project", targetPath));
            importElement.Add(new XAttribute("Condition", $"Exists('{targetPath}')"));

            importGroupExtensionTargetsElement.Add(importElement);
        }

        // Add a target to check that a restore has been performed.
        XElement nugetCheckTargetElement = new XElement(XName.Get("Target", ns),
            new XElement(XName.Get("PropertyGroup", ns),
                new XElement(XName.Get("ErrorText", ns), "This project references NuGet package(s) that are missing on this computer. Use NuGet Package Restore to download them.  For more information, see http://go.microsoft.com/fwlink/?LinkID=322105. The missing file is {0}.")));
        nugetCheckTargetElement.Add(new XAttribute("Name", "EnsureNuGetPackageBuildImports"));
        nugetCheckTargetElement.Add(new XAttribute("BeforeTargets", "PrepareForBuild"));

        foreach (string packageName in packages)
        {
            XElement errorElement = new XElement(XName.Get("Error", ns));
            string targetPath = $@"..\packages\{packageName}.{projectInfo.SpatialAnchorsSdkVersion}\build\native\{packageName}.targets";
            errorElement.Add(new XAttribute("Condition", $"!Exists('{targetPath}')"));
            errorElement.Add(new XAttribute("Text", $"$([System.String]::Format('$(ErrorText)', '{targetPath}'))"));

            nugetCheckTargetElement.Add(errorElement);
        }

        vcxProjDoc.Root.Add(nugetCheckTargetElement);

        // Update the contents of the vcxproj
        using (XmlWriter xmlWriter = XmlWriter.Create(vcxProjFilePath, new XmlWriterSettings { Indent = true }))
        {
            vcxProjDoc.Save(xmlWriter);
        }
    }

    private static void PatchVCXProjFiltersFile(HoloLensProjectInfo projectInfo)
    {
        string vcxProjFiltersFilePath = Path.Combine(projectInfo.BuildOutputPath, projectInfo.ProjectName, $"{projectInfo.ProjectName}.vcxproj.filters");

        Debug.Log($"Patching {vcxProjFiltersFilePath} with {AzureSpatialAnchorsPackage} {projectInfo.SpatialAnchorsSdkVersion}");

        if (!File.Exists(vcxProjFiltersFilePath))
        {
            Debug.LogWarning($"The project is not a native MSBuild project: {projectInfo.BuildOutputPath}");
            return;
        }

        XDocument vcxProjDoc = XDocument.Load(vcxProjFiltersFilePath);
        string ns = vcxProjDoc.Root.Name.NamespaceName;

        // Add the packages.config file reference.
        XElement noneElement = new XElement(XName.Get("None", ns));
        noneElement.Add(new XAttribute("Include", "packages.config"));
        vcxProjDoc.Root.Add(new XElement(XName.Get("ItemGroup", ns), noneElement));

        // Update the contents of the vcxproj.filters
        using (XmlWriter xmlWriter = XmlWriter.Create(vcxProjFiltersFilePath, new XmlWriterSettings { Indent = true }))
        {
            vcxProjDoc.Save(xmlWriter);
        }
    }

    private void SetHiddenAttributeOnFileOrFolder(string FileOrFolderName)
    {
        FileAttributes fileAttributes = File.GetAttributes(FileOrFolderName);
        File.SetAttributes(FileOrFolderName, fileAttributes | FileAttributes.Hidden);
    }

    private void ClearHiddenAttributeOnFileOrFolder(string FileOrFolderName)
    {
        FileAttributes fileAttributes = File.GetAttributes(FileOrFolderName);
        File.SetAttributes(FileOrFolderName, fileAttributes & ~FileAttributes.Hidden);
    }

    private enum MsBuildProjectType
    {
        Unknown,
        CSProj,
        VCXProj
    }

    private class HoloLensProjectInfo
    {
        public string BuildOutputPath { get; private set; }

        public string ProjectName { get; private set; }

        public string SpatialAnchorsSdkVersion { get; private set; }

        public MsBuildProjectType ProjectType { get; private set; }

        private HoloLensProjectInfo()
        {
        }

        public static HoloLensProjectInfo Create(string projectName, string buildOutputPath)
        {
            MsBuildProjectType projectType = DetermineHoloLensProjectType(projectName, buildOutputPath);
            string spatialAnchorsSdkVersion = GetPackageVersionFromVersionFile();

            return new HoloLensProjectInfo
            {
                BuildOutputPath = buildOutputPath,
                ProjectName = projectName,
                ProjectType = projectType,
                SpatialAnchorsSdkVersion = spatialAnchorsSdkVersion
            };
        }

        private static MsBuildProjectType DetermineHoloLensProjectType(string projectName, string buildOutputPath)
        {
            string csprojFilePath = Path.Combine(buildOutputPath, projectName, $"{projectName}.csproj");

            if (File.Exists(csprojFilePath))
            {
                return MsBuildProjectType.CSProj;
            }

            string vcxProjFilePath = Path.Combine(buildOutputPath, projectName, $"{projectName}.vcxproj");

            if (File.Exists(vcxProjFilePath))
            {
                return MsBuildProjectType.VCXProj;
            }

            return MsBuildProjectType.Unknown;
        }

        private static string GetPackageVersionFromVersionFile()
        {
            string versionFilePath = Path.Combine(Directory.GetCurrentDirectory(), UnityRelativePackageVersionFilePath);

            if (!File.Exists(versionFilePath))
            {
                return null;
            }

            return File.ReadAllText(versionFilePath).Trim();
        }
    }
}
