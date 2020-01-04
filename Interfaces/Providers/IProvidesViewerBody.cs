using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to checks that can test against the viewer's body
    /// </summary>
    public interface IProvidesViewerBody : IFunctionalityProvider
    {
        /// <summary>
        /// Check whether the specified transform is over the viewer's shoulders and behind the head
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin to test</param>
        /// <returns>Whether the specified transform is over the viewer's shoulders and behind the head</returns>
        bool IsOverShoulder(Transform rayOrigin);

        /// <summary>
        /// Check whether the specified transform is over the viewer's head
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin to test</param>
        /// <returns>Whether the specified transform is over the viewer's head</returns>
        bool IsAboveHead(Transform rayOrigin);
    }
}
