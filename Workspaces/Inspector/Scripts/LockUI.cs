using System;
using UnityEngine;
using UnityEngine.UI;

public class LockUI : MonoBehaviour
{
	[SerializeField]
	Image m_LockImage;

	[SerializeField]
	Sprite m_LockIcon;

	[SerializeField]
	Sprite m_UnlockIcon;

	public event Action lockButtonPressed;
	
	public void OnLockButtonPressed()
	{
		if (lockButtonPressed != null)
			lockButtonPressed();
	}

	public void UpdateIcon(bool locked)
	{
		m_LockImage.sprite = locked ? m_LockIcon : m_UnlockIcon;
	}
}
