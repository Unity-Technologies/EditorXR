using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
#if UNITY_EDITOR
    [RequiresTag(k_MiniWorldCameraTag)]
    [RequiresTag(ShowInMiniWorldTag)]
#endif
    sealed class MiniWorldRenderer : MonoBehaviour, IScriptReference
    {
        public const string ShowInMiniWorldTag = "ShowInMiniWorld";
        const string k_MiniWorldCameraTag = "MiniWorldCamera";
        const float k_MinScale = 0.001f;

        static int s_DefaultLayer;

#pragma warning disable 649
        [SerializeField]
        Shader m_ClipShader;
#pragma warning restore 649

        List<Renderer> m_IgnoreList = new List<Renderer>();

        Camera m_MiniCamera;

        int[] m_IgnoredObjectLayer;
        bool[] m_IgnoreObjectRendererEnabled;

        public List<Renderer> ignoreList
        {
            set
            {
                m_IgnoreList = value;
                var count = m_IgnoreList == null ? 0 : m_IgnoreList.Count;
                if (m_IgnoreObjectRendererEnabled == null || count > m_IgnoreObjectRendererEnabled.Length)
                {
                    m_IgnoredObjectLayer = new int[count];
                    m_IgnoreObjectRendererEnabled = new bool[count];
                }
            }
        }

        public MiniWorld miniWorld { private get; set; }
        public LayerMask cullingMask { private get; set; }

        public Matrix4x4 GetWorldToCameraMatrix(Camera camera)
        {
            return camera.worldToCameraMatrix * miniWorld.miniToReferenceMatrix;
        }

        void Awake()
        {
            s_DefaultLayer = LayerMask.NameToLayer("Default");
        }

        void OnEnable()
        {
            var moduleParent = ModuleLoaderCore.instance.GetModuleParent();
            m_MiniCamera = (Camera)EditorXRUtils.CreateGameObjectWithComponent(typeof(Camera), moduleParent.transform);
            var go = m_MiniCamera.gameObject;
            go.name = "MiniWorldCamera";
            go.tag = k_MiniWorldCameraTag;
            go.SetActive(false);
            Camera.onPostRender += RenderMiniWorld;
        }

        void OnDisable()
        {
            Camera.onPostRender -= RenderMiniWorld;
            UnityObjectUtils.Destroy(m_MiniCamera.gameObject);
        }

        void RenderMiniWorld(Camera camera)
        {
            // Do not render if miniWorld scale is too low to avoid errors in the console
            if (!camera.gameObject.CompareTag(k_MiniWorldCameraTag) && miniWorld && miniWorld.transform.lossyScale.magnitude > k_MinScale)
            {
                m_MiniCamera.CopyFrom(camera);

                m_MiniCamera.cullingMask = cullingMask;
                m_MiniCamera.cameraType = CameraType.Game;
                m_MiniCamera.clearFlags = CameraClearFlags.Nothing;
                m_MiniCamera.worldToCameraMatrix = GetWorldToCameraMatrix(camera);

                var referenceBounds = miniWorld.referenceBounds;
                var inverseRotation = Quaternion.Inverse(miniWorld.referenceTransform.rotation);

                Shader.SetGlobalVector("_GlobalClipCenter", inverseRotation * referenceBounds.center);
                Shader.SetGlobalVector("_GlobalClipExtents", referenceBounds.extents);
                Shader.SetGlobalMatrix("_InverseRotation", Matrix4x4.TRS(Vector3.zero, inverseRotation, Vector3.one));

                for (var i = 0; i < m_IgnoreList.Count; i++)
                {
                    var hiddenRenderer = m_IgnoreList[i];
                    if (!hiddenRenderer || !hiddenRenderer.gameObject.activeInHierarchy)
                        continue;

                    if (hiddenRenderer.CompareTag(ShowInMiniWorldTag))
                    {
                        m_IgnoredObjectLayer[i] = hiddenRenderer.gameObject.layer;
                        hiddenRenderer.gameObject.layer = s_DefaultLayer;
                    }
                    else
                    {
                        m_IgnoreObjectRendererEnabled[i] = hiddenRenderer.enabled;
                        hiddenRenderer.enabled = false;
                    }
                }

                // Because this is a manual render it is necessary to set the target texture to whatever the active RT
                // is, which would either be the left/right eye in the case of VR rendering, or the custom SceneView RT
                // in the case of the SceneView rendering, etc.
                m_MiniCamera.targetTexture = RenderTexture.active;

                m_MiniCamera.SetReplacementShader(m_ClipShader, "RenderType");
                m_MiniCamera.Render();

                for (var i = 0; i < m_IgnoreList.Count; i++)
                {
                    var hiddenRenderer = m_IgnoreList[i];
                    if (!hiddenRenderer || !hiddenRenderer.gameObject.activeInHierarchy)
                        continue;

                    if (hiddenRenderer.CompareTag(ShowInMiniWorldTag))
                        hiddenRenderer.gameObject.layer = m_IgnoredObjectLayer[i];
                    else
                        hiddenRenderer.enabled = m_IgnoreObjectRendererEnabled[i];
                }
            }
        }
    }
}
