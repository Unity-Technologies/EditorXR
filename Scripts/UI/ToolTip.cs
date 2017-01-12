using System.Collections;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.UI;

namespace UnityEngine.Experimental.EditorVR.UI
{
	public class ToolTip : MonoBehaviour
	{
		const float kTransitionDuration = 0.75f;

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
			var tipText = U.Object.Instantiate(m_TextPrefab); // No need for InstantiateUI because no interaction
			tipText.gameObject.SetActive(false);
			tipText.transform.parent = transform;
			tipText.transform.localPosition = m_Offset;

			m_Text = tipText.GetComponentInChildren<Text>();
			m_Text.text = m_ToolTipText;

			m_CanvasGroup = tipText.GetComponentInChildren<CanvasGroup>();
		}

		public void Show()
		{
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
					var duration = Time.realtimeSinceStartup - m_ShowStartTime;
					if (duration < kTransitionDuration)
					{
						m_CanvasGroup.alpha = duration / kTransitionDuration;
					}

					m_CanvasGroup.transform.LookAt(U.Camera.GetMainCamera().transform, Vector3.up);
				}

				if (m_HideStartTime > 0)
				{
					var duration = Time.realtimeSinceStartup - m_HideStartTime;
					if (duration < kTransitionDuration)
					{
						m_CanvasGroup.alpha = duration / kTransitionDuration;
					}
					else
					{
						m_CanvasGroup.alpha = 0;
						m_ShowStartTime = -1;
						m_HideStartTime = -1;
						m_CanvasGroup.gameObject.SetActive(false);
						yield break;
					}
				}

				yield return null;
			}
		}
	}
}
