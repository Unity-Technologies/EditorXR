using UnityEngine;

namespace Unity.EditorXR
{
    interface IInspectorWorkspace
    {
        void UpdateInspector(GameObject obj, bool fullRebuild = false);
    }
}
