using System;
using UnityEditor;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Actions
{
	[ActionMenuItem("Clone", ActionMenuItemAttribute.kDefaultActionSectionName, 3)]
	public class Clone : BaseAction, IUsesSpatialHash
	{
		public Action<GameObject> addToSpatialHash { get; set; }
		public Action<GameObject> removeFromSpatialHash { get; set; }

		public override void ExecuteAction()
		{
			var selection = Selection.gameObjects;
			var bounds = U.Object.GetBounds(selection);
			foreach (var s in selection)
			{
				var clone = U.Object.Instantiate(s.gameObject);
				clone.hideFlags = HideFlags.None;
				var cloneTransform = clone.transform;
				var cameraTransform = U.Camera.GetMainCamera().transform;
				var viewDirection = cloneTransform.position - cameraTransform.position;
				cloneTransform.position = cameraTransform.TransformPoint(Vector3.forward * viewDirection.magnitude)
					+ cloneTransform.position - bounds.center;
				addToSpatialHash(clone);
			}
		}
	}
}
