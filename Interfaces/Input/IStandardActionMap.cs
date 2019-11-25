using UnityEngine.InputNew;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Receive the default action map from the system
    /// </summary>
    public interface IStandardActionMap : IProcessInput
    {
        /// <summary>
        /// The default action map will be set on this property by the system
        /// </summary>
        ActionMap standardActionMap { set; }
    }
}
