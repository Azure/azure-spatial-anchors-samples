using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;

namespace Unity.UIElements.Runtime.Editor
{
    internal static class MenuItems
    {
        private const string kUILayerName = "UI";
        
        [MenuItem("GameObject/UIElements/Panel", false, 9)]
        public static void AddPanel(MenuCommand menuCommand)
        {
            GameObject parent = menuCommand.context as GameObject;
            var root = ObjectFactory.CreateGameObject("Panel", typeof(PanelRenderer), typeof(UIElementsEventSystem));
            root.layer = LayerMask.NameToLayer(kUILayerName);
            
            // Works for all stages.
            StageUtility.PlaceGameObjectInCurrentStage(root);
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                Undo.SetTransformParent(root.transform, prefabStage.prefabContentsRoot.transform, "");
            }

            Undo.SetCurrentGroupName("Create " + root.name);

            SetParentAndAlign(root, parent);
            Selection.activeGameObject = root;
        }

        private static void SetParentAndAlign(GameObject child, GameObject parent)
        {
            if (parent == null)
                return;

            Undo.SetTransformParent(child.transform, parent.transform, "");
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            SetLayerRecursively(child, parent.layer);
        }
        
        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            Transform t = go.transform;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursively(t.GetChild(i).gameObject, layer);
        }
    }
}
