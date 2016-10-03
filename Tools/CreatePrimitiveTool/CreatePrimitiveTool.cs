using System;
using UnityEngine;
using UnityEditor.VR;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;
using UnityEngine.InputNew;

public class CreatePrimitiveTool : MonoBehaviour, ITool, IStandardActionMap, IRay, IInstantiateUI, ICustomRay
{
	protected PrimitiveType s_SelectedPrimitiveType = PrimitiveType.Cube;
	protected bool s_Freeform = false;

	protected GameObject m_CurrentGameObject = null;

	protected Vector3 m_PointA = Vector3.zero;
	protected Vector3 m_PointB = Vector3.zero;

	protected const float kDrawDistance = 0.08f;
	protected const float kWaitTime = 0.2f;

	protected PrimitiveCreationStates m_State = PrimitiveCreationStates.PointA;

	public Standard standardInput {	get; set; }

	public Transform rayOrigin { get; set; }

	public Func<GameObject, GameObject> instantiateUI {	private get; set; }

	public Action hideDefaultRay { private get; set; }

	public Action showDefaultRay { private get; set; }

	protected enum PrimitiveCreationStates
	{
		PointA,
		PointB,
		Freeform,
	}

	protected bool m_RayHidden;

	protected virtual void Awake()
	{
	}

	void Start()
	{
		m_RayHidden = false;
	}

	void Update()
	{
		if(!m_RayHidden)
		{
			hideDefaultRay();
			m_RayHidden = true;
		}

		switch(m_State)
		{
			case PrimitiveCreationStates.PointA:
			{
				if(standardInput.action.wasJustPressed)
				{
					m_CurrentGameObject = GameObject.CreatePrimitive(s_SelectedPrimitiveType);
					m_CurrentGameObject.transform.localScale = new Vector3(0.0025f,0.0025f,0.0025f);

					m_PointA = rayOrigin.position + rayOrigin.forward * kDrawDistance;
					m_CurrentGameObject.transform.position = m_PointA;

					if(s_Freeform)
						m_State = PrimitiveCreationStates.Freeform;
					else
						m_State = PrimitiveCreationStates.PointB;
				}
				break;
			}
			case PrimitiveCreationStates.PointB:
			{
				m_PointB = rayOrigin.position + rayOrigin.forward * kDrawDistance;

				m_CurrentGameObject.transform.position = (m_PointA + m_PointB) * .5f;
				var corner = (m_PointA - m_PointB).magnitude;
				m_CurrentGameObject.transform.localScale = Vector3.one * corner;

				if(standardInput.action.wasJustReleased)
					m_State = PrimitiveCreationStates.PointA;

				break;
			}
			case PrimitiveCreationStates.Freeform:
			{
				m_PointB = rayOrigin.position + rayOrigin.forward * kDrawDistance;
				m_CurrentGameObject.transform.position = (m_PointA + m_PointB) * 0.5f;
				Vector3 maxCorner = Vector3.Max(m_PointA,m_PointB);
				Vector3 minCorner = Vector3.Min(m_PointA,m_PointB);
				m_CurrentGameObject.transform.localScale = (maxCorner - minCorner);

				if(standardInput.action.wasJustReleased)
					m_State = PrimitiveCreationStates.PointA;

				break;
			}
		}
	}
}