#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
	/// <summary>
	/// Reference container for additional content origins on a device
	/// </summary>
	sealed class ProxyHelper : MonoBehaviour
	{
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
	}
}
#endif
