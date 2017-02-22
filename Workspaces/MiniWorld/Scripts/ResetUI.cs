using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.UI
{
	internal sealed class ResetUI : MonoBehaviour
	{
		public UnityEngine.UI.Button resetButton
		{
			get { return m_ResetButton; }
		}

		[SerializeField]
		UnityEngine.UI.Button m_ResetButton;
	}
}
