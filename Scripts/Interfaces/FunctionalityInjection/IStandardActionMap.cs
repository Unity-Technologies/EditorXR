using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Receive the default action map from the system
    /// </summary>
    public interface IStandardActionMap : IProcessInput
    {
        ActionMap standardActionMap { set; }
    }
}
