using UnityEngine.InputNew;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Receive the default action map from the system
    /// </summary>
    public interface IStandardActionMap : IProcessInput
    {
        ActionMap standardActionMap { set; }
    }
}
