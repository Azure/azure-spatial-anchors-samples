// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    public class SceneSelector : MonoBehaviour
    {
        public Text SelectedSceneNameText;

        List<int> SceneBuildIndices = new List<int>();
        private int _SceneIndex = -1;
        int SceneIndex
        {
            get
            {
                return _SceneIndex;
            }
            set
            {
                if (_SceneIndex != value)
                {
                    _SceneIndex = value;
                    UpdateSceneText();
                }
            }
        }
        
#pragma warning disable CS1998 // Conditional compile statements are removing await
        async void Start()
#pragma warning restore CS1998
        {
            if (SelectedSceneNameText == null)
            {
                Debug.Log("Missing text field");
                return;
            }

#if !UNITY_EDITOR && (UNITY_WSA || WINDOWS_UWP)
            // Ensure that the device is running a suported build with the spatialperception capability declared.
            bool accessGranted = false;
            try
            {
                Windows.Perception.Spatial.SpatialPerceptionAccessStatus accessStatus = await Windows.Perception.Spatial.SpatialAnchorExporter.RequestAccessAsync();
                accessGranted = (accessStatus == Windows.Perception.Spatial.SpatialPerceptionAccessStatus.Allowed);
            }
            catch {}

            if (!accessGranted)
            {
                Button[] buttons = GetComponentsInChildren<Button>();
                foreach (Button b in buttons)
                {
                    b.gameObject.SetActive(false);
                }

                SelectedSceneNameText.resizeTextForBestFit = true;
                SelectedSceneNameText.verticalOverflow = VerticalWrapMode.Overflow;
                SelectedSceneNameText.text = "Access denied to spatial anchor exporter.  Ensure your OS build is up to date and the spatialperception capability is set.";
                return;
            }
#endif

            GetScenes();

            if (SceneBuildIndices.Count == 0)
            {
                SelectedSceneNameText.text = "No scenes";
                Debug.Log("Not enough scenes in the build");
                return;
            }

            SceneIndex = 0;
        }

        void UpdateSceneText()
        {
            // Unity's scene.name function only works after a scene is loaded
            // so we have to do a little work to get a friendly scene name
            if (SceneIndex >= 0 && SceneIndex < SceneBuildIndices.Count)
            {
                int selected = SceneBuildIndices[SceneIndex];

                // this gets us a string like /Assets/AzureSpatialAnchorsPlugin/Examples/Scenes/AzureSpatialAnchorsSceneName.Unity
                string path = SceneUtility.GetScenePathByBuildIndex(selected);
                // Trim off /Assets/AzureSpatialAnchorsPlugin/Examples/Scenes/AzureSpatialAnchors
                path = path.Substring(path.LastIndexOf('/') + "AzureSpatialAnchors".Length + 1);
                // Trim off .Unity
                path = path.Substring(0, path.LastIndexOf('.'));
                SelectedSceneNameText.text = path;
            }
            else
            {
                SelectedSceneNameText.text = $"Invalid scene id {SceneIndex}";
            }
        }

        void GetScenes()
        {

            Scene currentScene = SceneManager.GetActiveScene();

            for (int index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(index);
                Scene s = SceneManager.GetSceneByPath(path);
                if (s.name == currentScene.name)
                {
                    continue;
                }

                SceneBuildIndices.Add(index);
            }
        }

        public void Next()
        {
            if (SceneBuildIndices.Count == 0)
            {
                return;
            }

            SceneIndex = (SceneIndex + 1) % SceneBuildIndices.Count;
        }

        public void Previous()
        {
            if (SceneBuildIndices.Count == 0)
            {
                return;
            }
            // instead of decrementing and dealing with underflow, 
            // increment by 1 less than the list size, and mod.
            SceneIndex = (SceneIndex + SceneBuildIndices.Count - 1) % SceneBuildIndices.Count;
        }

        public void LaunchSelected()
        {
            if (SceneIndex >= 0 && SceneIndex < SceneBuildIndices.Count)
            {
                SceneManager.LoadScene(SceneBuildIndices[SceneIndex]);
            }
        }
    }
}
