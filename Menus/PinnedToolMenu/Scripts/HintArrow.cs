using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class HintArrow : MonoBehaviour
{
	[SerializeField]
	Image m_Icon;

	readonly Vector3 k_HiddenScale = Vector3.zero;

	Transform m_IconTransform;
	Vector3 m_VisibleLocalScale;
	Coroutine m_VisibilityCoroutine;

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

	void Awake()
	{
		m_IconTransform = m_Icon.transform;
		m_VisibleLocalScale = m_IconTransform.localScale * 1.25F;
		m_Icon.color = Color.black;
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
		while (currentDuration < targetDuration)
		{
			m_IconTransform.localScale = Vector3.Lerp(currentLocalScale, m_VisibleLocalScale, currentDuration / targetDuration);
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
		while (currentDuration < targetDuration)
		{
			m_IconTransform.localScale = Vector3.Lerp(currentLocalScale, k_HiddenScale, currentDuration / targetDuration);
			currentDuration += Time.unscaledDeltaTime;
			yield return null;
		}

		m_IconTransform.localScale = k_HiddenScale;
	}
}
