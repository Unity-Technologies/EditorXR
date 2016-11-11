using System;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.VR.Tools;
using UnityEngine.InputNew;

[MainMenuItem("Primitive", "Primitive", "Create primitives in the scene")]
public class CreatePrimitiveTool : MonoBehaviour, ITool, IStandardActionMap, IConnectInterfaces, IUsesRayOrigin, ICustomMenuUI, ICustomRay
{
	private PrimitiveType m_SelectedPrimitiveType = PrimitiveType.Cube;
	private bool m_Freeform = false;

	[SerializeField]
	private CreatePrimitiveMenu m_MenuPrefab;
	private bool m_MenuSpawned;

	private GameObject m_ToolMenu = null;

	private GameObject m_CurrentGameObject = null;

	private const float kDrawDistance = 0.075f;

	private Vector3 m_PointA = Vector3.zero;
	private Vector3 m_PointB = Vector3.zero;

	private PrimitiveCreationStates m_State = PrimitiveCreationStates.PointA;

	public Standard standardInput {	get; set; }

	public Func<Transform, GameObject, GameObject> instantiateMenuUI { private get; set; }
	public Action<GameObject> destroyMenuUI { private get; set; }

	public Transform rayOrigin { get; set; }

	public Action hideDefaultRay { private get; set; }
	public Action showDefaultRay { private get; set; }

	public ConnectInterfacesDelegate connectInterfaces { private get; set; }

	private enum PrimitiveCreationStates
	{
		PointA,
		PointB,
		Freeform,
	}

	void Update()
	{
		if (!m_MenuSpawned)
			SpawnMenu();

		switch(m_State)
		{
			case PrimitiveCreationStates.PointA:
			{
				HandlePointA();
				break;
			}
			case PrimitiveCreationStates.PointB:
			{
				UpdatePositions();
				SetScalingForObjectType();
				CheckForTriggerRelease();
				break;
			}
			case PrimitiveCreationStates.Freeform:
			{
				UpdatePositions();
				UpdateFreeformScale();
				CheckForTriggerRelease();
				break;
			}
		}
	}

	void SpawnMenu()
	{
		m_ToolMenu = instantiateMenuUI(rayOrigin, m_MenuPrefab.gameObject);
		var createPrimitiveMenu = m_ToolMenu.GetComponent<CreatePrimitiveMenu>();
		connectInterfaces(createPrimitiveMenu, rayOrigin);
		createPrimitiveMenu.selectPrimitive += SetSelectedPrimitive;
		m_MenuSpawned = true;
	}

	void SetSelectedPrimitive(PrimitiveType type,bool isFreeform)
	{
		m_SelectedPrimitiveType = type;
		m_Freeform = isFreeform;
	}

	void HandlePointA()
	{
		if(standardInput.action.wasJustPressed)
		{
			m_CurrentGameObject = GameObject.CreatePrimitive(m_SelectedPrimitiveType);
			
			//set starting minimum scale (don't allow zero scale object to be created)
			m_CurrentGameObject.transform.localScale = new Vector3(0.0025f,0.0025f,0.0025f);
			m_PointA = rayOrigin.position + rayOrigin.forward * kDrawDistance;
			m_CurrentGameObject.transform.position = m_PointA;

			if(m_Freeform)
				m_State = PrimitiveCreationStates.Freeform;
			else
				m_State = PrimitiveCreationStates.PointB;
		}
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

	void UpdatePositions()
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
		//ready for next object to be created
		if(standardInput.action.wasJustReleased)
			m_State = PrimitiveCreationStates.PointA;
	}

	void OnDestroy()
	{
		destroyMenuUI(m_ToolMenu);
	}
}
