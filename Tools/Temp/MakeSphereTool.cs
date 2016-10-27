using System;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;
using Object = UnityEngine.Object;

//[MainMenuItem("Sphere", "Create", "Create spheres in the scene")]
[MainMenuItem(false)]
public class MakeSphereTool : MonoBehaviour, ITool, ICustomActionMap, IRay, ISpatialHash
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

	public Action<Object> addObjectToSpatialHash { get; set; }
	public Action<Object> removeObjectFromSpatialHash { get; set; }

	private void Update()
	{
		if (m_Standard.action.wasJustPressed)
		{
			Transform cube = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
			if (rayOrigin)
				cube.position = rayOrigin.position + rayOrigin.forward * 5f;

			addObjectToSpatialHash(cube);
		}
	}
}