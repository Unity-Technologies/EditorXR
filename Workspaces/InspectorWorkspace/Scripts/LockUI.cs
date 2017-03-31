#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	sealed class LockUI : MonoBehaviour, IUsesStencilRef
	{
		[SerializeField]
		Image m_LockImage;

		[SerializeField]
		Sprite m_LockIcon;

		[SerializeField]
		Sprite m_UnlockIcon;

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
}
#endif
