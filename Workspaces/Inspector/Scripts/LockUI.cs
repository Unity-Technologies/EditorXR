#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
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

		List<Material> m_ButtonMaterials = new List<Material>();

		public byte stencilRef { get; set; }

		public event Action lockButtonPressed;

		void Start()
		{
			var mr = GetComponentInChildren<MeshRenderer>();
			foreach (var sm in mr.sharedMaterials)
			{
				var material = Instantiate<Material>(sm);
				material.SetInt("_StencilRef", stencilRef);
				m_ButtonMaterials.Add(material);
			}
			mr.sharedMaterials = m_ButtonMaterials.ToArray();
		}

		void OnDestroy()
		{
			foreach (var bm in m_ButtonMaterials)
			{
				ObjectUtils.Destroy(bm);
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
