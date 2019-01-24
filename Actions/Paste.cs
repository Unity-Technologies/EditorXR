
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Actions
{
    [ActionMenuItem("Paste", ActionMenuItemAttribute.DefaultActionSectionName, 6)]
    [SpatialMenuItem("Paste", "Actions", "Paste a copied object")]
    sealed class Paste : BaseAction, IUsesSpatialHash, IUsesViewerScale
    {
        static float s_BufferDistance;

        public static void SetBufferDistance(Transform[] transforms)
        {
            if (transforms != null)
            {
                var bounds = ObjectUtils.GetBounds(transforms);

                s_BufferDistance = bounds.size != Vector3.zero ? (bounds.center - CameraUtils.GetMainCamera().transform.position).magnitude : 1f;
                s_BufferDistance /= IUsesViewerScaleMethods.getViewerScale(); // Normalize this value in case viewer scale changes before paste happens
            }
        }

        public override void ExecuteAction()
        {
#if UNITY_EDITOR
            Unsupported.PasteGameObjectsFromPasteboard();
#endif
            var transforms = Selection.transforms;
            var bounds = ObjectUtils.GetBounds(transforms);
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

