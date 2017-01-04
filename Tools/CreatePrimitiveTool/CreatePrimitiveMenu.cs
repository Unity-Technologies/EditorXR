using System;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Menus;

public class CreatePrimitiveMenu : MonoBehaviour, IMenu
{
	[SerializeField]
	GameObject[] m_HighlightObjects;

	public Action<PrimitiveType, bool> selectPrimitive;

	public bool visible { get { return gameObject.activeSelf; } set { gameObject.SetActive(value); } }

	public GameObject menuContent { get { return gameObject; } }

	public void SelectPrimitive(int type)
	{
		selectPrimitive((PrimitiveType)type, false);

		// the order of the objects in m_HighlightObjects is matched to the values of the PrimitiveType enum elements
		for (var i = 0; i < m_HighlightObjects.Length; i++)
		{
			var go = m_HighlightObjects[i];
			go.SetActive(i == type);
		}
	}

	public void SelectFreeformCuboid()
	{
		selectPrimitive(PrimitiveType.Cube, true);

		foreach (GameObject go in m_HighlightObjects)
			go.SetActive(false);
	}
}