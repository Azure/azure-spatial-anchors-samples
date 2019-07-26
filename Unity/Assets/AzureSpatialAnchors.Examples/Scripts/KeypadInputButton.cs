// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    /// <summary>
    /// Class to wrap sending button presses to a control.
    /// Used on HoloLens for 'keyboard' like input.
    /// </summary>
    public class KeypadInputButton : MonoBehaviour
    {
        /// <summary>
        /// The parent Keypad Control which routes the key press
        /// </summary>
        KeypadInputControl keypadInputControl;

        /// <summary>
        /// The Character that should be registered button is when pressed
        /// </summary>
        public char Character;

        public void OnTapped()
        {
            keypadInputControl.KeyTapped(Character);
        }

        // Use this for initialization
        void Start()
        {
            keypadInputControl = KeypadInputControl.Instance;
            GetComponent<Button>().onClick.AddListener(OnTapped);
        }
    }
}
