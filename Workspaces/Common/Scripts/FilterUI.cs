using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.EditorVR.Extensions;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;

public class FilterUI : MonoBehaviour, IUsesStencilRef
{
	private const string kAllText = "All";

	public Text summaryText { get { return m_SummaryText; } }
	[SerializeField]
	private Text m_SummaryText;

	public Text descriptionText { get { return m_DescriptionText; } }
	[SerializeField]
	private Text m_DescriptionText;

	[SerializeField]
	private RectTransform m_ButtonList;

	[SerializeField]
	private GameObject m_ButtonPrefab;

	[SerializeField]
	private Color m_ActiveColor;

	[SerializeField]
	private Color m_DisableColor;

	[SerializeField]
	CanvasGroup m_CanvasGroup;

	[SerializeField]
	GridLayoutGroup m_ButtonListGrid;

	[SerializeField]
	CanvasGroup m_ButtonListCanvasGroup;

	[SerializeField]
	MeshRenderer m_Background;

	public string searchQuery { get { return m_SearchQuery; } }
	string m_SearchQuery = string.Empty;

	private FilterButtonUI[] m_VisibilityButtons;
	Coroutine m_ShowUICoroutine;
	Coroutine m_HideUICoroutine;
	Coroutine m_ShowButtonListCoroutine;
	Coroutine m_HideButtonListCoroutine;
	float m_HiddenButtonListYSpacing;
	List<string> m_FilterTypes;
	Material m_BackgroundMaterial;

	public List<string> filterList
	{
		set
		{
			// Clean up old buttons
			if (m_VisibilityButtons != null)
				foreach (var button in m_VisibilityButtons)
					U.Object.Destroy(button.gameObject);

			m_FilterTypes = value;
			m_FilterTypes.Sort();
			m_FilterTypes.Insert(0, kAllText);

			// Generate new button list
			m_VisibilityButtons = new FilterButtonUI[m_FilterTypes.Count];
			for (int i = 0; i < m_VisibilityButtons.Length; i++)
			{
				var button = U.Object.Instantiate(m_ButtonPrefab, m_ButtonList, false).GetComponent<FilterButtonUI>();
				m_VisibilityButtons[i] = button;

				button.button.onClick.AddListener(() =>
				{
					OnFilterClick(button);
				});

				button.text.text = m_FilterTypes[i];
			}
		}
	}

	public byte stencilRef { get; set; }

	void Awake()
	{
		m_HiddenButtonListYSpacing = -m_ButtonListGrid.cellSize.y;
	}

	void Start()
	{
		m_BackgroundMaterial = U.Material.GetMaterialClone(m_Background);
		m_BackgroundMaterial.SetInt("_StencilRef", stencilRef);
	}

	void OnDestroy()
	{
		U.Object.Destroy(m_BackgroundMaterial);
	}

	public void SetListVisibility(bool show)
	{
		if (show)
		{
			this.StopCoroutine(ref m_HideUICoroutine);
			m_HideUICoroutine = StartCoroutine(HideUIContent());

			this.StopCoroutine(ref m_ShowButtonListCoroutine);
			m_ShowButtonListCoroutine = StartCoroutine(ShowButtonList());
		}
		else
		{
			this.StopCoroutine(ref m_ShowUICoroutine);
			m_ShowUICoroutine = StartCoroutine(ShowUIContent());

			this.StopCoroutine(ref m_HideButtonListCoroutine);
			m_HideButtonListCoroutine = StartCoroutine(HideButtonList());
		}
	}

