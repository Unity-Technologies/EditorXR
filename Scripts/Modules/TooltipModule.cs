using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.UI;

public class TooltipModule : MonoBehaviour
{
	const float kDelay = 0.75f;
	const float kTransitionDuration = 0.3f;
	const float kOffsetDistance = 0.05f;

	[SerializeField]
	GameObject m_TooltipPrefab;

	[SerializeField]
	GameObject m_TooltipCanvasPrefab;

	class TooltipData
	{
		public ITooltip tooltip;
		public float startTime;
		public GameObject tooltipObject;
		public CanvasGroup canvasGroup;
		public Text text;
	}

	readonly Dictionary<Transform, TooltipData> m_Tooltips = new Dictionary<Transform, TooltipData>();

	Transform m_TooltipCanvas;

	void Start()
	{
		m_TooltipCanvas = Instantiate(m_TooltipCanvasPrefab).transform;
		m_TooltipCanvas.SetParent(transform);
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
					tooltipData.canvasGroup = tooltipObject.GetComponent<CanvasGroup>();
					tooltipData.text = tooltipObject.GetComponentInChildren<Text>(true);
				}

				var tooltipText = tooltipData.text;
				if (tooltipText)
					tooltipText.text = tooltipData.tooltip.tooltipText;

				var tooltipTransform = tooltipData.tooltipObject.transform;
				
				tooltipData.canvasGroup.alpha = Mathf.Clamp01((hoverTime - kDelay) / kTransitionDuration);
				var toCamera = (U.Camera.GetMainCamera().transform.position - target.position).normalized;

				tooltipTransform.position = target.position + toCamera * kOffsetDistance;
				tooltipTransform.rotation = Quaternion.LookRotation(-toCamera, Vector3.up);
			}
		}
	}
	
	public void ShowTooltip(ITooltip tooltip)
	{
		if (string.IsNullOrEmpty(tooltip.tooltipText))
			return;

		var target = tooltip.tooltipTarget;
		if (m_Tooltips.ContainsKey(target))
			return;

		m_Tooltips[target] = new TooltipData
		{
			tooltip = tooltip,
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

	static IEnumerator AnimateHide(GameObject tooltipObject)
	{
		var canvasGroup = tooltipObject.GetComponent<CanvasGroup>();
		var startTime = Time.realtimeSinceStartup;
		while (Time.realtimeSinceStartup - startTime < kTransitionDuration)
		{
			canvasGroup.alpha = 1 - (Time.realtimeSinceStartup - startTime) / kTransitionDuration;
			yield return null;
		}

		U.Object.Destroy(tooltipObject);
	}
}
