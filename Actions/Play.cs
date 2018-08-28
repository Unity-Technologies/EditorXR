#if UNITY_EDITOR
namespace UnityEditor.Experimental.EditorVR.Actions
{
    [ActionMenuItem("Play", addToSpatialMenu: false)]
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
#endif
