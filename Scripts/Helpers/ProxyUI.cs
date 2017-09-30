#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using VisibilityControlType = UnityEditor.Experimental.EditorVR.Core.ProxyAffordanceMap.VisibilityControlType;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
	public class ProxyUI : MonoBehaviour
	{
		List<Renderer> m_BodyRenderers; // Renderers not associated with controls, & will be hidden when displaying feedback/tooltips
		bool m_BodyRenderersVisible = true; // Renderers default to visible/true
		AffordanceObject[] m_Affordances;
		//List<Material> m_BodyMaterials; // Collection of unique body MeshRenderer materials // TODO : delete, no longer used?
		Coroutine m_BodyVisibilityCoroutine;

		// Map of unique body materials to their original Colors (used for affordances with the "color" visibility control type)
		// The second param, ColorPair, houses the original cached color, and a value, representing the color to lerp FROM when animating visibility
		Dictionary<Material, affordancePropertyTuple<Color>> m_BodyMaterialOriginalColorMap = new Dictionary<Material, affordancePropertyTuple<Color>>();
		Dictionary<Material, affordancePropertyTuple<float>> m_BodyMaterialOriginalAlphaMap = new Dictionary<Material, affordancePropertyTuple<float>>();

		// Used to draw visual attention to individual affordances
		Dictionary<VRInputDevice.VRControl, Dictionary<Material, Color>> m_AffordanceMaterialOriginalColorMap = new Dictionary<VRInputDevice.VRControl, Dictionary<Material, Color>>();

		[SerializeField]
		ProxyAffordanceMap m_AffordanceMap;

		/// <summary>
		/// Model containing original value, and values to "animate from", unique to each body MeshRenderer material.
		/// Visibility of all objects in the proxy body are driven by a single AffordanceVisibilityDefinition,
		/// as opposed to individual interactable affordances, which each have their own AffordanceVisibilityDefinition, which contains their unique value data.
		/// This is a lightweight class to store that data, alleviating the need to duplicate an affordance definition for each body renderer as well.
		/// </summary>
		private class affordancePropertyTuple<T>
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
		public AffordanceObject[] Affordances
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

				m_Affordances = value;
				var affordanceRenderers = new List<Renderer>();
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
								var originalColor = materialClone.color;
								var materialToOriginalColorMap = new Dictionary<Material, Color>();
								affordanceRenderers.Add(renderer); // Add to collection for later comparison against body renderers
								materialToOriginalColorMap[materialClone] = originalColor;
								m_AffordanceMaterialOriginalColorMap[control] = materialToOriginalColorMap;
							}
						}
					}
				}

				// Collect renderers not associated with affordances
				// Material swaps don't need to cache original values, only alpha & color
				var bodyVisibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
				m_BodyRenderers = GetComponentsInChildren<Renderer>(true).Where(x => !affordanceRenderers.Contains(x)).ToList();
				Debug.LogWarning("Don't collect renderers from the menu origins!!!");
				switch (m_AffordanceMap.bodyVisibilityDefinition.visibilityType)
				{
					case VisibilityControlType.colorProperty:
						foreach (var renderer in m_BodyRenderers)
						{
							// TODO: support for skipping the cloning of materials in the body that are shared between objects, to reduce draw calls
							var materialClone = MaterialUtils.GetMaterialClone(renderer); // TODO: support multiple materials per-renderer
							if (materialClone != null)
							{
								var originalColor = materialClone.color;
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
								m_BodyMaterialOriginalAlphaMap[materialClone] = new affordancePropertyTuple<float>(originalAlpha, originalAlpha);
							}
						}
						break;
				}
			}
		}

		/// <summary>
		/// Set the visibility of the renderers not associated with controls/input
		/// </summary>
		public bool bodyRenderersVisible
		{
			set
			{
				if (!gameObject.activeInHierarchy || m_BodyRenderersVisible == value)
					return;

				m_BodyRenderersVisible = value;
				var bodyVisibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
				switch (bodyVisibilityDefinition.visibilityType)
				{
					case VisibilityControlType.colorProperty:
						this.RestartCoroutine(ref m_BodyVisibilityCoroutine, AnimateColorBodyVisibility(value));
						break;
					case VisibilityControlType.alphaProperty:
						this.RestartCoroutine(ref m_BodyVisibilityCoroutine, AnimateAlphaBodyVisibility(value));
						break;
					case VisibilityControlType.materialSwap:
						SwapToHiddenMaterial(value);
						break;
				}
			}
		}

		IEnumerator AnimateColorBodyVisibility(bool isVisible)
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
				currentAmount += Time.unscaledDeltaTime * kSpeedScalar;
				var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentAmount);
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
		}

		IEnumerator AnimateAlphaBodyVisibility(bool isVisible)
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
				currentAmount += Time.unscaledDeltaTime * kSpeedScalar;
				var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentAmount);
				foreach (var kvp in m_BodyMaterialOriginalAlphaMap)
				{
					var valueFrom = kvp.Value.animateFromValue;
					var valueTo = isVisible ? kvp.Value.originalValue : kHiddenValue;
					var currentAlpha = Mathf.Lerp(valueFrom, valueTo, smoothedAmount);
					kvp.Key.SetFloat(shaderAlphaPropety, currentAlpha);
				}

				yield return null;
			}
		}

		void SwapToHiddenMaterial(bool swapToHiddenMaterial)
		{
			var bodyVisibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
			var swapMaterial = swapToHiddenMaterial ? bodyVisibilityDefinition.hiddenMaterial : bodyVisibilityDefinition.originalMaterial;
			foreach (var renderer in m_BodyRenderers)
			{
				renderer.material = swapMaterial;
			}
		}
	}
}
#endif
