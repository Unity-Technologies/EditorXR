
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Decorates tools which supply their own (singular) ActionMap
    /// </summary>
    public interface ICustomActionMap : IProcessInput
    {
        /// <summary>
        /// Provides access to the custom action map
        /// </summary>
        ActionMap actionMap { get; }

        /// <summary>
        /// Whether the custom action map will always receive input, regardless of locking
        /// </summary>
        bool ignoreLocking { get; }
    }
}

