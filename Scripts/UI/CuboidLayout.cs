using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.UI
{
    sealed class CuboidLayout : UIBehaviour
    {
        static readonly Vector2 k_CuboidPivot = new Vector2(0.5f, 0.5f);
        const float k_LayerHeight = 0.004f;
        const float k_ExtraSpace = 0.00055f; // To avoid Z-fighting

#pragma warning disable 649
        [SerializeField]
        RectTransform[] m_TargetTransforms;

        [SerializeField]
        RectTransform[] m_TargetHighlightTransforms;

        [Header("Prefab Templates")]
        [SerializeField]
        GameObject m_CubePrefab;

        [SerializeField]
        GameObject m_HighlightCubePrefab;
#pragma warning restore 649

        Transform[] m_CubeTransforms;
        Transform[] m_HighlightCubeTransforms;

        protected override void Awake()
        {
            Setup();

            UpdateObjects();
        }

        [ContextMenu("Setup")]
        void Setup()
        {
            m_CubeTransforms = new Transform[m_TargetTransforms.Length];
            for (var i = 0; i < m_CubeTransforms.Length; i++)
            {
                m_CubeTransforms[i] = ObjectUtils.Instantiate(m_CubePrefab, m_TargetTransforms[i], false).transform;
            }

            m_HighlightCubeTransforms = new Transform[m_TargetHighlightTransforms.Length];
            for (var i = 0; i < m_TargetHighlightTransforms.Length; i++)
            {
                m_HighlightCubeTransforms[i] = ObjectUtils.Instantiate(m_HighlightCubePrefab, m_TargetHighlightTransforms[i], false).transform;
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            UpdateObjects();
        }

        /// <summary>
        /// Set a new material on all backing cubes (used for instanced version of the material)
        /// </summary>
        /// <param name="backingCubeMaterial">New material to use</param>
        public void SetMaterials(Material backingCubeMaterial, Material[] highlightMaterials)
        {
            if (m_CubeTransforms != null)
            {
                foreach (var cube in m_CubeTransforms)
                {
                    cube.GetComponent<Renderer>().sharedMaterial = backingCubeMaterial;
                }

                // These are most likely WorkspaceButtons, so the material cloning that is done there will get stomped by this
                foreach (var hightlight in m_HighlightCubeTransforms)
                {
                    foreach (var child in hightlight.GetComponentsInChildren<Renderer>())
                    {
                        if (child.transform != hightlight)
                            child.sharedMaterials = highlightMaterials;
                        else
                            child.sharedMaterial = backingCubeMaterial;
                    }
                }
            }
        }

        [ContextMenu("UpdateObjects")]
        public void UpdateObjects()
        {
            if (m_CubeTransforms == null)
                return;

            // Update standard objects
            const float kStandardObjectSideScalePadding = 0.005f;
            for (var i = 0; i < m_CubeTransforms.Length; i++)
            {
                var rectSize = m_TargetTransforms[i].rect.size.Abs();

                // Scale pivot by rect size to get correct xy local position
                var pivotOffset = Vector2.Scale(rectSize, k_CuboidPivot - m_TargetTransforms[i].pivot);

                // Add space for target transform
                var localPosition = m_TargetTransforms[i].localPosition;
                m_TargetTransforms[i].localPosition = new Vector3(localPosition.x, localPosition.y, -k_LayerHeight);

                //Offset by 0.5 * height to account for pivot in center
                const float zOffset = k_LayerHeight * 0.5f + k_ExtraSpace;
                m_CubeTransforms[i].localPosition = new Vector3(pivotOffset.x, pivotOffset.y, zOffset);
                m_CubeTransforms[i].localScale = new Vector3(rectSize.x + kStandardObjectSideScalePadding, rectSize.y, k_LayerHeight);
            }

            // Update highlight objects
            for (var i = 0; i < m_HighlightCubeTransforms.Length; i++)
            {
                var rectSize = m_TargetHighlightTransforms[i].rect.size.Abs();

                // Scale pivot by rect size to get correct xy local position
                var pivotOffset = Vector2.Scale(rectSize, k_CuboidPivot - m_TargetHighlightTransforms[i].pivot);

                // Add space for target transform
                var localPosition = m_TargetHighlightTransforms[i].localPosition;
                m_TargetHighlightTransforms[i].localPosition = new Vector3(localPosition.x, localPosition.y, -k_LayerHeight);

                //Offset by 0.5 * height to account for pivot in center
                const float zOffset = k_LayerHeight * 0.5f + k_ExtraSpace;
                m_HighlightCubeTransforms[i].localPosition = new Vector3(pivotOffset.x, pivotOffset.y, zOffset);
                m_HighlightCubeTransforms[i].localScale = new Vector3(rectSize.x, rectSize.y, k_LayerHeight);
            }
        }
    }
}
