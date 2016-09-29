using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Modules;

public class DragAndDropModule : MonoBehaviour {
	private class DropData
	{
		public IDropReciever reciever; // The IDropReciever that we will call .Drop on
		public GameObject target; // The actual object that was hovered
	}

	private readonly Dictionary<Transform, DropData> m_DropRecievers = new Dictionary<Transform, DropData>();

	public void SetCurrentDropReciever(Transform rayOrigin, IDropReciever dropReciever, GameObject target)
	{
		if (dropReciever == null)
		{
			DropData data;
			if (m_DropRecievers.TryGetValue(rayOrigin, out data))
			{
				if (data.reciever.Equals(dropReciever))
					m_DropRecievers[rayOrigin] = null;
				}
			}
		else
		{
			m_DropRecievers[rayOrigin] = new DropData { reciever = dropReciever, target = target };
		}
	}

	public IDropReciever GetCurrentDropReciever(Transform rayOrigin, out GameObject target)
	{
		DropData data;
		if (m_DropRecievers.TryGetValue(rayOrigin, out data))
		{
			target = data.target;
			return data.reciever;
		}

		target = null;
		return null;
	}
}