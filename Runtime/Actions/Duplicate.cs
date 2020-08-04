using Unity.EditorXR.Interfaces;
using Unity.EditorXR.Utilities;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.SpatialHash;
using Unity.XRTools.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.EditorXR
{
    [ActionMenuItem("Duplicate", ActionMenuItemAttribute.DefaultActionSectionName, 3)]
    [SpatialMenuItem("Duplicate", "Actions", "Duplicate the selected object at the currently focused position")]
    sealed class Duplicate : BaseAction, IUsesSpatialHash, IUsesViewerScale
    {
#if !FI_AUTOFILL
        IProvidesSpatialHash IFunctionalitySubscriber<IProvidesSpatialHash>.provider { get; set; }
        IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
#endif

        public override void ExecuteAction()
        {
#if UNITY_EDITOR
            Unsupported.DuplicateGameObjectsUsingPasteboard();
#endif
            var selection = Selection.transforms;
            var bounds = BoundsUtils.GetBounds(selection);
            foreach (var s in selection)
            {
                var clone = s.gameObject;
                clone.hideFlags = HideFlags.None;
                var cloneTransform = clone.transform;
                var cameraTransform = CameraUtils.GetMainCamera().transform;
                var position = cloneTransform.position;
                var viewDirection = position - cameraTransform.position;
                position = cameraTransform.TransformPoint(Vector3.forward * viewDirection.magnitude / this.GetViewerScale())
                    + position - bounds.center;
                cloneTransform.position = position;
                this.AddToSpatialHash(clone);
            }
        }
    }
}
