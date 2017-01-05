using System;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Actions
{
	[ActionMenuItem("Paste", ActionMenuItemAttribute.kDefaultActionSectionName, 6)]
	public class Paste : BaseAction, IUsesSpatialHash
	{
		public static GameObject[] buffer
		{
			get
			{
				return m_Buffer;
			}
			set
			{
				m_Buffer = value;

				if (value != null)
				{
					var bounds = U.Object.GetBounds(value);
					
					m_BufferDistance = bounds.size == Vector3.zero ? (bounds.center - U.Camera.GetMainCamera().transform.position).magnitude : 0f;
				}
			}
		}
		static GameObject[] m_Buffer;

		static float m_BufferDistance;

		public Action<GameObject> addToSpatialHash { get; set; }
		public Action<GameObject> removeFromSpatialHash { get; set; }

		public override void ExecuteAction()
		{
			//return EditorApplication.ExecuteActionMenuItem("Edit/Paste");

			if (buffer != null)
			{
				var bounds = U.Object.GetBounds(buffer);
				foreach (var go in buffer)
				{
					var pasted = Instantiate(go);
					var pastedTransform = pasted.transform;
					pasted.hideFlags = HideFlags.None;
					var cameraTransform = U.Camera.GetMainCamera().transform;
					pastedTransform.position = cameraTransform.TransformPoint(Vector3.forward * m_BufferDistance)
						+ pastedTransform.position - bounds.center;
					pasted.SetActive(true);
					addToSpatialHash(pasted);
				}
			}
		}
	}
}
