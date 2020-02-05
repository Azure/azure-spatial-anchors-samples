using UnityEngine;

namespace Unity.UIElements.Runtime
{
    /// <summary>
    /// Used to display a panel on a rectangle in the scene.
    /// </summary>
    [AddComponentMenu("UIElements/World Quad Display")]
    public class WorldQuadDisplay : MonoBehaviour, IPanelTransform
    {
        /// <summary>
        /// The texture used to render the Panel.
        /// </summary>
        public RenderTexture targetTexture;
        
        /// <summary>
        /// Size of the quad.
        /// </summary>
        public Vector2 sizeInWorld;
        
        /// <summary>
        /// Camera to use to cast rays from the mouse on the screen to the UI.
        /// </summary>
        public Camera eventCamera;

        RenderTexture m_TargetTexture;
        // below properties are needed when doing world space UI using a render texture
        // we programmatically create a quad to map the texture on
        Material m_Material;
        Vector2 m_SizeInWorld ;
        MeshRenderer m_Renderer;
        MeshFilter m_MeshFilter;
        Mesh m_Mesh;
        
        // world space mesh is a simple quad, its UV & Triangles are constant,
        // its vertices are updated based on world space size
        readonly Vector2[] k_UV = new Vector2[]
        {
            new Vector2(0, 0), 
            new Vector2(1, 0), 
            new Vector2(1, 1), 
            new Vector2(0, 1), 
        };
        readonly int[] k_Triangles = new int[]
        {
            2, 1, 0, 0, 3, 2
        };
        Vector3[] m_Vertices = new Vector3[4];

        bool m_ShouldEmitShaderNotFoundError = true;

        /// <summary>
        /// Implementation of OnEnable().
        /// </summary>
        public void OnEnable()
        {
            m_TargetTexture = null;
            m_ShouldEmitShaderNotFoundError = true;
        }

        /// <summary>
        /// Implementation of OnDisable().
        /// </summary>
        public void OnDisable()
        {
            if (m_Renderer != null) 
                m_Renderer.enabled = false;
        }

        void UpdateWorldGeometryAndMaterial()
        {
            if (m_Material == null)
            {
                var shader = Shader.Find("Unlit/Transparent");
                if (shader == null)
                {
                    if (m_ShouldEmitShaderNotFoundError)
                    {
                        m_ShouldEmitShaderNotFoundError = false;
                        Debug.LogError("Shader \"Unlit/Transparent\" not found. Please make sure it is added to the Always Included Shaders list in Edit -> Project Settings -> Graphics.");
                    }
                    return;
                }

                m_Material = new Material(shader);
                m_Material.hideFlags = HideFlags.HideAndDontSave;
            }

            if (m_TargetTexture != targetTexture)
            {
                m_TargetTexture = targetTexture;
                m_Material.mainTexture = m_TargetTexture;
            }

            var meshNeedsUpdate = m_SizeInWorld != sizeInWorld;
            if (m_Mesh == null)
            {
                m_Mesh = new Mesh();
                m_Mesh.hideFlags = HideFlags.HideAndDontSave;
                m_Mesh.vertices = m_Vertices; // vertices will be updated later but must be set before UV are assigned
                m_Mesh.uv = k_UV;
                m_Mesh.triangles = k_Triangles;
                meshNeedsUpdate = true;
            }

            if (meshNeedsUpdate)
            {
                m_SizeInWorld = sizeInWorld;
                m_Vertices[0] = Vector2.zero;
                m_Vertices[1] = new Vector2(m_SizeInWorld.x, 0);
                m_Vertices[2] = new Vector2(m_SizeInWorld.x, m_SizeInWorld.y);
                m_Vertices[3] = new Vector2(0, m_SizeInWorld.y);
                m_Mesh.vertices = m_Vertices;
                // TODO use root layout rect as bounds
                m_Mesh.bounds = new Bounds(m_SizeInWorld * 0.5f, m_SizeInWorld);
            }

            if (m_MeshFilter == null)
            {
                m_MeshFilter = gameObject.AddComponent<MeshFilter>();
                m_MeshFilter.hideFlags = HideFlags.HideAndDontSave;
                m_MeshFilter.mesh = m_Mesh;
            }
            else if (meshNeedsUpdate)
                m_MeshFilter.mesh = m_Mesh;

            if (m_Renderer == null)
            {
                m_Renderer = gameObject.AddComponent<MeshRenderer>();
                m_Renderer.hideFlags = HideFlags.HideAndDontSave;
                m_Renderer.material = m_Material;
            }
            else if (!m_Renderer.enabled)
                m_Renderer.enabled = true;
        }

        /// <summary>
        /// Implementation of Update().
        /// </summary>
        public void Update()
        {
            if (targetTexture != null)
            {
                UpdateWorldGeometryAndMaterial();
            }
            else if (m_TargetTexture != null)
            {
                // target texture was just set to null, deactivate world space renderer
                m_TargetTexture = null;
                if (m_Renderer != null)
                    m_Renderer.enabled = false;
            }
        }

        /// <summary>
        /// Transforms a screen position to a position relative to the panel.
        /// </summary>
        /// <param name="screenPosition">The position in screen coordinates.</param>
        /// <param name="panelUVPosition">The relative position in panel space. Values are between 0 and 1.</param>
        /// <returns>Returns true if the point is inside the panel, false otherwise.</returns>
        bool IPanelTransform.ScreenToPanelUV(Vector2 screenPosition, out Vector2 panelUVPosition)
        {
            screenPosition.y = Screen.height - screenPosition.y;
            var ray = eventCamera.ScreenPointToRay(screenPosition);
            
            var panelRayDir = transform.InverseTransformDirection(ray.direction);
            var panelRayOrigin = transform.InverseTransformPoint(ray.origin);

            var panelRay = new Ray(panelRayOrigin, panelRayDir);
            var plane = new Plane(Vector3.forward, Vector3.zero);
            float distance = 0;
            
            if (plane.Raycast(panelRay, out distance))
            {
                var hitPosition = panelRay.GetPoint(distance);
                panelUVPosition = new Vector2(hitPosition.x / m_SizeInWorld.x, 1 - hitPosition.y / m_SizeInWorld.y);
                return true;
            }

            panelUVPosition = Vector2.zero;
            return false;
        }
    }
}
