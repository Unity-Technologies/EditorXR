using UnityEngine.VR.Utilities;
using UnityEngine;
using UnityEngine.UI;

public class CreatePrimitiveMenu : MonoBehaviour
{
	[SerializeField]
	private Slider m_ScaleSlider;

	[SerializeField]
	private Text m_ScaleLabel;

	public void CreatePrimitive(int type)
	{
		Transform primitive = GameObject.CreatePrimitive((PrimitiveType)type).transform;
		primitive.position = transform.position;
		primitive.localScale = Vector3.one * m_ScaleSlider.value;
		U.Object.Destroy(gameObject);
	}

	public void UpdateScaleValue()
	{
		m_ScaleLabel.text = m_ScaleSlider.value.ToString("0.0");
	}
}
