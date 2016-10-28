using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;

public class SubmenuFace : MonoBehaviour
{

	[SerializeField]
	Button m_BackButton;

	public void SetupBackButton(UnityAction backAction)
	{
		m_BackButton.onClick.RemoveAllListeners();
		m_BackButton.onClick.AddListener(backAction);
	}

}
