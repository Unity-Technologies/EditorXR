using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Utilities;

public class FilterUI : MonoBehaviour {
	private static readonly string[] kFilterTypes = {
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

	[SerializeField]
	private RectTransform m_ListPanel;

	public Text summaryText { get { return m_SummaryText; } }
	[SerializeField]
	private Text m_SummaryText;

	public Text descriptionText { get { return m_DescriptionText; } }
	[SerializeField]
	private Text m_DescriptionText;

	[SerializeField]
	private GameObject m_VisibilityButton;

	[SerializeField]
	private GameObject m_SummaryButton;

	[SerializeField]
	private RectTransform m_ButtonList;

	[SerializeField]
	private GameObject m_ButtonPrefab;

	[SerializeField]
	private Color m_ActiveColor;

	[SerializeField]
	private Color m_DisableColor;

	private FilterButtonUI[] m_VisibilityButtons;

	public string searchQuery { get { return m_SearchQuery; } }
	private string m_SearchQuery = string.Empty;

	private void Start()
	{
		//Make the list panel a child of the main panel so it stays aligned right
		m_ListPanel.SetParent(transform.parent, false);

		m_VisibilityButtons = new FilterButtonUI[kFilterTypes.Length + 1];
		for (int i = 0; i < kFilterTypes.Length + 1; i++)
		{
			var button = U.Object.Instantiate(m_ButtonPrefab, m_ButtonList, false).GetComponent<FilterButtonUI>();
			m_VisibilityButtons[i] = button;
			button.button.onClick.AddListener(() =>
			{
				OnFilterClick(button);
			});
			button.text.text = i == 0 ? "All" : kFilterTypes[i - 1];
		}
	}

	public void ShowList()
	{
		m_ListPanel.gameObject.SetActive(true);
		m_VisibilityButton.SetActive(false);
		m_SummaryButton.SetActive(false);
	}

	public void HideList()
	{
		m_ListPanel.gameObject.SetActive(false);
		m_VisibilityButton.SetActive(true);
		m_SummaryButton.SetActive(true);
	}

	public void OnFilterClick(FilterButtonUI clickedButton)
	{
		for (int i = 0; i < m_VisibilityButtons.Length; i++)
			if (clickedButton == m_VisibilityButtons[i])
				m_SearchQuery = i == 0 ? string.Empty : "t:" + kFilterTypes[i - 1];

		foreach (FilterButtonUI button in m_VisibilityButtons)
		{
			if (button == clickedButton)
				button.color = m_ActiveColor;
			else
				button.color = m_SearchQuery.Contains("t:") ? m_DisableColor : m_ActiveColor;
		}

		m_SummaryText.text = clickedButton.text.text + "s";
		m_DescriptionText.text = "Only " + m_SummaryText.text + " are visible";
	}

	public static bool TestFilter(string query, string type)
	{
		var pieces = query.Split(':');
		if (pieces.Length > 1)
		{
			if (pieces[1].StartsWith(type))
				return true;
		}
		else
		{
			return true;
		}
		return false;
	}
}