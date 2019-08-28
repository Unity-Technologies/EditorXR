using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Declares a class as something that can be vacuumed
    /// </summary>
    public interface IVacuumable
    {
        /// <summary>
        /// Bounding volume to test raycast
        /// </summary>
        Bounds vacuumBounds { get; }

        /// <summary>
        /// Does not require implementation unless implementing class is not a MonoBehaviour
        /// </summary>
        Transform transform { get; }
    }
}
