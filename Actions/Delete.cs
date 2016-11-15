using System;
using UnityEditor;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Delete", ActionMenuItemAttribute.kDefaultActionSectionName, 7)]
	public class Delete : BaseAction, IUsesSpatialHash
	{
		public Action<GameObject> addToSpatialHash { get; set; }
		public Action<GameObject> removeFromSpatialHash { get; set; }

		public override void ExecuteAction()
		{
			var gameObjects = Selection.gameObjects;
			foreach (var go in gameObjects)
			{
				removeFromSpatialHash(go);
				U.Object.Destroy(go);
			}

			Selection.objects = null;
		}
	}
}
