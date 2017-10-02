#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using VisibilityControlType = UnityEditor.Experimental.EditorVR.Core.ProxyAffordanceMap.VisibilityControlType;
using AffordanceDefinition = UnityEditor.Experimental.EditorVR.Core.ProxyAffordanceMap.AffordanceDefinition;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
	public class ProxyUI : MonoBehaviour
	{
		private const string k_ZWritePropertyName = "_ZWrite";

		List<Renderer> m_BodyRenderers; // Renderers not associated with affordances/controls, & will be HIDDEN when displaying feedback/tooltips
		List<Renderer> m_AffordanceRenderers; // Renderers associated with affordances/controls, & will be SHOWN when displaying feedback/tooltips
		bool m_BodyRenderersVisible = true; // Body renderers default to visible/true
		bool m_AffordanceRenderersVisible = true; // Affordance renderers default to visible/true
		AffordanceObject[] m_Affordances;
		Coroutine m_BodyVisibilityCoroutine;

		/// <summary>
		/// Collection of proxy origins under which not to perform any affordance/body visibility changes
		/// </summary>
		List<Transform> m_ProxyOrigins;

		// Map of unique body materials to their original Colors (used for affordances with the "color" visibility control type)
		// The second param, ColorPair, houses the original cached color, and a value, representing the color to lerp FROM when animating visibility
		Dictionary<Material, affordancePropertyTuple<Color>> m_BodyMaterialOriginalColorMap = new Dictionary<Material, affordancePropertyTuple<Color>>();
		Dictionary<Material, affordancePropertyTuple<float>> m_BodyMaterialOriginalAlphaMap = new Dictionary<Material, affordancePropertyTuple<float>>();

		[SerializeField]
		ProxyAffordanceMap m_AffordanceMap;

		/// <summary>
		/// Model containing original value, and values to "animate from", unique to each body MeshRenderer material.
		/// Visibility of all objects in the proxy body are driven by a single AffordanceVisibilityDefinition,
		/// as opposed to individual interactable affordances, which each have their own AffordanceVisibilityDefinition, which contains their unique value data.
		/// This is a lightweight class to store that data, alleviating the need to duplicate an affordance definition for each body renderer as well.
		/// </summary>
		class affordancePropertyTuple<T>
		{
			public T originalValue { get; private set; }
			public T animateFromValue { get; set; }

			public affordancePropertyTuple(T originalValue, T animateFromValue)
			{
				this.originalValue = originalValue;
				this.animateFromValue = animateFromValue;
			}
		}

		/// <summary>
		/// Set ProxyHelper affordances in this ProxyUI
		/// </summary>
		AffordanceObject[] affordances
		{
			set
			{
				if (m_Affordances != null)
					return;

				if (m_AffordanceMap == null)
				{
					Debug.LogError("An Affordance Map must be assigned to ProxyUI on : " + gameObject.name);
					return;
				}

				// Clone the affordance map, in order to allow a single map to drive the visuals of many duplicate
				// This also allows coroutine sets in the ProxyAffordanceMap to be performed simultaneously for n-number of devices in a proxy
				m_AffordanceMap = Instantiate(m_AffordanceMap);

				m_Affordances = value;
				m_AffordanceRenderers = new List<Renderer>();
				foreach (var affordanceDefinition in m_AffordanceMap.AffordanceDefinitions)
				{
					var control = affordanceDefinition.control;
					var affordance = m_Affordances.FirstOrDefault(x => x.control == control);
					if (affordance != null)
					{
						var renderer = affordance.renderer;
						if (renderer != null)
						{
							var materialClone = MaterialUtils.GetMaterialClone(renderer); // TODO: support multiple materials
							if (materialClone != null)
							{
								var visualDefinition = affordanceDefinition.visibilityDefinition;
								var originalColor = materialClone.color;
								m_AffordanceRenderers.Add(renderer); // Add to collection for later optimized comparison against body renderers
								visualDefinition.renderer = renderer;
								visualDefinition.originalColor = originalColor;
								visualDefinition.material = materialClone;

								// Clone that utilize the standard can be cloned and lose their ZWrite value (1), if it was enabled on the material
								// Set it again, to avoid ZWrite + transparency visual issues
								if (materialClone.HasProperty(k_ZWritePropertyName))
									materialClone.SetFloat(k_ZWritePropertyName, 1);
							}
						}
					}
				}

				// Collect renderers not associated with affordances
				// Material swaps don't need to cache original values, only alpha & color
				var bodyVisibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
				m_BodyRenderers = GetComponentsInChildren<Renderer>(true).Where(x => !m_AffordanceRenderers.Contains(x) && !IsChildOfProxyOrigin(x.transform)).ToList();
				switch (m_AffordanceMap.bodyVisibilityDefinition.visibilityType)
				{
					case VisibilityControlType.colorProperty:
						foreach (var renderer in m_BodyRenderers)
						{
							// TODO: support for skipping the cloning of materials in the body that are shared between objects, in order to reduce draw calls
							var materialClone = MaterialUtils.GetMaterialClone(renderer); // TODO: support multiple materials per-renderer
							if (materialClone != null)
							{
								var originalColor = materialClone.color;
								if (materialClone.HasProperty(k_ZWritePropertyName))
									materialClone.SetFloat(k_ZWritePropertyName, 1);

								m_BodyMaterialOriginalColorMap[materialClone] = new affordancePropertyTuple<Color>(originalColor, originalColor);
							}
						}
						break;
					case VisibilityControlType.alphaProperty:
						string shaderAlphaPropety = bodyVisibilityDefinition.alphaProperty;
						foreach (var renderer in m_BodyRenderers)
						{
							var materialClone = MaterialUtils.GetMaterialClone(renderer); // TODO: support multiple materials per-renderer
							if (materialClone != null)
							{
								var originalAlpha = materialClone.GetFloat(shaderAlphaPropety);
								if (materialClone.HasProperty(k_ZWritePropertyName))
									materialClone.SetFloat(k_ZWritePropertyName, 1);

								m_BodyMaterialOriginalAlphaMap[materialClone] = new affordancePropertyTuple<float>(originalAlpha, originalAlpha);
							}
						}
						break;
				}
			}
		}

		/// <summary>
		/// Set the visibility of the affordance renderers that are associated with controls/input
		/// </summary>
		public bool affordancesVisible
		{
			set
			{
				if (m_ProxyOrigins == null || !gameObject.activeInHierarchy || m_AffordanceRenderersVisible == value)
					return;

				m_AffordanceRenderersVisible = value;
				foreach (var affordanceDefinition in m_AffordanceMap.AffordanceDefinitions)
				{
					var visibilityDefinition = affordanceDefinition.visibilityDefinition;
					switch (visibilityDefinition.visibilityType)
					{
						case VisibilityControlType.colorProperty:
							this.RestartCoroutine(ref visibilityDefinition.affordanceVisibilityCoroutine, AnimateAffordanceColorVisibility(value, affordanceDefinition));
							break;
						case VisibilityControlType.alphaProperty:
							this.RestartCoroutine(ref visibilityDefinition.affordanceVisibilityCoroutine, AnimateAffordanceAlphaVisibility(value, affordanceDefinition));
							break;
						case VisibilityControlType.materialSwap:
							SwapAffordanceToHiddenMaterial(value, affordanceDefinition);
							break;
					}
				}
			}
		}

		/// <summary>
		/// Set the visibility of the renderers not associated with controls/input
		/// </summary>
		public bool bodyVisible
		{
			set
			{
				if (m_ProxyOrigins == null || !gameObject.activeInHierarchy || m_BodyRenderersVisible == value)
					return;

				m_BodyRenderersVisible = value;
				var visibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
				switch (visibilityDefinition.visibilityType)
				{
					case VisibilityControlType.colorProperty:
						this.RestartCoroutine(ref m_BodyVisibilityCoroutine, AnimateBodyColorVisibility(value));
						break;
					case VisibilityControlType.alphaProperty:
						this.RestartCoroutine(ref m_BodyVisibilityCoroutine, AnimateBodyAlphaVisibility(value));
						break;
					case VisibilityControlType.materialSwap:
						SwapBodyToHiddenMaterial(value);
						break;
				}
			}
		}

		/// <summary>
		/// Setup this ProxyUI
		/// </summary>
		/// <param name="affordances">The ProxyHelper affordances that drive visual changes in the ProxyUI</param>
		/// <param name="proxyOrigins">ProxyOrigins whose child renderers will not be controlled by the PRoxyUI</param>
		public void Setup(AffordanceObject[] affordances, List<Transform> proxyOrigins)
		{
			// Set affordances AFTER setting origins, as the origins are referenced when setting up affordances
			m_ProxyOrigins = proxyOrigins;
			this.affordances = affordances;
			affordancesVisible = false;
			bodyVisible = false;
		}

		IEnumerator AnimateAffordanceColorVisibility(bool isVisible, AffordanceDefinition definition)
		{
			// Set original cached color when visible, transparent when hidden
			const float kSpeedScalar = 2f;
			const float kTargetAmount = 1f;
			const float kHiddenValue = 0.25f;
			var currentAmount = 0f;
			var visibilityDefinition = definition.visibilityDefinition;
			var material = visibilityDefinition.material;
			var shaderColorPropety = visibilityDefinition.colorProperty;
			var animateFromColor = material.GetColor(shaderColorPropety);
			var animateToColor = isVisible ? visibilityDefinition.originalColor : new Color(animateFromColor.r, animateFromColor.g, animateFromColor.b, kHiddenValue);
			while (currentAmount < kTargetAmount)
			{
				var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentAmount += Time.unscaledDeltaTime * kSpeedScalar);
				var currentColor = Color.Lerp(animateFromColor, animateToColor, smoothedAmount);
				material.SetColor(shaderColorPropety, currentColor);

				yield return null;
			}
		}

		IEnumerator AnimateAffordanceAlphaVisibility(bool isVisible, AffordanceDefinition definition)
		{
			const float kSpeedScalar = 2f;
			const float kTargetAmount = 1f;
			const float kHiddenValue = 0.25f;
			var visibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
			var material = visibilityDefinition.material;
			string shaderAlphaPropety = visibilityDefinition.alphaProperty;
			var animateFromAlpha = material.GetFloat(shaderAlphaPropety);
			var animateToAlpha = isVisible ? visibilityDefinition.originalAlpha : kHiddenValue;
			var currentAmount = 0f;
			while (currentAmount < kTargetAmount)
			{
				var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentAmount += Time.unscaledDeltaTime * kSpeedScalar);
				var currentAlpha = Mathf.Lerp(animateFromAlpha, animateToAlpha, smoothedAmount);
				material.SetFloat(shaderAlphaPropety, currentAlpha);

				yield return null;
			}

			// Mandate target value has been set
			material.SetFloat(shaderAlphaPropety, animateToAlpha);
		}

		IEnumerator AnimateBodyColorVisibility(bool isVisible)
		{
			var bodyVisibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
			foreach (var kvp in m_BodyMaterialOriginalColorMap)
			{
				kvp.Value.animateFromValue = kvp.Key.GetColor(bodyVisibilityDefinition.colorProperty);
			}

			const float kSpeedScalar = 2f;
			const float kTargetAmount = 1f;
			const float kHiddenValue = 0.25f;
			var currentAmount = 0f;
			while (currentAmount < kTargetAmount)
			{
				var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentAmount += Time.unscaledDeltaTime * kSpeedScalar);
				foreach (var kvp in m_BodyMaterialOriginalColorMap)
				{
					// Set original cached color when visible, transparent when hidden
					var valueFrom = kvp.Value.animateFromValue;
					var valueTo = isVisible ? kvp.Value.originalValue : new Color(valueFrom.r, valueFrom.g, valueFrom.b, kHiddenValue);
					var currentColor = Color.Lerp(valueFrom, valueTo, smoothedAmount);
					kvp.Key.color = currentColor;
				}

				yield return null;
			}

			// Mandate target values have been set
			foreach (var kvp in m_BodyMaterialOriginalColorMap)
			{
				var originalColor = kvp.Value.originalValue;
				kvp.Key.color = isVisible ? originalColor : new Color(originalColor.r, originalColor.g, originalColor.b, kHiddenValue);
			}
		}

		IEnumerator AnimateBodyAlphaVisibility(bool isVisible)
		{
			var bodyVisibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
			foreach (var kvp in m_BodyMaterialOriginalAlphaMap)
			{
				kvp.Value.animateFromValue = kvp.Key.GetFloat(bodyVisibilityDefinition.alphaProperty);
			}

			const float kSpeedScalar = 2f;
			const float kTargetAmount = 1f;
			const float kHiddenValue = 0.25f;
			string shaderAlphaPropety = bodyVisibilityDefinition.alphaProperty;
			var currentAmount = 0f;
			while (currentAmount < kTargetAmount)
			{
				var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentAmount += Time.unscaledDeltaTime * kSpeedScalar);
				foreach (var kvp in m_BodyMaterialOriginalAlphaMap)
				{
					var valueFrom = kvp.Value.animateFromValue;
					var valueTo = isVisible ? kvp.Value.originalValue : kHiddenValue;
					var currentAlpha = Mathf.Lerp(valueFrom, valueTo, smoothedAmount);
					kvp.Key.SetFloat(shaderAlphaPropety, currentAlpha);
				}

				yield return null;
			}

			// Mandate target values have been set
			foreach (var kvp in m_BodyMaterialOriginalAlphaMap)
			{
				kvp.Key.SetFloat(shaderAlphaPropety, isVisible ? kvp.Value.originalValue : kHiddenValue);
			}
		}

		void SwapAffordanceToHiddenMaterial(bool swapToHiddenMaterial, AffordanceDefinition definition)
		{
			var visibilityDefinition = definition.visibilityDefinition;
			var swapMaterial = swapToHiddenMaterial ? visibilityDefinition.hiddenMaterial : visibilityDefinition.material;
			var renderer = visibilityDefinition.renderer;
			renderer.material = swapMaterial;
		}

		void SwapBodyToHiddenMaterial(bool swapToHiddenMaterial)
		{
			var bodyVisibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
			var swapMaterial = swapToHiddenMaterial ? bodyVisibilityDefinition.hiddenMaterial : bodyVisibilityDefinition.material;
			foreach (var renderer in m_BodyRenderers)
			{
				renderer.material = swapMaterial;
			}
		}

		bool IsChildOfProxyOrigin(Transform transform)
		{
			// Prevent any menu/origin content from having its visibility changed
			var isChild = false;
			foreach (var origin in m_ProxyOrigins)
			{
				// m_ProxyOrgins is populated by ProxyHelper
				// Validate that the transform param is not a child of any proxy origins
				// Those objects shouldn't have their visibility changed by the ProxyUI
				// Their individual controllers should handle their visibility
				if (transform.IsChildOf(origin))
				{
					isChild = true;
					break;
				}
			}

			return isChild;
		}
	}
}
#endif
