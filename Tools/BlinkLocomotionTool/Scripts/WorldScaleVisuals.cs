using UnityEngine;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.UI;

public class WorldScaleVisuals : MonoBehaviour, IUsesCameraRig
{
	[SerializeField]
	float m_IconTranslateCoefficient = -0.16f;

	[SerializeField]
	float m_IconTranslateOffset = 0.08f;

	[SerializeField]
	float m_DotUVScale = 50f;

	[SerializeField]
	RawImage m_DottedLine;

	[SerializeField]
	Transform m_IconsContainer;

	[SerializeField]
	Text m_ScaleText;

	[SerializeField]
	Sprite[] m_Icons;

	[SerializeField]
	GameObject m_IconPrefab;

	public Transform leftHand { private get; set; }
	public Transform rightHand { private get; set; }
	public Transform cameraRig { private get; set; }

	void Start()
	{
		for (var i = 0; i < m_Icons.Length; i++)
		{
			var icon = m_Icons[i];
			var image = ((GameObject)Instantiate(m_IconPrefab, m_IconsContainer, false)).GetComponent<Image>();
			image.sprite = icon;
		}
	}

	void Update()
	{
		SetPosition();
	}

	public void SetPosition()
	{
		var viewerScale = cameraRig.localScale.x;
		var iconContainerLocal = m_IconsContainer.localPosition;
		iconContainerLocal.x = Mathf.Log10(viewerScale) * m_IconTranslateCoefficient + m_IconTranslateOffset;
		m_IconsContainer.localPosition = iconContainerLocal;

		var camera = U.Camera.GetMainCamera().transform;
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
		var length = leftToRight.magnitude;
		m_DottedLine.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, length);
		var uvRect = m_DottedLine.uvRect;
		uvRect.width = length * m_DotUVScale;
		m_DottedLine.uvRect = uvRect;

		m_ScaleText.text = string.Format("Viewer Scale: {0:f2}", viewerScale);
	}
}
