using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif

namespace Unity.UIElements.Runtime
{
    /// <summary>
    /// Bridge to access Unity editor functionality.
    /// </summary>
    public static class EditorInternalBridge
    {
        /// <summary>
        /// Create the debug panel.
        /// </summary>
        /// <param name="panel">The panel for which to create a debug panel.</param>
        public static void CreateDebugPanel(IPanel panel)
        {
#if UNITY_EDITOR
            UIElementsEditorRuntimeUtility.CreateRuntimePanelDebug(panel);
#endif
        }
    }
}
