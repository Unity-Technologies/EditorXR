#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Actions
{
    [ActionMenuItem("OpenScene", "Scene")]
    sealed class OpenScene : BaseAction
    {
        public override void ExecuteAction()
        {
            Debug.LogError("ExecuteAction Action should open a sub-panel showing available scenes to open, if any are found");
        }
    }
}
#endif
