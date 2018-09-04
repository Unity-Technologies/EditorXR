#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Actions
{
    [ActionMenuItem("SaveScene", "Scene")]
    [SpatialMenuItem("Save Scene", "Actions", "Save the currently open scene")]
    sealed class SaveScene : BaseAction
    {
        public override void ExecuteAction()
        {
            Debug.LogError("ExecuteAction Action should save a scene here");
        }
    }
}
#endif
