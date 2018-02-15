using UnityEngine;

interface IInspectorWorkspace
{
    void UpdateInspector(GameObject obj, bool fullRebuild = false);
}
