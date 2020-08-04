using System;
using System.Collections.Generic;

namespace Unity.EditorXR
{
    /// <summary>
    /// Provides access to other tools of the same type
    /// </summary>
    public interface ILinkedObject
    {
        /// <summary>
        /// List of other tools of the same type (not including this one)
        /// </summary>
        List<ILinkedObject> linkedObjects { set; }
    }

    /// <summary>
    /// Extension methods for implementors of ILinkedObject
    /// </summary>
    public static class LinkedObjectMethods
    {
        internal static Func<ILinkedObject, bool> isSharedUpdater { get; set; }

        /// <summary>
        /// Returns whether the specified object is designated to perform the duties of all linked objects of this type
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="linkedObject">Object among the linked objects to check if it is the central one</param>
        /// <returns>True if the specified object is the shared updater</returns>
        public static bool IsSharedUpdater(this ILinkedObject user, ILinkedObject linkedObject)
        {
            return isSharedUpdater(linkedObject);
        }
    }
}
