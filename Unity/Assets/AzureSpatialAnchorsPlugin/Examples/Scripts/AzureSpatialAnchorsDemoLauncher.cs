using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AzureSpatialAnchorsDemoLauncher : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        for(int index=0;index<SceneManager.sceneCount;index++)
        {
            Scene s = SceneManager.GetSceneAt(index);
            Debug.Log(s.name);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
