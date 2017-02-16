using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;

public class TooltipModule : MonoBehaviour, IUsesCameraRig
{
	const float kDelay = 0; // In case we want to bring back a delay
	const float kTransitionDuration = 0.1f;
	const float kUVScale = 100f;
	const float kUVScrollSpeed = 1.5f;

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
		public float startTime;
		public TooltipUI tooltipUI;
	}

	readonly Dictionary<ITooltip, TooltipData> m_Tooltips = new Dictionary<ITooltip, TooltipData>();

	Transform m_TooltipCanvas;
	Vector3 m_TooltipScale;

	public Transform cameraRig { get; set; }

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
			var tooltip = kvp.Key;
			var tooltipData = kvp.Value;
			var hoverTime = Time.realtimeSinceStartup - tooltipData.startTime;
			if (hoverTime > kDelay)
			{
				var target = tooltip.tooltipTarget;

				var tooltipUI = tooltipData.tooltipUI;
				if (!tooltipUI)
				{
					var tooltipObject = (GameObject)Instantiate(m_TooltipPrefab, m_TooltipCanvas);
					tooltipUI = tooltipObject.GetComponent<TooltipUI>();
					tooltipData.tooltipUI = tooltipUI;
					tooltipUI.highlight.material = m_HighlightMaterial;
					var tooltipTransform = tooltipObject.transform;
					U.Math.SetTransformOffset(target, tooltipTransform, Vector3.zero, Quaternion.identity);
					tooltipTransform.localScale = Vector3.zero;
				}

				var lerp = Mathf.Clamp01((hoverTime - kDelay) / kTransitionDuration);
				UpdateVisuals(tooltip, tooltipUI, lerp);
			}
		}
	}

	void UpdateVisuals(ITooltip tooltip, TooltipUI tooltipUI, float lerp)
	{
		var target = tooltip.tooltipTarget;
		var tooltipTransform = tooltipUI.transform;

		var tooltipText = tooltipUI.text;
		if (tooltipText)
			tooltipText.text = tooltip.tooltipText;

		tooltipTransform.localScale = m_TooltipScale * lerp * cameraRig.localScale.x;

		// Adjust for alignment
		var offset = Vector3.zero;
		switch (tooltip.tooltipAlignment)
		{
			case TextAlignment.Right:
				offset = Vector3.left;
				break;
			case TextAlignment.Left:
				offset = Vector3.right;
				break;
		}

		var rectTransform = tooltipUI.GetComponent<RectTransform>();
		var rect = rectTransform.rect;
		var halfWidth = rect.width * 0.5f;
		var halfHeight = rect.height * 0.5f;
		
		offset *= halfWidth * rectTransform.lossyScale.x;

		U.Math.SetTransformOffset(target, tooltipTransform, offset * lerp, Quaternion.identity);

		var source = tooltip.tooltipSource;
		var toSource = tooltipTransform.InverseTransformPoint(source.position);

		// Position spheres: one at source, one on the closest edge of the tooltip
		var spheres = tooltipUI.spheres;
		spheres[0].position = source.position;

		var attachedSphere = spheres[1];
		var boxSlope = halfHeight / halfWidth;
		var toSourceSlope = Mathf.Abs(toSource.y / toSource.x);

		halfHeight *= Mathf.Sign(toSource.y);
		halfWidth *= Mathf.Sign(toSource.x);
		attachedSphere.localPosition = toSourceSlope > boxSlope
			? new Vector3(0, halfHeight) : new Vector3(halfWidth, 0);

		// Align dotted line
		var attachedSpherePosition = attachedSphere.position;
		toSource = source.position - attachedSpherePosition;
		var midPoint = attachedSpherePosition + toSource * 0.5f;
		var dottedLine = tooltipUI.dottedLine;
		var length = toSource.magnitude;
		var uvRect = dottedLine.uvRect;
		uvRect.width = length * kUVScale;
		uvRect.xMin += kUVScrollSpeed * Time.unscaledDeltaTime;
		dottedLine.uvRect = uvRect;

		var dottedLineTransform = dottedLine.transform.parent.GetComponent<RectTransform>();
		dottedLineTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, length / tooltipTransform.lossyScale.x);
		dottedLineTransform.position = midPoint;
		dottedLineTransform.rotation = Quaternion.LookRotation(toSource, -tooltipTransform.forward);
	}

	public void OnRayEntered(GameObject gameObject, RayEventData eventData)
	{
		var tooltip = gameObject.GetComponent<ITooltip>();
		if (tooltip != null)
			ShowTooltip(tooltip);
	}

	public void OnRayExited(GameObject gameObject, RayEventData eventData)
	{
		var tooltip = gameObject.GetComponent<ITooltip>();
		if (tooltip != null)
			HideTooltip(tooltip);
	}

	public void ShowTooltip(ITooltip tooltip)
	{
		if (string.IsNullOrEmpty(tooltip.tooltipText))
			return;

		if (m_Tooltips.ContainsKey(tooltip))
			return;

		m_Tooltips[tooltip] = new TooltipData
		{
			startTime = Time.realtimeSinceStartup
		};
	}

	public void HideTooltip(ITooltip tooltip)
	{
		TooltipData tooltipData;
		if (m_Tooltips.TryGetValue(tooltip, out tooltipData))
		{
			m_Tooltips.Remove(tooltip);

			if (tooltipData.tooltipUI)
				StartCoroutine(AnimateHide(tooltip, tooltipData.tooltipUI));
		}
	}

	IEnumerator AnimateHide(ITooltip tooltip, TooltipUI tooltipUI)
	{
		var startTime = Time.realtimeSinceStartup;
		while (Time.realtimeSinceStartup - startTime < kTransitionDuration)
		{
			UpdateVisuals(tooltip, tooltipUI,
				1 - (Time.realtimeSinceStartup - startTime) / kTransitionDuration);
			yield return null;
		}

		U.Object.Destroy(tooltipUI.gameObject);
	}
}
