using System;
using UnityEngine;

public class CreatePrimitiveMenu : MonoBehaviour
{
	[SerializeField]
	GameObject[] m_HighlightObjects;

	public Action<PrimitiveType, bool> selectPrimitive;

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