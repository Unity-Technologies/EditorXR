using System;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.VR.Tools;
using UnityEngine.InputNew;

[MainMenuItem("Primitive", "Primitive", "create primitives")]
public class CreatePrimitiveTool : MonoBehaviour, ITool, IStandardActionMap, IRay, IInstantiateMenuUI, ICustomRay
{
	private PrimitiveType m_SelectedPrimitiveType = PrimitiveType.Cube;
	private bool m_Freeform = false;

	[SerializeField]
	private Canvas m_CanvasPrefab;
	private bool m_CanvasSpawned;

	private GameObject m_CurrentGameObject = null;

	private const float kDrawDistance = 0.075f;

	private Vector3 m_PointA = Vector3.zero;
	private Vector3 m_PointB = Vector3.zero;

	private PrimitiveCreationStates m_State = PrimitiveCreationStates.PointA;

	public Standard standardInput {	get; set; }

	public Transform rayOrigin { get; set; }

	public Func<Node,MenuOrigin,GameObject,GameObject> instantiateMenuUI { private get; set; }

	public Action hideDefaultRay { private get; set; }

	public Action showDefaultRay { private get; set; }

	public Node node { private get; set; }

	private enum PrimitiveCreationStates
	{
		PointA,
		PointB,
		Freeform,
	}

	void Update()
	{
		if(!m_CanvasSpawned)
			SpawnCanvas();

		switch(m_State)
		{
			case PrimitiveCreationStates.PointA:
			{
				if(standardInput.action.wasJustPressed)
				{
					SetStartPoint();

					if(m_Freeform)
						m_State = PrimitiveCreationStates.Freeform;
					else
						m_State = PrimitiveCreationStates.PointB;
				}
				break;
			}
			case PrimitiveCreationStates.PointB:
			{
				UpdateEndPoint();
				SetScalingForObjectType();
				CheckForTriggerRelease();
				break;
			}
			case PrimitiveCreationStates.Freeform:
			{
				UpdateEndPoint();
				UpdateFreeformScale();
				CheckForTriggerRelease();
				break;
			}
		}
	}

	void SpawnCanvas()
	{
		hideDefaultRay();
		var go = instantiateMenuUI(node,MenuOrigin.Main,m_CanvasPrefab.gameObject);
		go.GetComponent<CreatePrimitiveMenu>().selectPrimitive += SetSelectedPrimitive;
		m_CanvasSpawned = true;
	}

	void SetSelectedPrimitive(PrimitiveType type,bool isFreeform)
	{
		m_SelectedPrimitiveType = type;
		m_Freeform = isFreeform;
	}

	void SetStartPoint()
	{
		m_CurrentGameObject = GameObject.CreatePrimitive(m_SelectedPrimitiveType);
		m_CurrentGameObject.transform.localScale = new Vector3(0.0025f,0.0025f,0.0025f);
		m_PointA = rayOrigin.position + rayOrigin.forward * kDrawDistance;
		m_CurrentGameObject.transform.position = m_PointA;
	}

	void SetScalingForObjectType()
	{
		var corner = (m_PointA - m_PointB).magnitude;

		// it feels better to scale the capsule and cylinder type primitives vertically with the drawpoint
		if(m_SelectedPrimitiveType == PrimitiveType.Capsule || m_SelectedPrimitiveType == PrimitiveType.Cylinder)
			m_CurrentGameObject.transform.localScale = Vector3.one * corner * 0.5f;
		else
			m_CurrentGameObject.transform.localScale = Vector3.one * corner;
	}

	void UpdateEndPoint()
	{
		m_PointB = rayOrigin.position + rayOrigin.forward * kDrawDistance;
		m_CurrentGameObject.transform.position = (m_PointA + m_PointB) * 0.5f;
	}

	void UpdateFreeformScale()
	{
		Vector3 maxCorner = Vector3.Max(m_PointA,m_PointB);
		Vector3 minCorner = Vector3.Min(m_PointA,m_PointB);
		m_CurrentGameObject.transform.localScale = (maxCorner - minCorner);
	}

	void CheckForTriggerRelease()
	{
		if(standardInput.action.wasJustReleased)
			m_State = PrimitiveCreationStates.PointA;
	}
}