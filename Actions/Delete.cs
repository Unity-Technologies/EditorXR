using System;
using UnityEditor;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Delete", ActionMenuItemAttribute.kDefaultActionSectionName, 7)]
	public class Delete : BaseAction, ISpatialHash
	{
		public Action<Object> addObjectToSpatialHash { get; set; }
		public Action<Object> removeObjectFromSpatialHash { get; set; }

		public override void ExecuteAction()
		{
			var selection = Selection.activeObject;
			if (selection)
			{
				removeObjectFromSpatialHash(selection);
				U.Object.Destroy(selection);
				Selection.activeObject = null;
			}
		}
	}
}
