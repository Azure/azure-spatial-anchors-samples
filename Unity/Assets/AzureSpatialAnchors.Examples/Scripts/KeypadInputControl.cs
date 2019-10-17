// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    /// <summary>
    /// Wraps a collection of buttons that represent a keypad
    /// </summary>
    public class KeypadInputControl : MonoBehaviour
    {
        static KeypadInputControl _Instance;
        public static KeypadInputControl Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = FindObjectOfType<KeypadInputControl>();
                }
                return _Instance;
            }
        }

        /// <summary>
        /// The input field to send the key presses to.
        /// </summary>
        public InputField TargetInputField;

        /// <summary>
        /// The collection of buttons. Gathered here for easy
        /// toggling of visibility.
        /// </summary>
        public GameObject Buttons;

        void Start()
        {
            Buttons.SetActive(true);
        }

        void Update()
        {
            Buttons.SetActive(TargetInputField.isActiveAndEnabled);
        }

        public void KeyTapped(char KeyCode)
        {
            switch (KeyCode)
            {
                case 'd': // d means delete 
                    if (TargetInputField.text.Length > 0)
                    {
                        TargetInputField.text = TargetInputField.text.Substring(0, TargetInputField.text.Length - 1);
                    }
                    break;
                default:
                    TargetInputField.text += KeyCode;
                    break;
            }
        }
    }
}
