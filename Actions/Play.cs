
namespace UnityEditor.Experimental.EditorVR.Actions
{
    [ActionMenuItem("Play")]
    sealed class Play : BaseAction
    {
        public override void ExecuteAction()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = true;
#endif
        }
    }
}

