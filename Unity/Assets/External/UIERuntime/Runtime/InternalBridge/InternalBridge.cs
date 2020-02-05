using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIElements.Runtime
{
    /// <summary>
    /// Bridge to access Unity engine functionality.
    /// </summary>
    public static class InternalBridge
    {
        /// <summary>
        /// Create a new panel.
        /// </summary>
        /// <param name="scriptableObject">The owner of the panel.</param>
        /// <returns>Returns the new panel.</returns>
        public static IPanel CreatePanel(ScriptableObject scriptableObject)
        {
            var panel = UIElementsRuntimeUtility.CreateRuntimePanel(scriptableObject);
#if UNITY_EDITOR
            EditorInternalBridge.CreateDebugPanel(panel);
#endif
            panel.visualTree.pseudoStates |= PseudoStates.Root;
            return panel;
        }

        /// <summary>
        /// Register the panel in the engine.
        /// </summary>
        /// <param name="ownerID">The id of the ScriptableObject that owns the panel.</param>
        /// <param name="panel">The panel to register.</param>
        public static void RegisterPanel(int ownerID, IPanel panel)
        {
            var runtimePanel = panel as RuntimePanel;
            UIElementsRuntimeUtility.RegisterCachedPanel(ownerID, runtimePanel);
        }

        /// <summary>
        /// Unregister the panel from the engine.
        /// </summary>
        /// <param name="ownerID">The id of the ScriptableObject that owns the panel.</param>
        public static void UnregisterPanel(int ownerID)
        {
            UIElementsRuntimeUtility.RemoveCachedPanel(ownerID);
        }
        
        /// <summary>
        /// Update the panel: run animations, update layout and data bindings.
        /// </summary>
        /// <param name="panel">The panel to update.</param>
        public static void UpdatePanel(IPanel panel)
        {
            var runtimePanel = panel as RuntimePanel;
            runtimePanel.Update();
        }

        /// <summary>
        /// Redraw the panel.
        /// </summary>
        /// <param name="panel">The panel to redraw.</param>
        /// <param name="e">A redraw event.</param>
        public static void RepaintPanel(IPanel panel, Event e)
        {
            var runtimePanel = panel as RuntimePanel;
            runtimePanel.Repaint(e);
        }
        
        /// <summary>
        /// Set the texture as a rendering target for the panel.
        /// </summary>
        /// <param name="panel">The panel for which to set the rendering target.</param>
        /// <param name="texture">The texture to use as a rendering target.</param>
        public static void SetTargetTexture(IPanel panel, RenderTexture texture)
        {
            var runtimePanel = panel as RuntimePanel;
            runtimePanel.targetTexture = texture;
        }

        /// <summary>
        /// Mark the stylesheet as the default one.
        /// </summary>
        /// <param name="styleSheet">The stylesheet to use as the default.</param>
        public static void MarkAsDefaultStyleSheet(StyleSheet styleSheet)
        {
            styleSheet.isUnityStyleSheet = true;
        }
        
        /// <summary>
        /// Set the rendering scale for the panel.
        /// </summary>
        /// <param name="panel">The panel for which to change the scale.</param>
        /// <param name="scale">The scale to use.</param>
        public static void SetScale(IPanel panel, float scale)
        {
            var runtimePanel = panel as RuntimePanel;
            runtimePanel.scale = scale;
        }
        
        /// <summary>
        /// Create a UIElements event from an Event.
        /// </summary>
        /// <param name="evt">The event to use as the source event.</param>
        /// <returns>Returns a new UIElements event.</returns>
        public static EventBase CreateEvent(Event evt)
        {
            return UIElementsRuntimeUtility.CreateEvent(evt);
        }
    }
}
