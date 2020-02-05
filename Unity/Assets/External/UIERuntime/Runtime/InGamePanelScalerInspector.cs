using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIElements.Runtime
{
    /// <summary>
    /// Displays an in-game status of the panel scaler.
    /// </summary>
    [ExecuteInEditMode]
    public class InGamePanelScalerInspector : MonoBehaviour
    {
        private PanelScaler scaler;
        
        private static PanelScaler.ScaleMode[] s_ScaleModes =
        {
            PanelScaler.ScaleMode.ConstantPixelSize,
            PanelScaler.ScaleMode.ScaleWithScreenSize,
            PanelScaler.ScaleMode.ConstantPhysicalSize
        };
        
        private static string[] s_ScaleModeLabels =
        {
            "Constant Pixel Size",
            "Scale With Screen Size",
            "Constant Physical Size"
        };

        void OnEnable()
        {
            scaler = GetComponent<PanelScaler>();
        }

        void OnGUI()
        {
            if (scaler == null)
            {
                GUILayout.Label("Failed to retrieve UIECanvasScaler component");
                return;
            }

            var bgColor = GUI.backgroundColor;
            var contentColor = GUI.contentColor;
            GUI.backgroundColor = Color.grey;
            GUI.contentColor = Color.white;
            GUILayout.BeginVertical();
            
            var index = Array.IndexOf(s_ScaleModes, scaler.scaleMode);
            index = GUILayout.SelectionGrid(index, s_ScaleModeLabels, s_ScaleModes.Length);
            scaler.scaleMode = s_ScaleModes[index];

            switch (scaler.scaleMode)
            {
                case PanelScaler.ScaleMode.ConstantPixelSize: 
                    scaler.constantPixelSizeScaler = Edit(scaler.constantPixelSizeScaler);
                    break;
                case PanelScaler.ScaleMode.ConstantPhysicalSize:
                    scaler.constantPhysicalSizeScaler = Edit(scaler.constantPhysicalSizeScaler);
                    break;
                case PanelScaler.ScaleMode.ScaleWithScreenSize:
                    scaler.scaleWithScreenSizeScaler = Edit(scaler.scaleWithScreenSizeScaler);
                    break;
            }
            
            GUILayout.EndVertical();
            GUI.backgroundColor = bgColor;
            GUI.contentColor = contentColor;
        }

        PanelScaler.ConstantPixelSizeScaler Edit(PanelScaler.ConstantPixelSizeScaler scalerImpl)
        {
            
            GUILayout.Label($"Scale Factor {scalerImpl.scaleFactor}");
            scalerImpl.scaleFactor = GUILayout.HorizontalSlider(scalerImpl.scaleFactor, 0.001f, 4);
            return scalerImpl;
        }
        
        PanelScaler.ConstantPhysicalSizeScaler Edit(PanelScaler.ConstantPhysicalSizeScaler scalerImpl)
        {
            GUILayout.Label($"Reference DPI {scalerImpl.referenceDpi}");
            scalerImpl.referenceDpi = Mathf.RoundToInt(GUILayout.HorizontalSlider(scalerImpl.referenceDpi, 1, 300));

            GUILayout.Label($"Fallback DPI {scalerImpl.fallbackDpi}");
            scalerImpl.fallbackDpi = Mathf.RoundToInt(GUILayout.HorizontalSlider(scalerImpl.fallbackDpi, 1, 300));
            return scalerImpl;
        }

        private static PanelScaler.ScreenMatchMode[] s_MatchModes =
        {
            PanelScaler.ScreenMatchMode.MatchWidthOrHeight,
            PanelScaler.ScreenMatchMode.Expand,
            PanelScaler.ScreenMatchMode.Shrink
        };
        
        private static string[] s_MatchModeLabels =
        {
            "Match Width Or Height",
            "Expand",
            "Shrink"
        };
        
        PanelScaler.ScaleWithScreenSizeScaler Edit(PanelScaler.ScaleWithScreenSizeScaler scalerImpl)
        {
            
            var refX = scalerImpl.referenceResolution.x;
            var refY = scalerImpl.referenceResolution.y;
            GUILayout.Label($"Reference Resolution Width {refX}");
            refX = Mathf.RoundToInt(GUILayout.HorizontalSlider(refX, 1, 1920));
            GUILayout.Label($"Reference Resolution Height {refY}");
            refY = Mathf.RoundToInt(GUILayout.HorizontalSlider(refY, 1, 1080));
            scalerImpl.referenceResolution = new Vector2Int(refX, refY);

            var index = Array.IndexOf(s_MatchModes, scalerImpl.screenMatchMode);
            index = GUILayout.SelectionGrid(index, s_MatchModeLabels, s_MatchModes.Length);
            scalerImpl.screenMatchMode = s_MatchModes[index];
            
            if (scalerImpl.screenMatchMode == PanelScaler.ScreenMatchMode.MatchWidthOrHeight)
            {
                GUILayout.Label($"Match {scalerImpl.match}");
                scalerImpl.match = GUILayout.HorizontalSlider(scalerImpl.match, 0, 1);
            }

            return scalerImpl;
        }
    }
}