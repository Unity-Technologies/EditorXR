
using TMPro;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.UI
{
    /// <summary>
    /// Extension of TextMeshProUGUI allows the use of a custom clipping material by providing GetModifiedMaterial override
    /// </summary>
    sealed class ClipText : TextMeshProUGUI
    {
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
            {
                m_ModifiedMaterial.SetMatrix("_ParentMatrix", parentMatrix);
                m_ModifiedMaterial.SetVector("_ClipExtents", clipExtents);
            }
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
    }
}

