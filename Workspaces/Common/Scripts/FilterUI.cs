using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Utilities;

public class FilterUI : MonoBehaviour {
	[SerializeField]
	private RectTransform m_ListPanel;

	public Text summaryText { get { return m_SummaryText; } }
	[SerializeField]
	private Text m_SummaryText;

	public Text descriptionText { get { return m_DescriptionText; } }
	[SerializeField]
	private Text m_DescriptionText;
	
	[SerializeField]
	private RectTransform m_VisibilityPanel;

	[SerializeField]
	private RectTransform m_TypePanel;

	[SerializeField]
	private GameObject m_VisibilityButtonPrefab;

	[SerializeField]
	private GameObject m_VisibilityLabelPrefab;

	private static readonly string[] kFilterTypes = new string[]{
		"AnimationClip",
		"AudioClip",
		"AudioMixer",
		"Font",
		"GUISkin",
		"Material",
		"Mesh",
		"Model",
		"PhysicMaterial",
		"Prefab",
		"Scene",
		"Script",
		"Shader",
		"Sprite",
		"Texture"
	};

	private Button[] m_VisibilityButtons;
	private Button[] m_VisibilityLabels;

	private void Start()
	{
		m_VisibilityButtons = new Button[kFilterTypes.Length + 1];
		m_VisibilityLabels = new Button[kFilterTypes.Length + 1];
		for (int i = 0; i < kFilterTypes.Length + 1; i++)
		{
			var button = U.Object.InstantiateAndSetActive(m_VisibilityButtonPrefab, m_VisibilityPanel, false);
			m_VisibilityButtons[i] = button.GetComponentInChildren<Button>();
			var label = U.Object.InstantiateAndSetActive(m_VisibilityLabelPrefab, m_TypePanel, false);
			m_VisibilityLabels[i] = label.GetComponentInChildren<Button>();
			if(i == 0)
			{
				label.GetComponentInChildren<Text>().text = "All/None";
			}
			else
			{
				label.GetComponentInChildren<Text>().text = kFilterTypes[i - 1];
			}
		}
	}

	public void ShowList()
	{
		m_ListPanel.gameObject.SetActive(true);
	}

	public void HideList()
	{
		m_ListPanel.gameObject.SetActive(false);
	}
}