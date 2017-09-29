#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class TooltipModule : MonoBehaviour, IUsesViewerScale
	{
		const float k_Delay = 0; // In case we want to bring back a delay
		const float k_TransitionDuration = 0.1f;
		const float k_UVScale = 100f;
		const float k_UVScrollSpeed = 1.5f;
		const float k_Offset = 0.05f;

		const string k_MaterialColorTopProperty = "_ColorTop";
		const string k_MaterialColorBottomProperty = "_ColorBottom";

		[SerializeField]
		GameObject m_TooltipPrefab;

		[SerializeField]
		GameObject m_TooltipCanvasPrefab;

		[SerializeField]
		Material m_HighlightMaterial;

		[SerializeField]
		Material m_TooltipBackgroundMaterial;

		class TooltipData
		{
			public float startTime;
			public float lastModifiedTime;
			public TooltipUI tooltipUI;
			public Material customHighlightMaterial;
			public bool persistent;
			public float duration;
		}

		readonly Dictionary<ITooltip, TooltipData> m_Tooltips = new Dictionary<ITooltip, TooltipData>();

		Transform m_TooltipCanvas;
		Vector3 m_TooltipScale;
		Color m_OriginalBackgroundColor;

		// Local method use only -- created here to reduce garbage collection
		readonly List<ITooltip> m_TooltipsToHide = new List<ITooltip>();

		void Awake()
		{
			ISetTooltipVisibilityMethods.showTooltip = ShowTooltip;
			ISetTooltipVisibilityMethods.hideTooltip = HideTooltip;
		}

		void Start()
		{
			m_TooltipCanvas = Instantiate(m_TooltipCanvasPrefab).transform;
			m_TooltipCanvas.SetParent(transform);
			m_TooltipScale = m_TooltipPrefab.transform.localScale;
			m_HighlightMaterial = Instantiate(m_HighlightMaterial);
			m_TooltipBackgroundMaterial = Instantiate(m_TooltipBackgroundMaterial);
			m_OriginalBackgroundColor = m_TooltipBackgroundMaterial.color;
			var sessionGradient = UnityBrandColorScheme.sessionGradient;
			m_HighlightMaterial.SetColor(k_MaterialColorTopProperty, sessionGradient.a);
			m_HighlightMaterial.SetColor(k_MaterialColorBottomProperty, sessionGradient.b);
		}

		void Update()
		{
			m_TooltipsToHide.Clear();
			foreach (var kvp in m_Tooltips)
			{
				var tooltip = kvp.Key;
				var tooltipData = kvp.Value;
				var hoverTime = Time.time - tooltipData.startTime;
				if (hoverTime > k_Delay)
				{
					var placement = tooltip as ITooltipPlacement;
					var target = GetTooltipTarget(tooltip);

					var tooltipUI = tooltipData.tooltipUI;
					if (!tooltipUI)
					{
						var tooltipObject = Instantiate(m_TooltipPrefab, m_TooltipCanvas);
						tooltipUI = tooltipObject.GetComponent<TooltipUI>();
						tooltipData.tooltipUI = tooltipUI;
						tooltipUI.highlight.material = tooltipData.customHighlightMaterial ?? m_HighlightMaterial;
						tooltipUI.background.material = m_TooltipBackgroundMaterial;
						var tooltipTransform = tooltipObject.transform;
						MathUtilsExt.SetTransformOffset(target, tooltipTransform, Vector3.zero, Quaternion.identity);
						tooltipTransform.localScale = Vector3.zero;

						if (placement == null)
						{
							ObjectUtils.Destroy(tooltipUI.dottedLine.gameObject);
							foreach (var sphere in tooltipUI.spheres)
							{
								ObjectUtils.Destroy(sphere.gameObject);
							}
						}
					}

					var lerp = Mathf.Clamp01((hoverTime - k_Delay) / k_TransitionDuration);
					UpdateVisuals(tooltip, tooltipUI, target, lerp);
				}

				if (!IsValidTooltip(tooltip))
					m_TooltipsToHide.Add(tooltip);

				if (tooltipData.persistent)
				{
					var duration = tooltipData.duration;
					if (duration > 0 && Time.time - tooltipData.lastModifiedTime + k_Delay > duration)
						m_TooltipsToHide.Add(tooltip);
				}
			}

			foreach (var tooltip in m_TooltipsToHide)
			{
				HideTooltip(tooltip, true);
			}
		}

		static Transform GetTooltipTarget(ITooltip tooltip)
		{
			var placement = tooltip as ITooltipPlacement;
			if (placement != null)
				return placement.tooltipTarget;

			return ((MonoBehaviour)tooltip).transform;
		}

		void UpdateVisuals(ITooltip tooltip, TooltipUI tooltipUI, Transform target, float lerp)
		{
			var tooltipTransform = tooltipUI.transform;

			lerp = MathUtilsExt.SmoothInOutLerpFloat(lerp); // shape the lerp for better presentation

			var tooltipText = tooltipUI.text;
			if (tooltipText)
			{
				tooltipText.text = tooltip.tooltipText;
				tooltipText.color = Color.Lerp(Color.clear, Color.white, lerp);
			}

			var viewerScale = this.GetViewerScale();
			tooltipTransform.localScale = m_TooltipScale * lerp * viewerScale;

			m_TooltipBackgroundMaterial.SetColor("_Color", Color.Lerp(UnityBrandColorScheme.darker, m_OriginalBackgroundColor, lerp));

			var placement = tooltip as ITooltipPlacement;

			// Adjust for alignment
			var offset = Vector3.zero;
			if (placement != null)
			{
				switch (placement.tooltipAlignment)
				{
					case TextAlignment.Right:
						offset = Vector3.left;
						break;
					case TextAlignment.Left:
						offset = Vector3.right;
						break;
				}
			}

			var rectTransform = tooltipUI.GetComponent<RectTransform>();
			var rect = rectTransform.rect;
			var halfWidth = rect.width * 0.5f;
			var halfHeight = rect.height * 0.5f;

			if (placement != null)
				offset *= halfWidth * rectTransform.lossyScale.x;
			else
				offset = Vector3.back * k_Offset * this.GetViewerScale();

			MathUtilsExt.SetTransformOffset(target, tooltipTransform, offset * lerp, Quaternion.identity);

			if (placement != null)
			{
				var source = placement.tooltipSource;
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
					? new Vector3(0, halfHeight)
					: new Vector3(halfWidth, 0);

				// Align dotted line
				var attachedSpherePosition = attachedSphere.position;
				toSource = source.position - attachedSpherePosition;
				var midPoint = attachedSpherePosition + toSource * 0.5f;
				var dottedLine = tooltipUI.dottedLine;
				var length = toSource.magnitude;
				var uvRect = dottedLine.uvRect;
				var worldScale = 1 / viewerScale;
				uvRect.width = length * k_UVScale * worldScale;
				uvRect.xMin += k_UVScrollSpeed * Time.deltaTime;
				dottedLine.uvRect = uvRect;

				var dottedLineTransform = dottedLine.transform.parent.GetComponent<RectTransform>();
				dottedLineTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, length / tooltipTransform.lossyScale.x);
				dottedLineTransform.position = midPoint;
				dottedLineTransform.rotation = Quaternion.LookRotation(toSource, -tooltipTransform.forward);
			}
		}

		public void OnRayEntered(GameObject gameObject, RayEventData eventData)
		{
			if (gameObject == this.gameObject)
				return;

			var tooltip = gameObject.GetComponent<ITooltip>();
			if (tooltip != null)
				ShowTooltip(tooltip);
		}

		public void OnRayHovering(GameObject gameObject, RayEventData eventData)
		{
			if (gameObject == this.gameObject)
				return;

			var tooltip = gameObject.GetComponent<ITooltip>();
			if (tooltip != null)
				ShowTooltip(tooltip);
		}

		public void OnRayExited(GameObject gameObject, RayEventData eventData)
		{
			if (gameObject && gameObject != this.gameObject)
			{
				var tooltip = gameObject.GetComponent<ITooltip>();
				if (tooltip != null)
					HideTooltip(tooltip);
			}
		}

		public void ShowTooltip(ITooltip tooltip, bool persistent = false, float duration = 0f)
		{
			if (!IsValidTooltip(tooltip))
				return;

			TooltipData data;
			if (m_Tooltips.TryGetValue(tooltip, out data))
			{
				data.persistent |= persistent;
				if (duration > 0)
				{
					data.duration = duration;
					data.lastModifiedTime = Time.time;
				}

				return;
			}

			Material highlightMaterial = null;
			var customTooltipColor = tooltip as ISetCustomTooltipColor;
			if (customTooltipColor != null)
			{
				highlightMaterial = Instantiate(m_HighlightMaterial);
				var customTooltipHighlightColor = customTooltipColor.customTooltipHighlightColor;
				highlightMaterial.SetColor(k_MaterialColorTopProperty, customTooltipHighlightColor.a);
				highlightMaterial.SetColor(k_MaterialColorBottomProperty, customTooltipHighlightColor.b);
			}

			m_Tooltips[tooltip] = new TooltipData
			{
				customHighlightMaterial = highlightMaterial,
				startTime = Time.time,
				lastModifiedTime = Time.time,
				persistent = persistent,
				duration = duration
			};
		}

		static bool IsValidTooltip(ITooltip tooltip)
		{
			return !string.IsNullOrEmpty(tooltip.tooltipText);
		}

		public void HideTooltip(ITooltip tooltip, bool persistent = false)
		{
			TooltipData tooltipData;
			if (m_Tooltips.TryGetValue(tooltip, out tooltipData))
			{
				if (!persistent && tooltipData.persistent)
					return;

				m_Tooltips.Remove(tooltip);

				if (tooltipData.tooltipUI)
					StartCoroutine(AnimateHide(tooltip, tooltipData.tooltipUI));
			}
		}

		IEnumerator AnimateHide(ITooltip tooltip, TooltipUI tooltipUI)
		{
			var target = GetTooltipTarget(tooltip);
			var startTime = Time.realtimeSinceStartup;
			while (Time.realtimeSinceStartup - startTime < k_TransitionDuration)
			{
				if (!target)
					break;

				UpdateVisuals(tooltip, tooltipUI, target,
					1 - (Time.realtimeSinceStartup - startTime) / k_TransitionDuration);
				yield return null;
			}

			ObjectUtils.Destroy(tooltipUI.gameObject);
		}
	}
}
#endif
