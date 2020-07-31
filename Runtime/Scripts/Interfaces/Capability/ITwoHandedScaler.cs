using UnityEngine;

namespace Unity.EditorXR
{
    /// <summary>
    /// Provides a method used to check the status of two-handed scaling
    /// </summary>
    public interface ITwoHandedScaler
    {
        /// <summary>
        /// Returns whether the given ray origin is involved in two-handed scaling
        /// </summary>
        /// <param name="rayOrigin">The ray origin to check</param>
        /// <returns>Whether the given ray origin is involved in two-handed scaling</returns>
        bool IsTwoHandedScaling(Transform rayOrigin);
    }
}
