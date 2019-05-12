using System;
using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR
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

    public static class ILinkedObjectMethods
    {
        internal static Func<ILinkedObject, bool> isSharedUpdater { get; set; }

        /// <summary>
        /// Returns whether the specified object is designated to perform the duties of all linked objects of this type
        /// </summary>
        /// <param name="linkedObject">Object among the linked objects to check if it is the central one</param>
        public static bool IsSharedUpdater(this ILinkedObject obj, ILinkedObject linkedObject)
        {
            return isSharedUpdater(linkedObject);
        }
    }
}
