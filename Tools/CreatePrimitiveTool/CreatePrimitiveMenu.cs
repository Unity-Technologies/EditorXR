using System;
using UnityEngine;
using UnityEngine.VR.Tools;

public class CreatePrimitiveMenu : MonoBehaviour, ICustomRay
{
	[SerializeField]
	GameObject[] m_HighlightObjects;

	public Transform rayOrigin { get; set; }

	public Action hideDefaultRay { private get; set; }

	public Action showDefaultRay { private get; set; }

	public Action<PrimitiveType, bool> selectPrimitive;

	bool m_HideRayFirstFrame = true;

	void OnEnable()
	{
		if(hideDefaultRay != null)
			hideDefaultRay();
	}

	void OnDisable()
	{
		if(showDefaultRay != null)
			showDefaultRay();
	}

	void Update()
	{
		//interface is connected after OnEnalbe can run first time
		if (m_HideRayFirstFrame && hideDefaultRay != null)
		{
			hideDefaultRay();
			m_HideRayFirstFrame = false;
		}
	}

	public void CreatePrimitive(int type)
	{
		selectPrimitive((PrimitiveType)type,false);

		foreach(GameObject go in m_HighlightObjects)
			go.SetActive(false);

		// the order of the objects in m_HighlightObjects is matched to the values of the PrimitiveType enum elements
		m_HighlightObjects[type].SetActive(true);
	}

	public void CreateFreeformCube()
	{
		selectPrimitive(PrimitiveType.Cube,true);

		foreach(GameObject go in m_HighlightObjects)
			go.SetActive(false);
	}
}
