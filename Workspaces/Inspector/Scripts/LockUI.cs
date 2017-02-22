using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;

public class LockUI : MonoBehaviour, IUsesStencilRef
{
	[SerializeField]
	Image m_LockImage;

	[SerializeField]
	Sprite m_LockIcon;

	[SerializeField]
	Sprite m_UnlockIcon;

	List<Material> m_ButtonMaterials = new List<Material>();

	public byte stencilRef { get; set; }

	public event Action lockButtonPressed;

	public void Setup()
	{
		var mr = GetComponentInChildren<MeshRenderer>();
		foreach (var sm in mr.sharedMaterials)
		{
			sm.SetInt("_StencilRef", stencilRef);
		}
	}

	void OnDestroy()
	{
		foreach (var bm in m_ButtonMaterials)
		{
			U.Object.Destroy(bm);
		}
	}

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
