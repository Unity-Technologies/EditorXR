using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Modules;

public class DragAndDropModule : MonoBehaviour
{
	readonly Dictionary<Transform, object> m_DropObjects = new Dictionary<Transform, object>();
	readonly Dictionary<Transform, IDropReceiver> m_DropReceivers = new Dictionary<Transform, IDropReceiver>();

	readonly Dictionary<Transform, GameObject> m_HoverObjects = new Dictionary<Transform, GameObject>();

	void SetCurrentDropObject(Transform rayOrigin, object obj)
	{
		m_DropObjects[rayOrigin] = obj;
	}

	object GetCurrentDropObject(Transform rayOrigin)
	{
		object obj;
		return m_DropObjects.TryGetValue(rayOrigin, out obj) ? obj : null;
	}

	void SetCurrentDropReceiver(Transform rayOrigin, IDropReceiver dropReceiver)
	{
		if (dropReceiver == null)
			m_DropReceivers.Remove(rayOrigin);
		else
			m_DropReceivers[rayOrigin] = dropReceiver;
	}

	public IDropReceiver GetCurrentDropReceiver(Transform rayOrigin)
	{
		IDropReceiver dropReceiver;
		if (m_DropReceivers.TryGetValue(rayOrigin, out dropReceiver))
			return dropReceiver;

		return null;
	}

	public void OnRayEntered(GameObject gameObject, RayEventData eventData)
	{
		var dropReceiver = gameObject.GetComponent<IDropReceiver>();
		if (dropReceiver != null)
		{
			if (dropReceiver.CanDrop(GetCurrentDropObject(eventData.rayOrigin)))
			{
				dropReceiver.OnDropHoverStarted();
				m_HoverObjects[eventData.rayOrigin] = gameObject;
				SetCurrentDropReceiver(eventData.rayOrigin, dropReceiver);
			}
		}
	}

	public void OnRayExited(GameObject gameObject, RayEventData eventData)
	{
		if (!gameObject)
			return;

		var dropReceiver = gameObject.GetComponent<IDropReceiver>();
		if (dropReceiver != null)
		{
			if (m_HoverObjects.Remove(eventData.rayOrigin))
			{
				dropReceiver.OnDropHoverEnded();
				SetCurrentDropReceiver(eventData.rayOrigin, null);
			}
		}
	}

	public void OnDragStarted(GameObject gameObject, RayEventData eventData)
	{
		var droppable = gameObject.GetComponent<IDroppable>();
		if (droppable != null)
			SetCurrentDropObject(eventData.rayOrigin, droppable.GetDropObject());
	}

	public void OnDragEnded(GameObject gameObject, RayEventData eventData)
	{
		var droppable = gameObject.GetComponent<IDroppable>();
		if (droppable != null)
		{
			var rayOrigin = eventData.rayOrigin;
			SetCurrentDropObject(rayOrigin, null);

			var dropReceiver = GetCurrentDropReceiver(rayOrigin);
			var dropObject = droppable.GetDropObject();
			if (dropReceiver != null && dropReceiver.CanDrop(dropObject))
				dropReceiver.ReceiveDrop(droppable.GetDropObject());
		}
	}
}