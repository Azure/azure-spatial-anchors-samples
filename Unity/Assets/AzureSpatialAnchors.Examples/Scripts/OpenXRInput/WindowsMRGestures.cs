/* 
 * This script is adapted from the WindowsMRGestures script.
 * It implements the minimum functionality needed by this sample using the OpenXR API.
 */
#if MIXED_REALITY_OPENXR && MIXED_REALITY_WINDOWSMR
#error "WindowsMR and OpenXR plugins cannot be used together, uninstall the Windows MR plugin to use OpenXR"
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using System;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples.OpenXR
{
    public class WindowsMRGestures : MonoBehaviour
    {
#if MIXED_REALITY_OPENXR
        public event Action<WindowsMRTappedGestureEvent> onTappedChanged;
        bool[] wasClicked = new bool[2] { false, false };

        void Update()
        {
            bool rightClicked = HandClicked(XRNode.RightHand);
            bool leftClicked = HandClicked(XRNode.LeftHand);

            if (rightClicked && rightClicked != wasClicked[0] || leftClicked && leftClicked != wasClicked[1])
            {
                onTappedChanged?.Invoke(new WindowsMRTappedGestureEvent());
            }

            wasClicked[0] = rightClicked;
            wasClicked[1] = leftClicked;
        }

        private bool TryGetIsGrabbing(InputDevice device)
        {
            bool isGrabbing = false;

            if (device.TryGetFeatureValue(CommonUsages.triggerButton, out isGrabbing))
            {
                return isGrabbing;
            }
            else if (device.TryGetFeatureValue(CommonUsages.gripButton, out isGrabbing))
            {
                return isGrabbing;
            }
            else if (device.TryGetFeatureValue(CommonUsages.primaryButton, out isGrabbing))
            {
                return isGrabbing;
            }
            return isGrabbing;
        }

        bool HandClicked(XRNode handNode)
        {
            bool Clicking = false;
            InputDevice handDevice = InputDevices.GetDeviceAtXRNode(handNode);
            if (handDevice != null)
            {
                bool deviceValid = handDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool deviceIsTracked);
                if (deviceValid && deviceIsTracked)
                {
                    Clicking = TryGetIsGrabbing(handDevice);
                }
            }

            return Clicking;
        }
#endif
    }

#if MIXED_REALITY_OPENXR
    public class WindowsMRTappedGestureEvent
    {

    }
#endif
}
