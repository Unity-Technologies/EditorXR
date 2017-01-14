using System.Collections;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.UI;

namespace UnityEngine.Experimental.EditorVR.UI
{
	public class ToolTip : MonoBehaviour
	{
		const float kTransitionDuration = 0.3f;

		[SerializeField]
		GameObject m_TextPrefab;

		[SerializeField]
		string m_ToolTipText;

		[SerializeField]
		Vector3 m_Offset;

		Text m_Text;
		CanvasGroup m_CanvasGroup;

		float m_ShowStartTime = -1;
		float m_HideStartTime = -1;

		public string text
		{
			set { m_Text.text = value; }
		}

		void Start()
		{
			var tipText = U.Object.Instantiate(m_TextPrefab, transform); // No need for InstantiateUI because no interaction
			tipText.gameObject.SetActive(false);
			tipText.transform.localPosition = m_Offset;

			m_Text = tipText.GetComponentInChildren<Text>();
			m_Text.text = m_ToolTipText;

			m_CanvasGroup = tipText.GetComponentInChildren<CanvasGroup>();
		}

		public void Show()
		{
			StopAllCoroutines();
			Reset();
			m_ShowStartTime = Time.realtimeSinceStartup;
			m_CanvasGroup.gameObject.SetActive(true);
			StartCoroutine(UpdateText());
		}

		public void Hide()
		{
			m_HideStartTime = Time.realtimeSinceStartup;
		}

		IEnumerator UpdateText()
		{
			while (true)
			{
				if (m_ShowStartTime > 0)
				{
					var startAlpha = m_CanvasGroup.alpha;
					var duration = Time.realtimeSinceStartup - m_ShowStartTime;
					if (duration < kTransitionDuration)
					{
						m_CanvasGroup.alpha = Mathf.Max(startAlpha, duration / kTransitionDuration);
					}

					var canvasGroupTransform = m_CanvasGroup.transform;
					canvasGroupTransform.rotation = Quaternion.LookRotation(canvasGroupTransform.position - U.Camera.GetMainCamera().transform.position, Vector3.up);
				}

				if (m_HideStartTime > 0)
				{
					var startAlpha = m_CanvasGroup.alpha;
					var duration = Time.realtimeSinceStartup - m_HideStartTime;
					if (duration < kTransitionDuration)
					{
						m_CanvasGroup.alpha = Mathf.Min(startAlpha, 1 - duration / kTransitionDuration);
					}
					else
					{
						Reset();
						yield break;
					}
				}

				yield return null;
			}
		}

		void Reset()
		{
			m_CanvasGroup.alpha = 0;
			m_ShowStartTime = -1;
			m_HideStartTime = -1;
			m_CanvasGroup.gameObject.SetActive(false);
		}
	}
}
