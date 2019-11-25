using Unity.Labs.EditorXR.Core;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.EditorXR.Utilities;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.EditorXR
{
    [ActionMenuItem("Paste", ActionMenuItemAttribute.DefaultActionSectionName, 6)]
    [SpatialMenuItem("Paste", "Actions", "Paste a copied object")]
    sealed class Paste : BaseAction, IUsesSpatialHash, IUsesViewerScale
    {
        static float s_BufferDistance;

#if !FI_AUTOFILL
        IProvidesSpatialHash IFunctionalitySubscriber<IProvidesSpatialHash>.provider { get; set; }
        IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
#endif

        public static void SetBufferDistance(Transform[] transforms)
        {
            if (transforms != null)
            {
                var bounds = BoundsUtils.GetBounds(transforms);

                s_BufferDistance = bounds.size != Vector3.zero ? (bounds.center - CameraUtils.GetMainCamera().transform.position).magnitude : 1f;
                var viewerModule = ModuleLoaderCore.instance.GetModule<EditorXRViewerModule>();
                s_BufferDistance /= viewerModule.GetViewerScale(); // Normalize this value in case viewer scale changes before paste happens
            }
        }

        public override void ExecuteAction()
        {
#if UNITY_EDITOR
            Unsupported.PasteGameObjectsFromPasteboard();
#endif
            var transforms = Selection.transforms;
            var bounds = BoundsUtils.GetBounds(transforms);
            foreach (var transform in transforms)
            {
                var pasted = transform.gameObject;
                var pastedTransform = pasted.transform;
                pasted.hideFlags = HideFlags.None;
                var cameraTransform = CameraUtils.GetMainCamera().transform;
                pastedTransform.position = cameraTransform.TransformPoint(Vector3.forward * s_BufferDistance)
                    + pastedTransform.position - bounds.center;
                pasted.SetActive(true);
                this.AddToSpatialHash(pasted);
            }
        }
    }
}
