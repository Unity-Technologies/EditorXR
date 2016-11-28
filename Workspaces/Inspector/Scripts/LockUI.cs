using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR;
using UnityEngine.VR.Tools;

public class LockUI : MonoBehaviour, IGameObjectLocking
{
	[SerializeField]
	Image m_LockImage;

	[SerializeField]
	Sprite m_LockIcon;

	[SerializeField]
	Sprite m_UnlockIcon;

	public Action<GameObject, bool> setLocked { private get; set; }
	public Func<GameObject, bool> isLocked { private get; set; }
	
	void Start()
	{
		UpdateIcon();
	}

	public void OnLockButtonPressed()
	{
#if UNITY_EDITOR
		var go = Selection.activeGameObject;
		setLocked(go, !isLocked(go));
#endif
		UpdateIcon();
	}

	void UpdateIcon()
	{
		var locked = false;
#if UNITY_EDITOR
		locked = isLocked(Selection.activeGameObject);
#endif
		m_LockImage.sprite = locked ? m_LockIcon : m_UnlockIcon;
	}
}
