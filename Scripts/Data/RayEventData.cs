using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.EventSystems;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	public class RayEventData : PointerEventData
	{
		/// <summary>
		/// The root from where the ray is cast
		/// </summary>
		public Transform rayOrigin { get; set; }

		/// <summary>
		/// The node associated with the ray
		/// </summary>
		public Node node { get; set; }

		/// <summary>
		/// The length of the direct selection pointer
		/// </summary>
		public float pointerLength { get; set; }

		public RayEventData(EventSystem eventSystem) : base(eventSystem)
		{
		}
	}
}