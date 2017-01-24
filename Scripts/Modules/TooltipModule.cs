using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.UI;

public class TooltipModule : MonoBehaviour
{
	const float kDelay = 0; // In case we want to bring back a delay
	const float kTransitionDuration = 0.1f;

	const string kMaterialColorTopProperty = "_ColorTop";
	const string kMaterialColorBottomProperty = "_ColorBottom";

	[SerializeField]
	GameObject m_TooltipPrefab;

	[SerializeField]
	GameObject m_TooltipCanvasPrefab;

	[SerializeField]
	Material m_HighlightMaterial;

	class TooltipData
	{
		public ITooltip tooltip;
		public bool centered;
		public float startTime;
		public GameObject tooltipObject;
		public Text text;
	}

	readonly Dictionary<Transform, TooltipData> m_Tooltips = new Dictionary<Transform, TooltipData>();

	Transform m_TooltipCanvas;
	Vector3 m_TooltipScale;

	void Start()
	{
		m_TooltipCanvas = Instantiate(m_TooltipCanvasPrefab).transform;
		m_TooltipCanvas.SetParent(transform);
		m_TooltipScale = m_TooltipPrefab.transform.localScale;
		m_HighlightMaterial = Instantiate(m_HighlightMaterial);
		var sessionGradient = UnityBrandColorScheme.sessionGradient;
		m_HighlightMaterial.SetColor(kMaterialColorTopProperty, sessionGradient.a);
		m_HighlightMaterial.SetColor(kMaterialColorBottomProperty, sessionGradient.b);
	}

	void Update()
	{
		foreach (var kvp in m_Tooltips)
		{
			var target = kvp.Key;
			var tooltipData = kvp.Value;
			var hoverTime = Time.realtimeSinceStartup - tooltipData.startTime;
			if (hoverTime > kDelay)
			{
				if (!tooltipData.tooltipObject)
				{
					var tooltipObject = (GameObject)Instantiate(m_TooltipPrefab, m_TooltipCanvas);
					tooltipData.tooltipObject = tooltipObject;
					tooltipData.text = tooltipObject.GetComponentInChildren<Text>(true);
					tooltipObject.transform.Find("TooltipHighlight").GetComponent<Image>().material = m_HighlightMaterial;
				}

				var tooltipTransform = tooltipData.tooltipObject.transform;

				var tooltipText = tooltipData.text;
				if (tooltipText)
					tooltipText.text = tooltipData.tooltip.tooltipText;

				var lerp = Mathf.Clamp01((hoverTime - kDelay) / kTransitionDuration);
				tooltipTransform.localScale = m_TooltipScale * lerp;

				var rectTransform = tooltipData.tooltipObject.GetComponent<RectTransform>();
				var offset = Vector3.zero;
				if (!tooltipData.centered)
					offset += Vector3.left * rectTransform.rect.width * 0.5f * rectTransform.lossyScale.x;

				var rotation = Quaternion.identity;
				if (Vector3.Dot(target.up, Vector3.up) < 0)
					rotation = Quaternion.AngleAxis(180, Vector3.forward);

				U.Math.SetTransformOffset(target, tooltipTransform, offset * lerp, rotation);
			}
		}
	}
	
	public void ShowTooltip(ITooltip tooltip, bool centered = true)
	{
		if (string.IsNullOrEmpty(tooltip.tooltipText))
			return;

		var target = tooltip.tooltipTarget;
		if (m_Tooltips.ContainsKey(target))
			return;

		m_Tooltips[target] = new TooltipData
		{
			tooltip = tooltip,
			centered = centered,
			startTime = Time.realtimeSinceStartup
		};
	}

	public void HideTooltip(ITooltip tooltip)
	{
		var target = tooltip.tooltipTarget;
		TooltipData tooltipData;
		if (m_Tooltips.TryGetValue(target, out tooltipData))
		{
			if (tooltipData.tooltipObject)
				StartCoroutine(AnimateHide(tooltipData.tooltipObject));

			m_Tooltips.Remove(target);
		}
	}

	IEnumerator AnimateHide(GameObject tooltipObject)
	{
		var startTime = Time.realtimeSinceStartup;
		while (Time.realtimeSinceStartup - startTime < kTransitionDuration)
		{
			tooltipObject.transform.localScale = m_TooltipScale * (1 - (Time.realtimeSinceStartup - startTime) / kTransitionDuration);
			yield return null;
		}

		U.Object.Destroy(tooltipObject);
	}
}
