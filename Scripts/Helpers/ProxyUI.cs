#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
	public class ProxyUI : MonoBehaviour
	{
		List<Renderer> m_BodyRenderers; // Renderers not associated with controls, & will be hidden when displaying feedback/tooltips
		bool m_BodyRenderersVisible = true; // Renderers default to visible/true
		AffordanceObject[] m_Affordances;
		List<Material> m_BodyMaterials; // Collection of unique body MeshRenderer materials
		Coroutine m_BodyVisibilityCoroutine;

		private class ColorPair
		{
			/// <summary>
			/// The original/cached color of the material
			/// </summary>
			public Color originalColor { get; set; }

			/// <summary>
			/// The color to lerp FROM as the current color when animating visibility
			/// </summary>
			public Color animateFromColor { get; set; }

			public ColorPair(Color originalColor, Color animateFromColor)
			{
				this.originalColor = originalColor;
				this.animateFromColor = animateFromColor;
			}
		}

		// Map of unique body materials to their original Colors (used for affordances with the "color" visibility control type)
		// The second/nested dictionary of color has a key, representing the original cached color, and a value, representing the color to lerp FROM when animating visibility
		Dictionary<Material, ColorPair> m_BodyMaterialOriginalColorMap = new Dictionary<Material, ColorPair>();
		Dictionary<VRInputDevice.VRControl, Dictionary<Material, Color>> m_AffordanceMaterialOriginalColorMap = new Dictionary<VRInputDevice.VRControl, Dictionary<Material, Color>>();

		[SerializeField]
		ProxyAffordanceMap m_AffordanceMap;

		//public Dictionary<VRInputDevice.VRControl, AffordanceObject> controlToAffordanceMap { private get; set; }

		//[SerializeField]
		//ProxyHelper m_ProxyHelper;

			//[SerializeField]
			//ProxyHelper.VisibilityControlType m_BodyVisibilityControlType;

			//public ProxyHelper.ButtonObject[] buttons { get; set; }

		public AffordanceObject[] Affordances
		{
			set
			{
				//build body material original color map for each control with color chosen as visibilty type in the affordance map that has a matching control

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
					var affordance = m_Affordances.Where(x => x.control == control).FirstOrDefault();
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
								affordanceRenderers.Add(renderer); // Add to collection for later comparison again body renderers
								materialToOriginalColorMap[materialClone] = originalColor;
								m_AffordanceMaterialOriginalColorMap[control] = materialToOriginalColorMap;
							}
						}
					}
				}
				// Collect renderers not associated with affordances
				m_BodyRenderers = GetComponentsInChildren<Renderer>(true).Where(x => !affordanceRenderers.Contains(x)).ToList();
				foreach (var renderer in m_BodyRenderers)
				{
					// TODO: support for skipping the cloning of materials in the body that are shared between objects, to reduce draw calls
					var materialClone = MaterialUtils.GetMaterialClone(renderer); // TODO: support multiple materials per-renderer
					if (materialClone != null)
					{
						var originalColor = materialClone.color;
						m_BodyMaterialOriginalColorMap[materialClone] = new ColorPair(originalColor, originalColor);
					}
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
				this.RestartCoroutine(ref m_BodyVisibilityCoroutine, AnimateBodyVisibility(value));
			}
		}

		IEnumerator AnimateBodyVisibility(bool isVisible)
		{
			foreach (var kvp in m_BodyMaterialOriginalColorMap)
			{
				kvp.Value.animateFromColor = kvp.Key.GetColor("_Color");
			}

			const float kSpeedScalar = 2f;
			const float kTargetAmount = 1f;
			var currentAmount = 0f;
			while (currentAmount < kTargetAmount)
			{
				currentAmount += Time.unscaledDeltaTime * kSpeedScalar;
				var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentAmount);
				foreach (var kvp in m_BodyMaterialOriginalColorMap)
				{
					// Set original cached color when visible, transparent when hidden
					var colorFrom = kvp.Value.animateFromColor;
					var colorTo = isVisible ? kvp.Value.originalColor : new Color(colorFrom.r, colorFrom.g, colorFrom.b, 0.25f);
					var currentColor = Color.Lerp(colorFrom, colorTo, smoothedAmount);
					kvp.Key.color = currentColor;
				}

				yield return null;
			}
		}
	}
}
#endif
