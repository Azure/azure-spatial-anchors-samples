using UnityEditor;

namespace Unity.UIElements.Runtime.Editor
{
    [CustomEditor(typeof(PanelScaler))]
    public class PanelScalerInspector : UnityEditor.Editor
    {
        /// <summary>
        /// Implementation of OnInspectorGUI().
        /// </summary>
        public override void OnInspectorGUI()
        {
            var scaler = (PanelScaler)target;

            scaler.scaleMode = (PanelScaler.ScaleMode)EditorGUILayout.EnumPopup("Scale Mode", scaler.scaleMode);

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
        }

        PanelScaler.ConstantPixelSizeScaler Edit(PanelScaler.ConstantPixelSizeScaler scalerImpl)
        {
            scalerImpl.scaleFactor = EditorGUILayout.FloatField("Scale Factor", scalerImpl.scaleFactor);
            return scalerImpl;
        }
        
        PanelScaler.ConstantPhysicalSizeScaler Edit(PanelScaler.ConstantPhysicalSizeScaler scalerImpl)
        {
            scalerImpl.referenceDpi = EditorGUILayout.FloatField("Reference DPI", scalerImpl.referenceDpi);
            scalerImpl.fallbackDpi = EditorGUILayout.FloatField("Fallback DPI", scalerImpl.fallbackDpi);
            return scalerImpl;
        }
        
        PanelScaler.ScaleWithScreenSizeScaler Edit(PanelScaler.ScaleWithScreenSizeScaler scalerImpl)
        {
            scalerImpl.referenceResolution = 
                EditorGUILayout.Vector2IntField("Reference Resolution", scalerImpl.referenceResolution);
            scalerImpl.screenMatchMode = 
                (PanelScaler.ScreenMatchMode)EditorGUILayout.EnumPopup("Screen Match Mode", scalerImpl.screenMatchMode);
            if (scalerImpl.screenMatchMode == PanelScaler.ScreenMatchMode.MatchWidthOrHeight)
                scalerImpl.match = EditorGUILayout.Slider("Match", scalerImpl.match, 0, 1);
            return scalerImpl;
        }
    }
}
