#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.Serialization;
using VisibilityControlType = UnityEditor.Experimental.EditorVR.Core.ProxyAffordanceMap.VisibilityControlType;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
	[Serializable]
	public class AffordanceObject
	{
		[SerializeField]
		VRInputDevice.VRControl m_Control;

		[SerializeField]
		Transform m_Transform;

		[SerializeField]
		Renderer m_Renderer;

		public VRInputDevice.VRControl control { get { return m_Control; } }
		public Transform transform { get { return m_Transform; } }
		public Renderer renderer { get { return m_Renderer; } }
	}

	/// <summary>
	/// Reference container for additional content origins on a device
	/// </summary>
	sealed class ProxyHelper : MonoBehaviour
	{
		List<Renderer> m_BodyRenderers; // renderers not associated with controls, & will be hidden when displaying feedback/tooltips
		bool m_BodyRenderersVisible;

		/// <summary>
		/// The transform that the device's ray contents (default ray, custom ray, etc) will be parented under
		/// </summary>
		public Transform rayOrigin
		{
			get { return m_RayOrigin; }
		}

		[SerializeField]
		private Transform m_RayOrigin;

		/// <summary>
		/// The transform that the menu content will be parented under
		/// </summary>
		public Transform menuOrigin
		{
			get { return m_MenuOrigin; }
		}

		[SerializeField]
		private Transform m_MenuOrigin;

		/// <summary>
		/// The transform that the alternate-menu content will be parented under
		/// </summary>
		public Transform alternateMenuOrigin
		{
			get { return m_AlternateMenuOrigin; }
		}

		[SerializeField]
		private Transform m_AlternateMenuOrigin;

		/// <summary>
		/// The transform that the display/preview objects will be parented under
		/// </summary>
		public Transform previewOrigin
		{
			get { return m_PreviewOrigin; }
		}

		[SerializeField]
		private Transform m_PreviewOrigin;

		/// <summary>
		/// The transform that the display/preview objects will be parented under
		/// </summary>
		public Transform fieldGrabOrigin
		{
			get { return m_FieldGrabOrigin; }
		}

		[SerializeField]
		private Transform m_FieldGrabOrigin;

		/// <summary>
		/// The root transform of the device/controller mesh-renderers/geometry
		/// </summary>
		public Transform meshRoot
		{
			get { return m_MeshRoot; }
		}

		[SerializeField]
		private Transform m_MeshRoot;

		[SerializeField]
		ProxyUI m_ProxyUI;

		[SerializeField]
		AffordanceObject[] m_Affordances;

		public AffordanceObject[] Affordances { get { return m_Affordances; } }

		[SerializeField]
		VisibilityControlType m_BodyVisibilityControlType;

		/// <summary>
		/// Set the visibility of the renderers not associated with controls/input
		/// </summary>
		public bool bodyRenderersVisible { set { if (m_ProxyUI != null) m_ProxyUI.bodyRenderersVisible = value; } }

		void Start()
		{
			m_ProxyUI.Affordances = m_Affordances;
		}

		public void ShowAllControls()
		{
			foreach (var button in m_Affordances)
			{
				button.renderer.enabled = true;
			}
		}

		public void HideAffordances(AffordanceObject[] affordanceObjects)
		{
			// HIDE specific affordances
		}
	}
}
#endif
