using System;
using UnityEngine;
using UnityEditor.VR;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;
using UnityEngine.InputNew;

[MainMenuItem("Primitive", "Primitive", "Create primitives in the scene")]
public class CreatePrimitiveTool : MonoBehaviour, ITool, IStandardActionMap, IRay, IInstantiateUI, ICustomRay
{
	public static PrimitiveType s_SelectedPrimitiveType = PrimitiveType.Cube;
	public static bool s_Freeform = false;

	private GameObject m_CurrentGameObject = null;
	//private float m_CurrentDistance;

	private Vector3 m_PointA = Vector3.zero;
	private Vector3 m_PointB = Vector3.zero;

	private const float kDrawDistance = 0.08f;
	private const float kWaitTime = 0.2f;

	private float m_TimeStamp = 0.0f;

	private PrimitiveCreationStates m_State = PrimitiveCreationStates.PointA;

	[SerializeField]
	private Canvas CanvasPrefab;
	private Canvas m_ToolCanvas;
	private bool m_ToolCanvasSpawned = false;
	
	public Standard standardInput
	{
		get; set;
	}

	public Transform rayOrigin
	{
		get; set;
	}

	public Func<GameObject, GameObject> instantiateUI
	{
		private get; set;
	}

	public Action hideDefaultRay
	{
		private get; set;
	}
	public Action showDefaultRay
	{
		private get; set;
	}

	private enum PrimitiveCreationStates
	{
		PointA,
		PointB,
		Freeform,
	}

	void Awake()
	{
		m_TimeStamp = 0.0f;
		m_ToolCanvasSpawned = false;
	}

	void Update()
	{
		if(!m_ToolCanvasSpawned && standardInput.action.wasJustPressed)
		{
			if(m_ToolCanvas == null)
			{
				var go = instantiateUI(CanvasPrefab.gameObject);
				m_ToolCanvas = go.GetComponent<Canvas>();
				m_ToolCanvasSpawned = true;
			}
			m_ToolCanvas.transform.position = rayOrigin.position + rayOrigin.forward * 20f;
			m_ToolCanvas.transform.rotation = Quaternion.LookRotation(m_ToolCanvas.transform.position - VRView.viewerCamera.transform.position);
			hideDefaultRay();
			return;
		}

		switch(m_State)
		{
			case PrimitiveCreationStates.PointA:
			{
				if(standardInput.action.wasJustPressed)
				{
					m_CurrentGameObject = GameObject.CreatePrimitive(s_SelectedPrimitiveType);
					m_CurrentGameObject.GetComponent<Collider>().enabled = false;
					m_CurrentGameObject.transform.localScale = new Vector3(0.0025f,0.0025f,0.0025f);

					m_PointA = rayOrigin.position + rayOrigin.forward * kDrawDistance;

					m_CurrentGameObject.transform.position = m_PointA;

					if(s_Freeform)
						m_State = PrimitiveCreationStates.Freeform;
					else
						m_State = PrimitiveCreationStates.PointB;

					m_TimeStamp = Time.realtimeSinceStartup;
					break;
				}
				break;
			}
			case PrimitiveCreationStates.PointB:
			{
				m_PointB = rayOrigin.position + rayOrigin.forward * kDrawDistance;
                float dist = Vector3.Distance(m_PointA,m_PointB);
				m_CurrentGameObject.transform.localScale = new Vector3(1.0f,1.0f,1.0f) * dist;
				m_CurrentGameObject.transform.position = m_PointB;

				if(standardInput.action.wasJustReleased)
				{
					m_CurrentGameObject.GetComponent<Collider>().enabled = true;
					m_State = PrimitiveCreationStates.PointA;
				}
				break;
			}
			case PrimitiveCreationStates.Freeform:
			{
				m_PointB = rayOrigin.position + rayOrigin.forward * kDrawDistance;
				m_CurrentGameObject.transform.position = (m_PointA + m_PointB) * .5f;
				Vector3 maxCorner = Vector3.Max(m_PointA,m_PointB);
				Vector3 minCorner = Vector3.Min(m_PointA,m_PointB);

				m_CurrentGameObject.transform.localScale = (maxCorner - minCorner);

				if(standardInput.action.wasJustReleased)
				{
					m_CurrentGameObject.GetComponent<Collider>().enabled = true;
					m_State = PrimitiveCreationStates.PointA;
				}
				break;
			}
		}
	}

	void OnDestroy()
	{
		if (m_ToolCanvas != null)
			U.Object.Destroy(m_ToolCanvas.gameObject);
	}
}