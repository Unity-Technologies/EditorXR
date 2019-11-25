using Unity.Labs.EditorXR.Utilities;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Labs.EditorXR.Helpers
{
#if UNITY_EDITOR
    /// <summary>
    /// Updates a RawImage texture with data from an EditorWindowCapture
    /// </summary>
    sealed class EditorWindowCaptureUpdater : MonoBehaviour
    {
        [SerializeField]
        EditorWindowCapture m_EditorWindowCapture;

        [SerializeField]
        RawImage m_RawImage;

        [SerializeField]
        Material m_Material;

        [SerializeField]
        bool m_LockAspect = true;

        void Start()
        {
            if (!m_EditorWindowCapture)
                m_EditorWindowCapture = GetComponent<EditorWindowCapture>();

            if (!m_RawImage)
                m_RawImage = GetComponent<RawImage>();

            if (m_RawImage)
            {
                // Texture comes in flipped, so it's necessary to correct it
                var rect = m_RawImage.uvRect;
                rect.height *= -1f;
                m_RawImage.uvRect = rect;
            }

            if (!m_RawImage && !m_Material)
            {
                var renderer = GetComponent<Renderer>();
                m_Material = MaterialUtils.GetMaterialClone(renderer);
            }

            if (m_Material)
            {
                // Texture comes in flipped, so it's necessary to correct it
                var scale = m_Material.mainTextureScale;
                scale.y *= -1f;
                m_Material.mainTextureScale = scale;
            }
        }

        void OnDestroy()
        {
            UnityObjectUtils.Destroy(m_Material);
        }

        void LateUpdate()
        {
            // Only capture when we are looking at the view
            var camera = CameraUtils.GetMainCamera();
            if (camera)
            {
                var plane = new Plane(-transform.forward, transform.position);
                m_EditorWindowCapture.capture = plane.GetSide(camera.transform.position);
            }

            var tex = m_EditorWindowCapture.texture;
            if (tex)
            {
                if (m_RawImage && m_RawImage.texture != tex)
                    m_RawImage.texture = tex;

                if (m_Material && m_Material.mainTexture != tex)
                    m_Material.mainTexture = tex;

                if (m_LockAspect)
                {
                    var localScale = transform.localScale;
                    var texAspect = (float)tex.width / tex.height;
                    var aspect = localScale.x / localScale.y;
                    localScale.y *= aspect / texAspect;
                    transform.localScale = localScale;
                }
            }
        }
    }
#else
    sealed class EditorWindowCaptureUpdater : MonoBehaviour
    {
        [SerializeField]
        EditorWindowCapture m_EditorWindowCapture;

        [SerializeField]
        RawImage m_RawImage;

        [SerializeField]
        Material m_Material;

        [SerializeField]
        bool m_LockAspect;
    }
#endif
}
