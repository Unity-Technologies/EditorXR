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

		readonly Vector3 k_HiddenScale = Vector3.zero;

		Transform m_IconTransform;
		Vector3 m_VisibleLocalScale;
		Coroutine m_VisibilityCoroutine;
		Coroutine m_ScrollArrowPulseCoroutine;

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

		public void PulseColor()
		{
			this.RestartCoroutine(ref m_ScrollArrowPulseCoroutine, AnimatePulseColor());
		}

		IEnumerator AnimatePulseColor()
		{
			//Debug.LogError("Pulsing color of hint arrow : " + gameObject.name);
			const float kTargetDuration = 1f;
			var currentDuration = 0f;
			var currentColor = m_Icon.color;
			while (currentDuration < kTargetDuration)
			{
				var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(currentDuration / kTargetDuration);
				m_Icon.color = Color.Lerp(currentColor, m_PulseColor, shapedDuration);
				currentDuration += Time.unscaledDeltaTime * 5;
				yield return null;
			}

			while (currentDuration > 0f)
			{
				var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(currentDuration / kTargetDuration);
				m_Icon.color = Color.Lerp(m_VisibleColor, m_PulseColor, shapedDuration);
				currentDuration -= Time.unscaledDeltaTime * 2;
				yield return null;
			}

			m_Icon.color = m_VisibleColor;
		}
	}
}
