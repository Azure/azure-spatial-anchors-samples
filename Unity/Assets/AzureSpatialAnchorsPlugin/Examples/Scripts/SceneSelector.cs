using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    // Start is called before the first frame update
    void Start()
    {
        if (SelectedSceneNameText == null)
        {
            Debug.Log("Missing text field");
            return;
        }
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
            path = path.Substring(path.LastIndexOf('/')+ "AzureSpatialAnchors".Length + 1);
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
        SceneIndex = (SceneIndex + SceneBuildIndices.Count-1) % SceneBuildIndices.Count;
    }

    public void LaunchSelected()
    {
        if (SceneIndex >= 0 && SceneIndex < SceneBuildIndices.Count)
        {
            SceneManager.LoadScene(SceneBuildIndices[SceneIndex]);
        }
    }
}
