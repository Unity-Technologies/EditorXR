using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Modules;

public class DragAndDropModule : MonoBehaviour {
	private readonly Dictionary<Transform, IDropReciever> m_DropRecievers = new Dictionary<Transform, IDropReciever>();

	public void SetCurrentDropReciever(Transform rayOrigin, IDropReciever dropReciever)
	{
		if (dropReciever == null)
		{
			IDropReciever currentReciever;
			if (m_DropRecievers.TryGetValue(rayOrigin, out currentReciever))
			{
				if (currentReciever == dropReciever)
				{
					m_DropRecievers[rayOrigin] = null;
				}
			}
		}
		else
		{
			m_DropRecievers[rayOrigin] = dropReciever;
		}
	}

	public IDropReciever GetCurrentDropReciever(Transform rayOrigin)
	{
		return m_DropRecievers[rayOrigin];
	}
}