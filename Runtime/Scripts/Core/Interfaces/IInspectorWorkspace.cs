using UnityEngine;

namespace Unity.Labs.EditorXR
{
    interface IInspectorWorkspace
    {
        void UpdateInspector(GameObject obj, bool fullRebuild = false);
    }
}
