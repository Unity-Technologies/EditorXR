using System;
using UnityEngine;
using UnityEditor.VR;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;
using UnityEngine.InputNew;

[MainMenuItem("Primitive", "Primitive", "Create primitives in the scene")]
public class CreatePrimitiveTool : MonoBehaviour, ITool, IStandardActionMap, IRay, IInstantiateUI
{
	public static PrimitiveType s_SelectedPrimitiveType = PrimitiveType.Cube;

	private GameObject m_CurrentGameObject = null;
	private float m_CurrentDistance;

	private Vector3 m_PointA = Vector3.zero;
	private Vector3 m_PointB = Vector3.zero;

	private const float kDrawDistance = 10.0f;
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

	private enum PrimitiveCreationStates
	{
		PointA,
		Delay,
		PointB,
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
			m_ToolCanvas.transform.position = rayOrigin.position + rayOrigin.forward * kDrawDistance * 2f;
			m_ToolCanvas.transform.rotation = Quaternion.LookRotation(m_ToolCanvas.transform.position - VRView.viewerCamera.transform.position);
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
					m_CurrentGameObject.transform.localScale = new Vector3(1.0f,1.0f,1.0f);

					Vector3 spawnPos = Vector3.zero;
                    RaycastHit hit;
					Ray r = new Ray(rayOrigin.position,rayOrigin.forward);
					if(Physics.Raycast(r,out hit))
					{
						Vector3 temp = m_CurrentGameObject.GetComponent<MeshRenderer>().bounds.extents;
						temp.x = 0.0f;
						temp.z = 0.0f;
						spawnPos = hit.point + Quaternion.FromToRotation(Vector3.up,hit.normal) * temp;
						m_CurrentDistance = hit.distance;
					}
					else
					{
						m_CurrentDistance = 10.0f;
						spawnPos = r.GetPoint(kDrawDistance);
					}

					m_CurrentGameObject.transform.position = spawnPos;
					m_PointA = spawnPos;
					m_State = PrimitiveCreationStates.Delay;
					m_TimeStamp = Time.realtimeSinceStartup;
					break;
				}
				break;
			}

			case PrimitiveCreationStates.Delay:
			{
				if(Time.realtimeSinceStartup - m_TimeStamp > kWaitTime)
				{
					m_State = PrimitiveCreationStates.PointB;
					break;
				}
				if(standardInput.action.wasJustReleased)
				{
					m_State = PrimitiveCreationStates.PointA;
					m_CurrentGameObject.GetComponent<Collider>().enabled = true;
					break;
				}
				break;
			}
			case PrimitiveCreationStates.PointB:
			{
				m_PointB = rayOrigin.position + rayOrigin.forward * m_CurrentDistance;
				Vector3 temp_scale = m_CurrentGameObject.transform.localScale;
				float dist = Vector3.Distance(m_PointA,m_PointB);
				temp_scale = new Vector3(1.0f,1.0f,1.0f) * dist;
				m_CurrentGameObject.transform.localScale = temp_scale;
				m_CurrentGameObject.transform.position = m_PointA + ((m_PointB - m_PointA).normalized * dist / 2.0f);

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