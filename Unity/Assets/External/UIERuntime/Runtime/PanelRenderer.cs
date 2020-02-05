using UnityEngine.Profiling;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Unity.UIElements.Runtime
{
    internal interface IPanelTransform
    {
        bool ScreenToPanelUV(Vector2 screenPosition, out Vector2 panelPosition);
    }

    /// <summary>
    /// Component to render a UXML file and stylesheets in the game view.
    /// </summary>
    [AddComponentMenu("UIElements/Panel Renderer")]
    [RequireComponent(typeof(PanelScaler))]
    public class PanelRenderer : MonoBehaviour
    {
        class PanelOwner : ScriptableObject
        {
        }

        /// <summary>
        /// The UXML file to render
        /// </summary>
        public VisualTreeAsset uxml;
        
        /// <summary>
        /// The main style sheet file to give styles to Unity provided elements
        /// </summary>
        public StyleSheet unityStyleSheet;
        
        /// <summary>
        /// The associated stylesheets.
        /// </summary>
        public StyleSheet[] stylesheets;
        
        /// <summary>
        /// The top level element.
        /// </summary>
        public VisualElement visualTree { get; private set; }
        
        /// <summary>
        /// The panel holding the visual tree instantiated from the UXML file.
        /// </summary>
        public IPanel panel { get; protected set; }
        
        /// <summary>
        /// An optional texture onto which the panel should be rendered.
        /// </summary>
        public RenderTexture targetTexture; // world space using render textures
        
        /// <summary>
        /// The transform to apply on the panel.
        /// </summary>
        public Component panelTransform;

        /// <summary>
        /// Enables live updates from the UI Builder.
        /// </summary>
        public bool enableLiveUpdates;

        /// <summary>
        /// Functions called after UXML document has been loaded.
        /// </summary>
        public Func<IEnumerable<Object>> postUxmlReload { get; set; }

        IPanelTransform m_PanelTransform;
        PanelScaler m_PanelScaler;
        PanelOwner m_PanelOwner;
        
        RenderTexture m_TargetTexture;
        int m_InitializedCounter;
        Event m_Event = new Event(); // will be used for panel repaint exclusively
        float m_Scale; // panel scaling factor (pixels <-> points)
        Vector2 m_TargetSize;
        
        CustomSampler m_InitSampler;
        CustomSampler m_UpdateSampler;
        bool m_ShouldWarnWorldTransformMissing = true;

        // Change Tracking
        HashSet<Object> m_TrackedAssetSet;
        List<Object> m_TrackedAssetList;
        List<int> m_TrackedAssetHashes;
        HashSet<Object> trackedAssetSet
        {
            get
            {
                if (m_TrackedAssetSet == null)
                    m_TrackedAssetSet = new HashSet<Object>();
                return m_TrackedAssetSet;
            }
        }
        List<Object> trackedAssetList
        {
            get
            {
                if (m_TrackedAssetList == null)
                    m_TrackedAssetList = new List<Object>();
                return m_TrackedAssetList;
            }
        }
        List<int> trackedAssetHashes
        {
            get
            {
                if (m_TrackedAssetHashes == null)
                    m_TrackedAssetHashes = new List<int>();
                return m_TrackedAssetHashes;
            }
        }

        void OnValidate()
        {
            m_ShouldWarnWorldTransformMissing = true;
            if (panelTransform != null && panelTransform is IPanelTransform)
                m_PanelTransform = panelTransform as IPanelTransform;
        }

        private void Reset()
        {
#if UNITY_EDITOR
            unityStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.ui.runtime/USS/Default.uss.asset");
#endif
        }

        /// <summary>
        /// Implementation of OnEnable()
        /// </summary>
        public void OnEnable()
        {
            m_PanelScaler = GetComponent<PanelScaler>();
            m_Event.type = EventType.Repaint;
            m_Scale = Single.NaN;
            m_TargetSize = new Vector2(Single.NaN, Single.NaN);
            m_TargetTexture = targetTexture;
            InternalBridge.SetTargetTexture(panel, m_TargetTexture);

            OnValidate();
            
            InternalBridge.RegisterPanel(gameObject.GetInstanceID(), panel);
        }

        /// <summary>
        /// Implementation of OnDisable()
        /// </summary>
        public void OnDisable()
        {
            InternalBridge.UnregisterPanel(gameObject.GetInstanceID());
        }

        /// <summary>
        /// Implementation of Awake()
        /// </summary>
        public void Awake()
        {
            m_InitSampler = CustomSampler.Create("UIElements." + gameObject.name + ".Initialize");
            m_UpdateSampler = CustomSampler.Create("UIElements." + gameObject.name + ".Update");

            m_InitSampler.Begin();
            Initialize(gameObject.name);
            m_InitSampler.End();
        }

        /// <summary>
        /// Implementation of OnDestroy()
        /// </summary>
        public void OnDestroy()
        {
            Deinitialize();
        }

        void Initialize(string name)
        {
            m_InitializedCounter++;
            if (m_InitializedCounter != 1)
                return;

            m_PanelOwner = ScriptableObject.CreateInstance<PanelOwner>();
            panel = InternalBridge.CreatePanel(m_PanelOwner);
            var root = panel.visualTree;
            root.name = name;

            visualTree = new VisualElement {name = "runtime-panel-container"};
            visualTree.style.overflow = Overflow.Hidden;

            root.Add(visualTree);

            if (unityStyleSheet != null)
            {
                InternalBridge.MarkAsDefaultStyleSheet(unityStyleSheet);
                root.styleSheets.Add(unityStyleSheet);
            }

            if (stylesheets != null)
            {
                foreach (var uss in stylesheets)
                    if (uss != null)
                        root.styleSheets.Add(uss);
            }
        }

        /// <summary>
        /// Implementation of Start()
        /// </summary>
        public void Start()
        {
            RecreateUIFromUxml();
        }

        /// <summary>
        /// Force rebuild the UI from UXML (if one is attached).
        /// </summary>
        public void RecreateUIFromUxml()
        {
            if (enableLiveUpdates)
            {
                trackedAssetSet.Clear();
                trackedAssetList.Clear();
                trackedAssetHashes.Clear();
            }

            visualTree.Clear();

            if (uxml == null)
                return;

            uxml.CloneTree(visualTree);

            if (!enableLiveUpdates)
            {
                postUxmlReload?.Invoke();
                return;
            }

#if UNITY_EDITOR
            // Add the main uxml to the tracking list.
            trackedAssetSet.Add(uxml);

            // In the default implementation, we only track the first (primary)
            // stylesheet. This is what the UI Builder uses as the primary
            // stylesheet so it works well together with the UI Builder.
            // To track additional assets, one can return this list from
            // the postUxmlReload callback. See below.
            foreach (var child in visualTree.Children())
            {
                if (child.styleSheets.count == 0)
                    continue;

                trackedAssetSet.Add(child.styleSheets[0]);
                break;
            }

            if (postUxmlReload != null)
            {
                var additionalTrackedAssets = postUxmlReload();
                if (additionalTrackedAssets != null)
                    trackedAssetSet.UnionWith(additionalTrackedAssets);
            }

            trackedAssetList.AddRange(trackedAssetSet);
            foreach (var asset in trackedAssetList)
                trackedAssetHashes.Add(EditorUtility.GetDirtyCount(asset));
#else
            postUxmlReload?.Invoke();
#endif
        }

        void Deinitialize()
        {
            panel.Dispose();
            DestroyImmediate(m_PanelOwner);
            m_InitializedCounter--;
        }

        /// <summary>
        /// Implementation of Update()
        /// </summary>
        public void Update()
        {
            if (panel == null)
                return;

#if UNITY_EDITOR
            if (enableLiveUpdates && m_TrackedAssetList != null && m_TrackedAssetHashes != null)
            {
                for (int i = 0; i < m_TrackedAssetList.Count; ++i)
                {
                    var asset = m_TrackedAssetList[i];
                    var hash = EditorUtility.GetDirtyCount(asset);
                    var cachedHash = m_TrackedAssetHashes[i];

                    if (hash == cachedHash)
                        continue;

                    RecreateUIFromUxml();
                    return;
                }
            }
#endif

            m_UpdateSampler.Begin();

            var targetSize = targetTexture == null
                ? GetActiveRenderTargetSize()
                : new Vector2(targetTexture.width, targetTexture.height);

            if (targetTexture != m_TargetTexture)
            {
                m_TargetTexture = targetTexture;
                InternalBridge.SetTargetTexture(panel, targetTexture);
            }

            // Temporary: clamp scale to prevent font atlas running out of space
            // won't be needed when using TextCore
            var scale = Mathf.Max(0.1f, m_PanelScaler == null ? 1 : m_PanelScaler.ComputeScalingFactor(targetSize));

            if (m_Scale != scale || m_TargetSize != targetSize)
            {
                InternalBridge.SetScale(panel, scale == 0 ? 0 : 1.0f / scale);
                visualTree.style.left = 0;
                visualTree.style.top = 0;
                visualTree.style.width = targetSize.x * scale;
                visualTree.style.height = targetSize.y * scale;
                m_Scale = scale;
                m_TargetSize = targetSize;
            }

            InternalBridge.UpdatePanel(panel);
            
            m_UpdateSampler.End();
        }

        private void OnRenderObject()
        {
            // render texture based world space rendering
            if (targetTexture != null)
            {
                // when doing world space repaint has to be called explicitly
                InternalBridge.RepaintPanel(panel, m_Event);
            }
        }

        private static Vector2 GetActiveRenderTargetSize()
        {
            return RenderTexture.active == null
                ? new Vector2(Screen.width, Screen.height)
                : new Vector2(RenderTexture.active.width, RenderTexture.active.height);
        }

        internal bool ScreenToPanel(Vector2 screenPosition, out Vector2 panelPosition)
        {
            // if no target texture is set, screen space overlay is assumed
            if (targetTexture == null)
            {
                panelPosition = screenPosition * m_Scale;
                return true;
            }
            
            // can we delegate to worldtransform?
            if (m_PanelTransform == null)
            {
                if (m_ShouldWarnWorldTransformMissing)
                {
                    m_ShouldWarnWorldTransformMissing = false;
                    Debug.LogError("PanelRenderer needs an IWorldTransform implementation for world-space rendering");
                }
                panelPosition = Vector2.zero;
                return false;
            }

            Vector2 panelUVPosition;
            var hit =  m_PanelTransform.ScreenToPanelUV(screenPosition, out panelUVPosition);

            if (!hit)
            {
                panelPosition = Vector2.zero;
                return false;
            }

            var panelSize = panel.visualTree.layout.size;
            panelPosition = new Vector2(panelUVPosition.x * panelSize.x, panelUVPosition.y * panelSize.y);
            return true;
        }
    }
}