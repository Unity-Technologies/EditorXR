using UnityEngine;

namespace Unity.EditorXR
{
    [ActionMenuItem("SaveScene", "Scene")]
    sealed class SaveScene : BaseAction
    {
        public override void ExecuteAction()
        {
            Debug.LogError("ExecuteAction Action should save a scene here");
        }
    }
}
