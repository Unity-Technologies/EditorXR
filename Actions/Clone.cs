using System;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Actions
{
	[ActionMenuItem("Clone", ActionMenuItemAttribute.kDefaultActionSectionName, 3)]
	internal sealed class Clone : BaseAction, IUsesSpatialHash
	{
		public Action<GameObject> addToSpatialHash { get; set; }
		public Action<GameObject> removeFromSpatialHash { get; set; }

		public override void ExecuteAction()
		{
			var selection = Selection.gameObjects;
			var bounds = ObjectUtils.GetBounds(selection);
			foreach (var s in selection)
			{
				var clone = ObjectUtils.Instantiate(s.gameObject);
				clone.hideFlags = HideFlags.None;
				var cloneTransform = clone.transform;
				var cameraTransform = CameraUtils.GetMainCamera().transform;
				var viewDirection = cloneTransform.position - cameraTransform.position;
				cloneTransform.position = cameraTransform.TransformPoint(Vector3.forward * viewDirection.magnitude)
					+ cloneTransform.position - bounds.center;
				addToSpatialHash(clone);
			}
		}
	}
}
