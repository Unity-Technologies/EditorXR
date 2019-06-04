using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class MiniWorld : MonoBehaviour, IMiniWorld
    {
        public LayerMask rendererCullingMask
        {
            get { return m_RendererCullingMask; }
            set
            {
                m_RendererCullingMask = value;
                if (m_MiniWorldRenderer)
                    m_MiniWorldRenderer.cullingMask = m_RendererCullingMask;
            }
        }

        [SerializeField]
        private LayerMask m_RendererCullingMask = -1;

        private Vector3 m_LocalBoundsSize = Vector3.one;

        private MiniWorldRenderer m_MiniWorldRenderer;

        public Transform miniWorldTransform
        {
            get { return transform; }
        }

        /// <summary>
        /// ReferenceTransform defines world space within the MiniWorld. When scaled up, a larger area is represented,
        /// thus the objects in the MiniWorld get smaller.
        /// </summary>
        public Transform referenceTransform
        {
            get { return m_ReferenceTransform; }
            set { m_ReferenceTransform = value; }
        }

        [SerializeField]
        Transform m_ReferenceTransform;

        public Matrix4x4 miniToReferenceMatrix
        {
            get { return transform.localToWorldMatrix * referenceTransform.worldToLocalMatrix; }
        }

        public Bounds referenceBounds
        {
            get { return new Bounds(referenceTransform.position, Vector3.Scale(referenceTransform.localScale, m_LocalBoundsSize)); }
            set
            {
                referenceTransform.position = value.center;
                m_LocalBoundsSize = Vector3.Scale(referenceTransform.localScale.Inverse(), value.size);
            }
        }

        public Bounds localBounds
        {
            get { return new Bounds(Vector3.zero, m_LocalBoundsSize); }
            set { m_LocalBoundsSize = value.size; }
        }

        public bool Contains(Vector3 position)
        {
            return localBounds.Contains(transform.InverseTransformPoint(position));
        }

        public Matrix4x4 GetWorldToCameraMatrix(Camera camera)
        {
            return m_MiniWorldRenderer.GetWorldToCameraMatrix(camera);
        }

        public List<Renderer> ignoreList
        {
            set { m_MiniWorldRenderer.ignoreList = value; }
        }

        private void OnEnable()
        {
            if (!referenceTransform)
            {
                referenceTransform = EditorXRUtils.CreateEmptyGameObject("MiniWorldReference").transform;
                referenceTransform.parent = ModuleLoaderCore.instance.GetModuleParent().transform;
            }

            m_MiniWorldRenderer = EditorXRUtils.AddComponent<MiniWorldRenderer>(CameraUtils.GetMainCamera().gameObject);
            m_MiniWorldRenderer.miniWorld = this;
            m_MiniWorldRenderer.cullingMask = m_RendererCullingMask;

            Transform rig = CameraUtils.GetCameraRig();
            referenceTransform.position = rig.transform.position;
        }

        private void OnDisable()
        {
            if (referenceTransform)
                UnityObjectUtils.Destroy(referenceTransform.gameObject);

            if (m_MiniWorldRenderer)
                UnityObjectUtils.Destroy(m_MiniWorldRenderer);
        }
    }
}
