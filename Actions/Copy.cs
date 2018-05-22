
namespace UnityEditor.Experimental.EditorVR.Actions
{
    [ActionMenuItem("Copy", ActionMenuItemAttribute.DefaultActionSectionName, 5)]
    sealed class Copy : BaseAction
    {
        public override void ExecuteAction()
        {
#if UNITY_EDITOR
            Unsupported.CopyGameObjectsToPasteboard();
#endif
            Paste.SetBufferDistance(Selection.transforms);
        }
    }
}

