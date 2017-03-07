#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class TooltipUI : MonoBehaviour
	{
		public Text text { get { return m_Text; } }
		[SerializeField]
		Text m_Text;

		public RawImage dottedLine { get { return m_DottedLine; } }
		[SerializeField]
		RawImage m_DottedLine;

		public Transform[] spheres { get { return m_Spheres; } }
		[SerializeField]
		Transform[] m_Spheres;

		public Image highlight { get { return m_Highlight; } }
		[SerializeField]
		Image m_Highlight;
	}
}
#endif