using System;
using UnityEditor;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Delete", ActionMenuItemAttribute.kDefaultActionSectionName, 7)]
	public class Delete : MonoBehaviour, IAction, ISpatialHash
	{
		public Sprite icon { get { return m_Icon; } }
		[SerializeField]
		private Sprite m_Icon;

		public Action<Object> addObjectToSpatialHash { get; set; }
		public Action<Object> removeObjectFromSpatialHash { get; set; }

		public bool ExecuteAction()
		{
			var selection = Selection.activeObject;
			if (selection)
			{
				removeObjectFromSpatialHash(selection);
				U.Object.Destroy(selection);
				Selection.activeObject = null;
				return true;
			}

			return false;
		}
	}
}