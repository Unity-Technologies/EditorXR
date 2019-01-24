
namespace UnityEditor.Experimental.EditorVR.Actions
{
    [ActionMenuItem("SelectParent", ActionMenuItemAttribute.DefaultActionSectionName, 8)]
    [SpatialMenuItem("Select Parent", "Actions", "Select the parent of the currently selected object")]
    sealed class SelectParent : BaseAction
    {
        public override void ExecuteAction()
        {
            var go = Selection.activeGameObject;
            if (go != null)
            {
                var parent = go.transform.parent;
                if (parent != null)
                {
                    var parentGO = parent.gameObject;
                    if (parentGO)
                    {
                        Selection.activeGameObject = parentGO;
                    }
                }
            }
        }
    }
}

