using System;
using UnityEngine;

public class CreatePrimitiveMenu : MonoBehaviour
{
	[SerializeField]
	GameObject[] HighlightObjects;

	public Action<PrimitiveType, bool> selectPrimitive;

	public void CreatePrimitive(int type)
	{
		selectPrimitive((PrimitiveType)type,false);

		foreach(GameObject go in HighlightObjects)
			go.SetActive(false);

		HighlightObjects[type].SetActive(true);
	}

	public void CreateFreeformCube()
	{
		selectPrimitive(PrimitiveType.Cube,true);

		foreach(GameObject go in HighlightObjects)
			go.SetActive(false);
	}
}