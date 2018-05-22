
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Method signature for consuming an input control
    /// <param name="control">InputControl to consume</param>
    /// </summary>
    public delegate void ConsumeControlDelegate(InputControl control);

    /// <summary>
    /// Decorates a class that needs to process input from the system
    /// </summary>
    public interface IProcessInput
    {
        /// <summary>
        /// Implement this method to access input and consume controls when necessary
        /// </summary>
        /// <param name="input">An ActionMapInput if one of the action map interfaces are used</param>
        /// <param name="consumeControl">A delegate for consuming a control that was used</param>
        void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl);
    }
}

