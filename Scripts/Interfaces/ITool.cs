using UnityEngine.InputNew;

namespace UnityEngine.VR.Tools
{
    public interface ITool
    {
        ActionMap ActionMap
        {
            get;
        }

        ActionMapInput ActionMapInput
        {
            get;
            set;
        }

        bool SingleInstance // TODO: When activating a tool, don't add multiple components / action maps if SingleInstance.
        {
            get;
        }
    }
}
