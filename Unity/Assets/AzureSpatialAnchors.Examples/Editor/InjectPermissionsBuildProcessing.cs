// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    /// <summary>
    /// Modifies the app manifest to request permissions for finding anchors near the user's device.
    /// </summary>
    public class InjectPermissionsBuildProcessing : IPostprocessBuildWithReport
    {
        public int callbackOrder
        {
            get
            {
                return 2;
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
                Debug.Log("No additional sensor permissions configuration necessary.");
            }
        }

        /// <summary>
        /// Configures a HoloLens project with sensor access permissions.
        /// </summary>
        /// <param name="buildOutputPath">The build output path.</param>
        private static void ConfigureHoloLensProject(string buildOutputPath)
        {
            if (!Directory.Exists(buildOutputPath))
            {
                Debug.LogError($"Unable to configure the HoloLens project. Output path does not exist: '{buildOutputPath}'.");
                return;
            }

            // Add the wiFiControl permission to Package.appxmanifest
            string projectPath = Path.Combine(buildOutputPath, Application.productName);
            AddWifiPermissionToUwpPackageManifest(projectPath);
        }

        /// <summary>
        /// Adds the wiFiControl device capability to the AppX manifest for the HoloLens project.
        /// </summary>
        /// <param name="projectInfo">The project information.</param>
        private static void AddWifiPermissionToUwpPackageManifest(string projectPath)
        {
            const string packageManifestFileName = "Package.appxmanifest";
            string packageManifestPath = Path.Combine(projectPath, packageManifestFileName);

            if (!File.Exists(packageManifestPath))
            {
                Debug.LogError($"Unable to locate the {packageManifestFileName} file to patch at: {packageManifestPath}");
                return;
            }

            const string permissionName = "wiFiControl";
            Debug.Log($"Patching {packageManifestPath} with {permissionName} permission");

            XmlDocument packageManifest = new XmlDocument();
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(packageManifest.NameTable);
            namespaceManager.AddNamespace("appx", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
            packageManifest.Load(packageManifestPath);
            XmlNode capabilities = packageManifest.SelectSingleNode("/appx:Package/appx:Capabilities", namespaceManager);
            XmlElement deviceCapability = packageManifest.CreateElement("DeviceCapability", packageManifest.DocumentElement.NamespaceURI);
            if (capabilities.SelectSingleNode($"appx:DeviceCapability[contains(@Name, '{permissionName}')]", namespaceManager) == null)
            {
                XmlAttribute permissionNameAttribute = packageManifest.CreateAttribute("Name");
                permissionNameAttribute.Value = permissionName;
                deviceCapability.Attributes.Append(permissionNameAttribute);
                capabilities.AppendChild(deviceCapability);
            }
            packageManifest.Save(packageManifestPath);
        }

        /// <summary>
        /// Configures an iOS Project with sensor access permissions.
        /// </summary>
        /// <param name="buildOutputPath">Build output path.</param>
        private static void ConfigureIOSProject(string buildOutputPath)
        {
            if (!Directory.Exists(buildOutputPath))
            {
                Debug.LogError($"Unable to configure the iOS project. Output path does not exist: '{buildOutputPath}'.");
                return;
            }

            AddBluetoothUsageDescription(buildOutputPath);
        }

        /// <summary>
        /// Adds the "Bluetooth Always Usage Description" property to Info.plist
        /// </summary>
        /// <param name="buildOutputPath">Build output path.</param>
        private static void AddBluetoothUsageDescription(string buildOutputPath)
        {
#if UNITY_IOS
            string infoPlistPath = Path.Combine(buildOutputPath, "Info.plist");
            var propertyList = new PlistDocument();
            propertyList.ReadFromFile(infoPlistPath);

            // Set both NSBluetoothAlwaysUsageDescription and NSBluetoothPeripheralUsageDescription
            // for compatibility with iOS versions earlier than 13.
            const string bluetoothUsageDescription = "This application uses bluetooth to find nearby beacons.";
            propertyList.root.SetString("NSBluetoothAlwaysUsageDescription", bluetoothUsageDescription);
            propertyList.root.SetString("NSBluetoothPeripheralUsageDescription", bluetoothUsageDescription);

            propertyList.WriteToFile(infoPlistPath);
#endif
        }
    }
}