	public void OnFilterClick(FilterButtonUI clickedButton)
	{
		for (int i = 0; i < m_VisibilityButtons.Length; i++)
			if (clickedButton == m_VisibilityButtons[i])
				m_SearchQuery = i == 0 ? string.Empty : "t:" + m_FilterTypes[i];

		foreach (FilterButtonUI button in m_VisibilityButtons)
		{
			if (button == clickedButton)
				button.color = m_ActiveColor;
			else
				button.color = m_SearchQuery.Contains("t:") ? m_DisableColor : m_ActiveColor;
		}

		if (clickedButton.text.text.Equals(kAllText))
		{
			m_SummaryText.text = clickedButton.text.text;
			m_DescriptionText.text = "All objects are visible";
		}
		else
		{
			m_SummaryText.text = clickedButton.text.text + "s";
			m_DescriptionText.text = "Only " + m_SummaryText.text + " are visible";
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

	IEnumerator ShowUIContent()
	{
		var currentAlpha = m_CanvasGroup.alpha;
		var kTargetAlpha = 1f;
		var transitionAmount = Time.unscaledDeltaTime;
		while (transitionAmount < 1)
		{
			m_CanvasGroup.alpha = Mathf.Lerp(currentAlpha, kTargetAlpha, transitionAmount);
			transitionAmount = transitionAmount + Time.unscaledDeltaTime;
			yield return null;
		}

		m_CanvasGroup.alpha = kTargetAlpha;
		m_ShowUICoroutine = null;
	}

	IEnumerator HideUIContent()
	{
		var currentAlpha = m_CanvasGroup.alpha;
		var kTargetAlpha = 0f;
		var transitionAmount = Time.unscaledDeltaTime;
		var kSpeedMultiplier = 3;
		while (transitionAmount < 1)
		{
			m_CanvasGroup.alpha = Mathf.Lerp(currentAlpha, kTargetAlpha, transitionAmount);
			transitionAmount = transitionAmount + Time.unscaledDeltaTime * kSpeedMultiplier;
			yield return null;
		}

		m_CanvasGroup.alpha = kTargetAlpha;
		m_HideUICoroutine = null;
	}

	IEnumerator ShowButtonList()
	{
		m_ButtonList.gameObject.SetActive(true);

		const float kTargetDuration = 0.5f;
		var currentAlpha = m_ButtonListCanvasGroup.alpha;
		var kTargetAlpha = 1f;
		var transitionAmount = 0f;
		var velocity = 0f;
		var currentDuration = 0f;
		while (currentDuration < kTargetDuration)
		{
			currentDuration += Time.unscaledDeltaTime;
			transitionAmount = U.Math.SmoothDamp(transitionAmount, 1f, ref velocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
			m_ButtonListGrid.spacing = new Vector2(0f, Mathf.Lerp(m_HiddenButtonListYSpacing, 0f, transitionAmount));
			m_ButtonListCanvasGroup.alpha = Mathf.Lerp(currentAlpha, kTargetAlpha, transitionAmount);
			yield return null;
		}

		m_ButtonListGrid.spacing = new Vector2(0f, 0f);
		m_ButtonListCanvasGroup.alpha = 1f;
		m_ShowButtonListCoroutine = null;
	}

	IEnumerator HideButtonList()
	{
		const float kTargetDuration = 0.25f;
		var currentAlpha = m_ButtonListCanvasGroup.alpha;
		var kTargetAlpha = 0f;
		var transitionAmount = 0f;
		var currentSpacing = m_ButtonListGrid.spacing.y;
		var velocity = 0f;
		var currentDuration = 0f;
		while (currentDuration < kTargetDuration)
		{
			currentDuration += Time.unscaledDeltaTime;
			transitionAmount = U.Math.SmoothDamp(transitionAmount, 1f, ref velocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
			m_ButtonListGrid.spacing = new Vector2(0f, Mathf.Lerp(currentSpacing, m_HiddenButtonListYSpacing, transitionAmount));
			m_ButtonListCanvasGroup.alpha = Mathf.Lerp(currentAlpha, kTargetAlpha, transitionAmount);
			yield return null;
		}

		m_ButtonList.gameObject.SetActive(false);
		m_HideButtonListCoroutine = null;
	}
}