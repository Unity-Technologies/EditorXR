using UnityEngine.EventSystems;
using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Tools
{
	public class FaceAutoScroller : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IRayHoverHandler
	{
		public void OnPointerEnter(PointerEventData eventData)
		{
			Debug.LogError("<color=orange>OnPointerEnter called on FaceAutoScroller</color> : ");
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			Debug.LogError("<color=orange>OnPointerExit called on FaceAutoScroller</color> : ");
		}

		public void OnRayHover(RayEventData eventData)
		{
			Debug.LogError("<color=orange>OnRayHover called on FaceAutoScroller</color> : ");
		}
	}
}