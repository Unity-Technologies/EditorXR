using System;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.Experimental.EditorVR.Menus;
using UnityEngine.Experimental.EditorVR.Tools;

//[MainMenuItem("Sphere", "Create", "Create spheres in the scene")]
[MainMenuItem(false)]
public class MakeSphereTool : MonoBehaviour, ITool, ICustomActionMap, IUsesRayOrigin, IUsesSpatialHash
{	
	public Transform rayOrigin { get; set; }

	public ActionMap actionMap
	{
		get
		{
			return m_ActionMap;
		}
		set
		{
			m_ActionMap = value;
		}
	}

	[SerializeField]
	private ActionMap m_ActionMap;

	public Action<GameObject> addToSpatialHash { get; set; }
	public Action<GameObject> removeFromSpatialHash { get; set; }

	public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
	{
		var standardAlt = (StandardAlt)input;
		if (standardAlt.action.wasJustPressed)
		{
			Transform sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
			if (rayOrigin)
				sphere.position = rayOrigin.position + rayOrigin.forward * 5f;

			addToSpatialHash(sphere.gameObject);

			consumeControl(standardAlt.action);
		}
	}
}
