using UnityEditor.Experimental.EditorVR.UI;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
	public class ViveProxyHelper : MonoBehaviour
	{
		[SerializeField]
		Tooltip[] m_LeftTooltips;

		[SerializeField]
		Tooltip[] m_RightTooltips;

		/// <summary>
		/// Tooltip components to be removed from a right-handed controller
		/// </summary>
		internal Tooltip[] leftTooltips { get { return m_LeftTooltips; } }

		/// <summary>
		/// Tooltip components to be removed from a left-handed controller
		/// </summary>
		internal Tooltip[] rightTooltips { get { return m_RightTooltips; } }
	}
}
