using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Modules;

public class DragAndDropModule : MonoBehaviour
{
	class DropData
	{
		public IDropReceiver receiver; // The IDropReceiver that we will call .Drop on
		public GameObject target; // The actual object that was hovered
	}

	readonly Dictionary<Transform, object> m_DropObjects = new Dictionary<Transform, object>();
	readonly Dictionary<Transform, DropData> m_DropReceivers = new Dictionary<Transform, DropData>();

	public void SetCurrentDropObject(Transform rayOrigin, object obj)
	{
		m_DropObjects[rayOrigin] = obj;
	}

	public object GetCurrentDropObject(Transform rayOrigin)
	{
		object obj;
		return m_DropObjects.TryGetValue(rayOrigin, out obj) ? obj : null;
	}

	public void SetCurrentDropReceiver(Transform rayOrigin, IDropReceiver dropReceiver, GameObject target)
	{
		if (dropReceiver == null)
		{
			DropData data;
			if (m_DropReceivers.TryGetValue(rayOrigin, out data) && data.target == target)
				m_DropReceivers.Remove(rayOrigin);
		}
		else
		{
			m_DropReceivers[rayOrigin] = new DropData { receiver = dropReceiver, target = target };
		}
	}

	public IDropReceiver GetCurrentDropReceiver(Transform rayOrigin, out GameObject target)
	{
		DropData data;
		if (m_DropReceivers.TryGetValue(rayOrigin, out data))
		{
			target = data.target;
			return data.receiver;
		}

		target = null;
		return null;
	}
}