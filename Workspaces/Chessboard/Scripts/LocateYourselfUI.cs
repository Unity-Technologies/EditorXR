using UnityEngine;
using UnityEngine.UI;

public class LocateYourselfUI : MonoBehaviour
{
	public Button resetButton { get { return m_ResetButton; } }
	[SerializeField]
	private Button m_ResetButton;
}
