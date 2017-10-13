#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
	/// <summary>
	/// Affordance model; data defining a proxy affordance (button/analog/etc)
	/// </summary>
	[Serializable]
	public class Affordance
	{
		[SerializeField]
		VRInputDevice.VRControl m_Control;

		[SerializeField]
		Transform m_Transform;

		[SerializeField]
		Tooltip[] m_Tooltips;

		[SerializeField]
		Renderer m_Renderer;

		[FlagsProperty]
		[SerializeField]
		AxisFlags m_TranslateAxes;

		[FlagsProperty]
		[SerializeField]
		AxisFlags m_RotateAxes;

		[SerializeField]
		float m_Min;

		[SerializeField]
		float m_Max;

		public VRInputDevice.VRControl control { get { return m_Control; } }
		public Transform transform { get { return m_Transform; } }
		public Renderer renderer { get { return m_Renderer; } }
		public Tooltip[] tooltips { get { return m_Tooltips; } }
		public AxisFlags translateAxes { get { return m_TranslateAxes; } }
		public AxisFlags rotateAxes { get { return m_RotateAxes; } }
		public float min { get { return m_Min; } }
		public float max { get { return m_Max; } }
	}
}
#endif
