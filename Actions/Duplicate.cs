
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Actions
{
    [ActionMenuItem("Duplicate", ActionMenuItemAttribute.DefaultActionSectionName, 3)]
    sealed class Duplicate : BaseAction, IUsesSpatialHash, IUsesViewerScale
    {
        public override void ExecuteAction()
        {
#if UNITY_EDITOR
            Unsupported.DuplicateGameObjectsUsingPasteboard();
#endif
            var selection = Selection.transforms;
            var bounds = ObjectUtils.GetBounds(selection);
            foreach (var s in selection)
            {
                var clone = s.gameObject;
                clone.hideFlags = HideFlags.None;
                var cloneTransform = clone.transform;
                var cameraTransform = CameraUtils.GetMainCamera().transform;
                var viewDirection = cloneTransform.position - cameraTransform.position;
                cloneTransform.position = cameraTransform.TransformPoint(Vector3.forward * viewDirection.magnitude / this.GetViewerScale())
                    + cloneTransform.position - bounds.center;
                this.AddToSpatialHash(clone);
            }
        }
    }
}

