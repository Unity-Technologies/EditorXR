using System;
using UnityEditor;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Clone", ActionMenuItemAttribute.kDefaultActionSectionName, 3)]
	public class Clone : MonoBehaviour, IAction, ISpatialHash
	{
		[SerializeField]
		const int directionCount = 6;
		static int directionCounter;

		[SerializeField]
		float positionOffset = 1.5f;

		public Sprite icon { get { return m_Icon; } }
		[SerializeField]
		private Sprite m_Icon;

		public Action<Object> addObjectToSpatialHash { get; set; }
		public Action<Object> removeObjectFromSpatialHash { get; set; }

		public bool ExecuteAction()
		{
			var selection = Selection.GetTransforms(SelectionMode.Editable);
			foreach (var s in selection)
			{
				var clone = U.Object.Instantiate(s.gameObject);
				clone.hideFlags = HideFlags.None;
				var bounds = U.Object.GetTotalBounds(clone.transform);
				var offset = Vector3.one;
				if (bounds.HasValue)
				{
					var camera = U.Camera.GetMainCamera();
					var viewDirection = camera.transform.position - clone.transform.position;
					viewDirection.y = 0;
					offset = Quaternion.LookRotation(viewDirection) * Quaternion.AngleAxis((float)directionCounter / directionCount * 360, Vector3.forward) * Vector3.left * bounds.Value.size.magnitude * positionOffset;
					directionCounter++;
				}
				clone.transform.position += offset;
				addObjectToSpatialHash(clone);
			}

			return true;
		}
	}
}