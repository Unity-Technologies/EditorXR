#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Actions
{
	[ActionMenuItem("Clone", ActionMenuItemAttribute.DefaultActionSectionName, 3)]
	sealed class Clone : BaseAction, IUsesSpatialHash, IUsesViewerScale
	{
		public override void ExecuteAction()
		{
			var selection = Selection.transforms;
			var clones = new GameObject[selection.Length];
			var index = 0;
			var bounds = ObjectUtils.GetBounds(selection);
			foreach (var s in selection)
			{
				var clone = ObjectUtils.Instantiate(s.gameObject);
				clone.hideFlags = HideFlags.None;
				var cloneTransform = clone.transform;
				var cameraTransform = CameraUtils.GetMainCamera().transform;
				var viewDirection = cloneTransform.position - cameraTransform.position;
				cloneTransform.position = cameraTransform.TransformPoint(Vector3.forward * viewDirection.magnitude / this.GetViewerScale())
					+ cloneTransform.position - bounds.center;
				this.AddToSpatialHash(clone);
				clones[index++] = clone;
			}
			Selection.objects = clones;
		}
	}
}
#endif
