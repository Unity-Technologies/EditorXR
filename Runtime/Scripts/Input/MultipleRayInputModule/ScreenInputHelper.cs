using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    public class ScreenInputHelper : MonoBehaviour
    {
        MultipleRayInputModule.ScreenRaycastSource m_Source;

        void Awake()
        {
            var inputModule = GetComponent<MultipleRayInputModule>();
            var camera = Camera.main;
            m_Source = new MultipleRayInputModule.ScreenRaycastSource(camera, inputModule);
            inputModule.AddRaycastSource(camera.transform, m_Source);
        }

        void Update() { m_Source.Update(); }
    }
}
