using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    public class ScreenInputHelper : MonoBehaviour
    {
        void Awake()
        {
            var inputModule = GetComponent<MultipleRayInputModule>();
        }
    }
}
