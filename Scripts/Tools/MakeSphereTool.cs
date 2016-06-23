using System;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;

public class MakeSphereTool : MonoBehaviour, ITool, ICustomActionMap, IRay
{	
	public Transform RayOrigin { get; set; }

	public ActionMap ActionMap
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

	public ActionMapInput ActionMapInput
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
			if (RayOrigin)
			{
				cube.position = RayOrigin.position + RayOrigin.forward * 5f;
				cube.parent = transform;
			}
		}
	}
}
