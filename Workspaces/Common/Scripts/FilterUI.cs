using System;
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
	private RectTransform m_VisibilityButton;

	[SerializeField]
	private RectTransform m_SummaryButton;

	[SerializeField]
	private RectTransform m_VisibilityPanel;

	[SerializeField]
	private RectTransform m_TypePanel;

	[SerializeField]
	private GameObject m_VisibilityButtonPrefab;

	[SerializeField]
	private GameObject m_VisibilityLabelPrefab;

	private Button[] m_VisibilityButtons;
	private Button[] m_VisibilityLabels;

	public string searchQuery { get { return m_SearchQuery; } }
	private string m_SearchQuery = string.Empty;

	private void Start()
	{
		//Make the list panel a child of the main panel so it stays aligned right
		m_ListPanel.transform.SetParent(transform.parent, false);

		m_VisibilityButtons = new Button[kFilterTypes.Length + 1];
		m_VisibilityLabels = new Button[kFilterTypes.Length + 1];
		for (int i = 0; i < kFilterTypes.Length + 1; i++)
		{
			var button = U.Object.InstantiateAndSetActive(m_VisibilityButtonPrefab, m_VisibilityPanel, false).GetComponent<Button>();
			m_VisibilityButtons[i] = button;
			button.onClick.AddListener(() =>
			{
				OnFilterClick(button);
			});
			var label = U.Object.InstantiateAndSetActive(m_VisibilityLabelPrefab, m_TypePanel, false).GetComponentInChildren<Button>();
			m_VisibilityLabels[i] = label;
			label.onClick.AddListener(() =>
			{
				OnFilterClick(label);
			});
			label.GetComponentInChildren<Text>().text = i == 0 ? "All/None" : kFilterTypes[i - 1];
		}
	}

	public void ShowList()
	{
		m_ListPanel.gameObject.SetActive(true);
		m_VisibilityButton.gameObject.SetActive(false);
		m_SummaryButton.gameObject.SetActive(false);
	}

	public void HideList()
	{
		m_ListPanel.gameObject.SetActive(false);
		m_VisibilityButton.gameObject.SetActive(true);
		m_SummaryButton.gameObject.SetActive(true);
	}

	public void OnFilterClick(Button button)
	{
		for (int i = 0; i < m_VisibilityButtons.Length; i++)
		{
			if (button == m_VisibilityButtons[i] || button == m_VisibilityLabels[i])
			{
				m_SearchQuery = i == 0 ? string.Empty : "t:" + kFilterTypes[i - 1];
			}
		}
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