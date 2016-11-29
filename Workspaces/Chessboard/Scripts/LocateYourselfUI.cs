using UnityEngine;
using UnityEngine.UI;

public class LocateYourselfUI : MonoBehaviour
{
	public Button locateButton { get { return m_LocateButton; } }
	[SerializeField]
	private Button m_LocateButton;

	public Button resetButton { get { return m_ResetButton; } }
	[SerializeField]
	private Button m_ResetButton;
}
