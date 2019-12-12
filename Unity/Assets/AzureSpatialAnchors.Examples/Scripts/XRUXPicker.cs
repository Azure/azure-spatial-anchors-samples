// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    /// <summary>
    /// Picks the appropriate UI game object to be used. 
    /// This allows us to have both HoloLens and Mobile UX in the same
    /// scene.
    /// </summary>
    public class XRUXPicker : MonoBehaviour
    {
        private static XRUXPicker _Instance;
        public static XRUXPicker Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = FindObjectOfType<XRUXPicker>();
                }

                return _Instance;
            }
        }

        public GameObject HoloLensUXTree;
        public GameObject MobileAndEditorUXTree;

        void Awake()
        {
#if UNITY_WSA
            HoloLensUXTree.SetActive(true);
#else
            MobileAndEditorUXTree.SetActive(true);
#endif
        }

        /// <summary>
        /// Gets the correct feedback text control for the demo
        /// </summary>
        /// <returns>The feedback text control if it found it</returns>
        public Text GetFeedbackText()
        {
            GameObject sourceTree = null;

#if UNITY_WSA
            sourceTree = HoloLensUXTree;
#else
            sourceTree = MobileAndEditorUXTree;
#endif

            Debug.Log(sourceTree.transform.childCount);
            int childCount = sourceTree.transform.childCount;
            for (int index = 0; index < childCount; index++)
            {
                GameObject child = sourceTree.transform.GetChild(index).gameObject;
                Text t = child.GetComponent<Text>();
                if (t != null)
                {
                    return t;
                }

            }

            Debug.LogError("Did not find feedback text control.");
            return null;
        }

        /// <summary>
        /// Gets the button used in the demo.
        /// </summary>
        /// <returns>The button used in the demo.  Returns null on HoloLens</returns>
        public Button GetDemoButton()
        {
#if UNITY_WSA
            Debug.LogError("Demo doesn't expect a button for HoloLens");
            return null;
#else
            return MobileAndEditorUXTree.GetComponentInChildren<Button>();
#endif
        }

        /// <summary>
        /// Gets the buttons used in the demo.
        /// </summary>
        /// <returns>The buttons used in the demo.</returns>
        public Button[] GetDemoButtons()
        {
#if UNITY_WSA
            return HoloLensUXTree.GetComponentsInChildren<Button>(true);
#else
            return MobileAndEditorUXTree.GetComponentsInChildren<Button>(true);
#endif
        }
    }
}
