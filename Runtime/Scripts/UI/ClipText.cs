using TMPro;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.UI
{
    /// <summary>
    /// Extension of TextMeshProUGUI allows the use of a custom clipping material by providing GetModifiedMaterial override
    /// </summary>
    sealed class ClipText : TextMeshProUGUI
    {
        static readonly int k_ParentMatrix = Shader.PropertyToID("_ParentMatrix");
        static readonly int k_ClipExtents = Shader.PropertyToID("_ClipExtents");

        /// <summary>
        /// Parent transform worldToLocalMatrix
        /// </summary>
        public Matrix4x4 parentMatrix { private get; set; }

        /// <summary>
        /// World space extents of visible (non-clipped) region
        /// </summary>
        public Vector3 clipExtents { private get; set; }

        Material m_ModifiedMaterial;

        /// <summary>
        /// Set material properties to update clipping
        /// </summary>
        public void UpdateMaterialClip()
        {
            if (m_ModifiedMaterial != null)
                SetMaterialClip(m_ModifiedMaterial, parentMatrix, clipExtents);
        }

        /// <summary>
        /// Get and cache the modified material instanced by the UI System (needed to apply properties)
        /// </summary>
        /// <param name="baseMaterial">Original material</param>
        /// <returns>Modified material</returns>
        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            m_ModifiedMaterial = base.GetModifiedMaterial(baseMaterial);
            UpdateMaterialClip();
            return m_ModifiedMaterial;
        }

        /// <summary>
        /// Set the ParentMatrix and ClipExtents property on a given material, to update clipping
        /// </summary>
        /// <param name="material">The material to update</param>
        /// <param name="parentMatrix">The transform matrix of the parent object whose extents will be transformed</param>
        /// <param name="extents">The local extents within which to clip</param>
        public static void SetMaterialClip(Material material, Matrix4x4 parentMatrix, Vector3 extents)
        {
            material.SetMatrix(k_ParentMatrix, parentMatrix);
            material.SetVector(k_ClipExtents, extents);
        }
    }
}
