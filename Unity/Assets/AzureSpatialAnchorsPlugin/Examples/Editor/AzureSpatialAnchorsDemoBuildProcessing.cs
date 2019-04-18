using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Microsoft.Azure.SpatialAnchors.Unity.Samples
{
    public class AzureSpatialAnchorsDemoBuildProcessing : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            AzureSpatialAnchorsDemoConfiguration demoConfig = Resources.Load<AzureSpatialAnchorsDemoConfiguration>("AzureSpatialAnchorsDemoConfig");
            if (string.IsNullOrWhiteSpace(demoConfig.SpatialAnchorsAccountId) || string.IsNullOrWhiteSpace(demoConfig.SpatialAnchorsAccountKey))
            {
                Debug.LogWarning(@"Missing security values in AzureSpatialAnchorsPlugin\Examples\Resources\AzureSpatialAnchorsDemoConfig");
            }
        }
    }
}
