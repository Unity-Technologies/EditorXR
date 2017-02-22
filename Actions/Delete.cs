using System;
using UnityEditor;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Actions
{
	[ActionMenuItem("Delete", ActionMenuItemAttribute.DefaultActionSectionName, 7)]
	internal sealed class Delete : BaseAction, IUsesSpatialHash
	{
		public Action<GameObject> addToSpatialHash { private get; set; }
		public Action<GameObject> removeFromSpatialHash { private get; set; }

		public override void ExecuteAction()
		{
			var gameObjects = Selection.gameObjects;
			foreach (var go in gameObjects)
			{
				removeFromSpatialHash(go);
				ObjectUtils.Destroy(go);
			}

			Selection.activeGameObject = null;
		}
	}
}
