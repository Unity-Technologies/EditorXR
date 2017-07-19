using System.Collections;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	public class HintLine : MonoBehaviour
	{
		const string k_ShaderLineRadiusPropertyName = "_lineRadius";

		[SerializeField]
		bool m_HideOnInitialize = true;

		//[SerializeField]
		//Image m_Icon;

		[SerializeField]
		VRLineRenderer m_ScrollLineRenderer;

		[SerializeField]
		float m_LineWidth = 1f;

		[SerializeField]
		Color m_VisibleColor = Color.white;

		[SerializeField]
		Color m_HiddenColor = Color.clear;

		[SerializeField]
		Color m_PulseColor = Color.white;

		[SerializeField]
		MeshRenderer m_MeshRenderer;

		readonly Vector3 k_HiddenScale = Vector3.zero;

		Transform m_IconTransform;
		Vector3 m_VisibleLocalScale;
		Coroutine m_VisibilityCoroutine;
		Coroutine m_ScrollArrowPulseCoroutine;
		float m_PulseDuration;
		Material m_HintLineMaterial;

		public float LineWidth { set { m_ScrollLineRenderer.SetWidth(value, value); } }
		public Vector3[] Positions { set { m_ScrollLineRenderer.SetPositions(value) ; } }

		/*
		public bool visible
		{
			set
			{
				if (value)
					this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateShow());
				else
					this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateHide());
			}
		}

		public Color visibleColor
		{
			set
			{
				m_VisibleColor = value;
				this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateShow());
			}
		}
		*/

		void Awake()
		{
			/*
			m_IconTransform = m_Icon.transform;
			m_VisibleLocalScale = m_IconTransform.localScale * 1.25F;
			m_Icon.color = m_VisibleColor;

			if (m_HideOnInitialize)
				visible = false;
			*/

			m_ScrollLineRenderer.SetVertexCount(4);
			m_ScrollLineRenderer.useWorldSpace = true;
			m_ScrollLineRenderer.SetWidth(0f, 0f);
			m_HintLineMaterial = MaterialUtils.GetMaterialClone(m_MeshRenderer);
		}

		void OnDestroy()
		{
			ObjectUtils.Destroy(m_HintLineMaterial);
		}

		/*
		IEnumerator AnimateShow()
		{
			var currentDuration = 0f;
			var targetDuration = 0f;
			var currentLocalScale = m_IconTransform.localScale;
			if (currentLocalScale == k_HiddenScale)
			{
				// Only perform delay if fully hidden; otherwise resume showing
				targetDuration = Random.Range(0.25f, 0.35f); // Set an initial random wait duration
				while (currentDuration < targetDuration)
				{
					currentDuration += Time.unscaledDeltaTime;
					yield return null;
				}
			}

			currentDuration = 0f;
			targetDuration = 0.125f; // Set animated reveal duration
			var currentColor = m_Icon.color;
			while (currentDuration < targetDuration)
			{
				var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(currentDuration / targetDuration);
				m_IconTransform.localScale = Vector3.Lerp(currentLocalScale, m_VisibleLocalScale, shapedDuration);
				m_Icon.color = Color.Lerp(currentColor, m_VisibleColor, shapedDuration);
				currentDuration += Time.unscaledDeltaTime;
				yield return null;
			}

			m_IconTransform.localScale = m_VisibleLocalScale;
		}

		IEnumerator AnimateHide()
		{
			var currentDuration = 0f;
			var targetDuration = Random.Range(0.125f, 0.25f); // Set an initial random wait duration
			var currentLocalScale = m_IconTransform.localScale;
			var currentColor = m_Icon.color;
			while (currentDuration < targetDuration)
			{
				m_IconTransform.localScale = Vector3.Lerp(currentLocalScale, k_HiddenScale, currentDuration / targetDuration);
				m_Icon.color = Color.Lerp(currentColor, m_HiddenColor, currentDuration);
				currentDuration += Time.unscaledDeltaTime;
				yield return null;
			}

			m_IconTransform.localScale = k_HiddenScale;
		}
		*/

		public void PulseColor()
		{
			if (Mathf.Approximately(m_PulseDuration, 0f) || m_PulseDuration > 0.85f)
				this.RestartCoroutine(ref m_ScrollArrowPulseCoroutine, AnimatePulseColor());
		}

		IEnumerator AnimatePulseColor()
		{
			//Debug.LogError("Pulsing color of hint arrow : " + gameObject.name);
			const float kTargetDuration = 1f;
			m_PulseDuration = 0f;
			var maxShaderLineRadius = new Vector3 (0.03f, 0f, 100f);
			var minShaderLineRadius = new Vector3 (0.005f, 0f, 100f);
			var currentVector3ShaderLineRadius = m_HintLineMaterial.GetVector(k_ShaderLineRadiusPropertyName);
			var currentColor = m_ScrollLineRenderer.Colors[0]; // The line stand & end colors are the same; fetch only one of them
			var currentShaderLineRadius = m_HintLineMaterial.GetFloat(k_ShaderLineRadiusPropertyName);
			while (m_PulseDuration < kTargetDuration)
			{
				var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(m_PulseDuration / kTargetDuration);
				var newColor = Color.Lerp(currentColor, m_PulseColor, shapedDuration);
				m_ScrollLineRenderer.SetColors(newColor, newColor);
				m_PulseDuration += Time.unscaledDeltaTime * 5;
				m_HintLineMaterial.SetVector(k_ShaderLineRadiusPropertyName, Vector3.Lerp(currentVector3ShaderLineRadius, maxShaderLineRadius, shapedDuration));
				yield return null;
			}

			while (m_PulseDuration > 0f)
			{
				var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(m_PulseDuration / kTargetDuration);
				var newColor = Color.Lerp(m_VisibleColor, m_PulseColor, shapedDuration); 
				m_ScrollLineRenderer.SetColors(newColor, newColor);
				m_PulseDuration -= Time.unscaledDeltaTime * 1.5f;
				m_HintLineMaterial.SetVector(k_ShaderLineRadiusPropertyName, Vector3.Lerp(minShaderLineRadius, maxShaderLineRadius, shapedDuration));
				yield return null;
			}

			m_ScrollLineRenderer.SetColors(m_VisibleColor, m_VisibleColor);
			m_PulseDuration = 0f;
		}
	}
}
