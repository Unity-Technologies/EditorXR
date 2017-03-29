#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	class SubmenuFace : MonoBehaviour
	{
		[SerializeField]
		Button m_BackButton;

		public void SetupBackButton(UnityAction backAction)
		{
			m_BackButton.onClick.RemoveAllListeners();
			m_BackButton.onClick.AddListener(backAction);
		}
	}
}
#endif