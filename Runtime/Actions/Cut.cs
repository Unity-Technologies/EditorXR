using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Actions
{
    [ActionMenuItem("Cut", ActionMenuItemAttribute.DefaultActionSectionName, 4)]
    [SpatialMenuItem("Cut", "Actions", "Cut the selected object")]
    sealed class Cut : BaseAction
    {
        public override void ExecuteAction()
        {
            var selection = Selection.transforms;
            if (selection != null)
            {
#if UNITY_EDITOR
                Unsupported.CopyGameObjectsToPasteboard();
#endif
                Paste.SetBufferDistance(Selection.transforms);

                foreach (var transform in selection)
                {
                    var go = transform.gameObject;
                    go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor;
                    go.SetActive(false);
                }

                Selection.activeGameObject = null;
            }
        }
    }
}
