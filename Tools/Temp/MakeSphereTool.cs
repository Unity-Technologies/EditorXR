using System;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;
using Object = UnityEngine.Object;

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

	public ActionMapInput actionMapInput
	{
		get
		{
			return m_Standard;
		}
		set
		{
			m_Standard = (StandardAlt)value;
		}
	}

	[SerializeField]
	private ActionMap m_ActionMap;
	[SerializeField]
	private StandardAlt m_Standard;

	public Action<GameObject> addToSpatialHash { get; set; }
	public Action<GameObject> removeFromSpatialHash { get; set; }

	private void Update()
	{
		if (m_Standard.action.wasJustPressed)
		{
			Transform sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
			if (rayOrigin)
				sphere.position = rayOrigin.position + rayOrigin.forward * 5f;

			addToSpatialHash(sphere.gameObject);
		}
	}
}
