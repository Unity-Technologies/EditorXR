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
    }
}
