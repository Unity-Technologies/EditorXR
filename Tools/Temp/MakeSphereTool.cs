using System;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;

[MainMenuItem("Sphere", "Create", "Create spheres in the scene")]
public class MakeSphereTool : MonoBehaviour, ITool, ICustomActionMap, IRay
{	
	public Transform rayOrigin { get; set; }
	public Node selfNode { get; set; }

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

	private void Update()
	{
		if (m_Standard.action.wasJustPressed)
		{
			Transform cube = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
			if (rayOrigin)
				cube.position = rayOrigin.position + rayOrigin.forward * 5f;
		}
	}
}
