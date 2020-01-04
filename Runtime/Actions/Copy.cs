using UnityEditor;

namespace Unity.Labs.EditorXR
{
    [ActionMenuItem("Copy", ActionMenuItemAttribute.DefaultActionSectionName, 5)]
    [SpatialMenuItem("Copy", "Actions", "Copy the selected object")]
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
