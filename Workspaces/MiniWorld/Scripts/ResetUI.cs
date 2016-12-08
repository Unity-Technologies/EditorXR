using UnityEngine;
using UnityEngine.UI;

public class ResetUI : MonoBehaviour
{
	public Button resetButton { get { return m_ResetButton; } }
	[SerializeField]
	Button m_ResetButton;
}
