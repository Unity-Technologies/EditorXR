using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.UI
{
	public class Button : UnityEngine.UI.Button
	{
		public SelectionFlags selectionFlags { get { return m_SelectionFlags; } set { m_SelectionFlags = value; } }
		[SerializeField]
		[FlagsProperty]
		protected SelectionFlags m_SelectionFlags = SelectionFlags.Ray | SelectionFlags.Direct;

		public OnEnterEvent onEnter = new OnEnterEvent();
		public OnEnterEvent onExit = new OnEnterEvent();
		public OnEnterEvent onDown = new OnEnterEvent();
		public OnEnterEvent onUp = new OnEnterEvent();

		public class OnEnterEvent : UnityEvent { }

		public override void OnPointerClick(PointerEventData eventData)
		{
			var rayEventData = eventData as RayEventData;
			if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
				base.OnPointerClick(eventData);
		}

		public override void OnPointerEnter(PointerEventData eventData)
		{
			var rayEventData = eventData as RayEventData;
			if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			{
				base.OnPointerEnter(eventData);
				onEnter.Invoke();
			}
		}

		public override void OnPointerExit(PointerEventData eventData)
		{
			var rayEventData = eventData as RayEventData;
			if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			{
				base.OnPointerExit(eventData);
				onExit.Invoke();
			}
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			var rayEventData = eventData as RayEventData;
			if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			{
				base.OnPointerDown(eventData);
				onDown.Invoke();
			}
		}

		public override void OnPointerUp(PointerEventData eventData)
		{
			var rayEventData = eventData as RayEventData;
			if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			{
				base.OnPointerUp(eventData);
				onUp.Invoke();
			}
		}

		public override void OnSubmit(BaseEventData eventData)
		{
			var rayEventData = eventData as RayEventData;
			if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
				base.OnSubmit(eventData);
		}

		public override void OnSelect(BaseEventData eventData)
		{
			//Not selectable
		}
	}
}