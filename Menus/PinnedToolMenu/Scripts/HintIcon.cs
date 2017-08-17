#if UNITY_EDITOR
using System.Collections;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	public class HintIcon : MonoBehaviour
	{
		[SerializeField]
		bool m_HideOnInitialize = true;

		[SerializeField]
		Image m_Icon;

		[SerializeField]
		Color m_VisibleColor = Color.white;

		[SerializeField]
		Color m_HiddenColor = Color.clear;

		[SerializeField]
		Color m_PulseColor = Color.white;

		[SerializeField]
		float m_ShowDuration = 0.125f;

		[SerializeField]
		float m_HideDuration = 0.25f;

		[SerializeField]
		bool m_SlightlyRandomizeHideDuration = true;

		readonly Vector3 k_HiddenScale = Vector3.zero;

		Transform m_IconTransform;
		Vector3 m_VisibleLocalScale;
		Coroutine m_VisibilityCoroutine;
		Coroutine m_ScrollArrowPulseCoroutine;
		float m_PulseDuration;

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

		void Awake()
		{
			m_IconTransform = m_Icon.transform;
			m_VisibleLocalScale = m_IconTransform.localScale * 1.25F;
			m_Icon.color = m_VisibleColor;

			if (m_HideOnInitialize)
				visible = false;
		}

		IEnumerator AnimateShow()
		{
			var currentDuration = 0f;
			var targetDuration = 0f;
			var currentLocalScale = m_IconTransform.localScale;
			if (currentLocalScale == k_HiddenScale)
			{
				// Only perform delay if fully hidden; otherwise resume showing
				targetDuration = Random.Range(0.125f, 0.175f); // Set an initial random wait duration
				while (currentDuration < targetDuration)
				{
					currentDuration += Time.unscaledDeltaTime;
					yield return null;
				}
			}

			currentDuration = 0f;
			targetDuration = m_ShowDuration; // Set animated reveal duration
			var currentColor = m_Icon.color;
			while (currentDuration < targetDuration)
			{
				var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(currentDuration / targetDuration);
				shapedDuration = Mathf.Pow(shapedDuration, 4);
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
			var targetDuration = m_HideDuration + (m_SlightlyRandomizeHideDuration ? 0f : Random.Range(0.125f, 0.2f)); // Set an initial random wait duration
			var currentLocalScale = m_IconTransform.localScale;
			var currentColor = m_Icon.color;
			while (currentDuration < targetDuration)
			{
				var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(currentDuration / targetDuration);
				shapedDuration = Mathf.Pow(shapedDuration, 4);
				m_IconTransform.localScale = Vector3.Lerp(currentLocalScale, k_HiddenScale, shapedDuration);
				m_Icon.color = Color.Lerp(currentColor, m_HiddenColor, currentDuration * 3);
				currentDuration += Time.unscaledDeltaTime;
				yield return null;
			}

			m_IconTransform.localScale = k_HiddenScale;
		}

		public void PulseColor()
		{
			if (Mathf.Approximately(m_PulseDuration, 0f) || m_PulseDuration > 0.85f)
				this.RestartCoroutine(ref m_ScrollArrowPulseCoroutine, AnimatePulseColor());
		}

		IEnumerator AnimatePulseColor()
		{
			const float kTargetDuration = 1f;
			m_PulseDuration = 0f;
			var currentColor = m_Icon.color;
			while (m_PulseDuration < kTargetDuration)
			{
				var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(m_PulseDuration / kTargetDuration);
				m_Icon.color = Color.Lerp(currentColor, m_PulseColor, shapedDuration);
				m_PulseDuration += Time.unscaledDeltaTime * 5;
				yield return null;
			}

			while (m_PulseDuration > 0f)
			{
				var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(m_PulseDuration / kTargetDuration);
				m_Icon.color = Color.Lerp(m_VisibleColor, m_PulseColor, shapedDuration);
				m_PulseDuration -= Time.unscaledDeltaTime * 2;
				yield return null;
			}

			m_Icon.color = m_VisibleColor;
			m_PulseDuration = 0f;
		}
	}
}
#endif
