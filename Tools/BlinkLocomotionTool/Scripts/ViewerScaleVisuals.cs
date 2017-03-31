#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	sealed class ViewerScaleVisuals : MonoBehaviour
	{
		[SerializeField]
		float m_IconTranslateCoefficient = -0.16f;

		[SerializeField]
		float m_IconTranslateOffset = 0.08f;

		[SerializeField]
		VRLineRenderer m_Line;

		[SerializeField]
		Transform m_IconsContainer;

		[SerializeField]
		Text m_ScaleText;

		[SerializeField]
		Sprite[] m_Icons;

		[SerializeField]
		GameObject m_IconPrefab;

		float m_LineWidth;

		public Transform leftHand { private get; set; }
		public Transform rightHand { private get; set; }
		public float viewerScale { private get; set; }

		void Start()
		{
			foreach (var icon in m_Icons)
			{
				var image = ((GameObject)Instantiate(m_IconPrefab, m_IconsContainer, false)).GetComponent<Image>();
				image.sprite = icon;
			}

			m_LineWidth = m_Line.widthStart;
			SetPosition();
		}

		void Update()
		{
			SetPosition();
		}

		void SetPosition()
		{
			var iconContainerLocal = m_IconsContainer.localPosition;
			iconContainerLocal.x = Mathf.Log10(viewerScale) * m_IconTranslateCoefficient + m_IconTranslateOffset;
			m_IconsContainer.localPosition = iconContainerLocal;

			var camera = CameraUtils.GetMainCamera().transform;
			var leftToRight = leftHand.position - rightHand.position;

			// If hands reverse, switch hands
			if (Vector3.Dot(leftToRight, camera.right) > 0)
			{
				leftToRight *= -1;
				var tmp = leftHand;
				leftHand = rightHand;
				rightHand = tmp;
			}

			transform.position = rightHand.position + leftToRight * 0.5f;
			transform.rotation = Quaternion.LookRotation(leftToRight, camera.position - transform.position);

			leftToRight = transform.InverseTransformVector(leftToRight);
			var length = leftToRight.magnitude * 0.5f;
			m_Line.SetPosition(0, Vector3.left * length);
			m_Line.SetPosition(1, Vector3.right * length);
			var lineWidth = m_LineWidth * viewerScale;
			m_Line.SetWidth(lineWidth, lineWidth);

			m_ScaleText.text = string.Format("Viewer Scale: {0:f2}", viewerScale);
		}
	}
}
#endif
