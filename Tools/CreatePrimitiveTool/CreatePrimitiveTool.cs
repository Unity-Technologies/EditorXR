using System;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Menus;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;

[MainMenuItem("Primitive", "Create", "Create primitives in the scene")]
public class CreatePrimitiveTool : MonoBehaviour, ITool, IStandardActionMap, IConnectInterfaces, IInstantiateMenuUI, IUsesRayOrigin, IUsesSpatialHash
{
	[SerializeField]
	CreatePrimitiveMenu m_MenuPrefab;

	const float kDrawDistance = 0.075f;

	GameObject m_ToolMenu;

	PrimitiveType m_SelectedPrimitiveType = PrimitiveType.Cube;
	bool m_Freeform;

	GameObject m_CurrentGameObject;

	Vector3 m_StartPoint = Vector3.zero;
	Vector3 m_EndPoint = Vector3.zero;

	PrimitiveCreationStates m_State = PrimitiveCreationStates.StartPoint;

	public Func<Transform, IMenu, GameObject> instantiateMenuUI { private get; set; }

	public Transform rayOrigin { get; set; }

	public ConnectInterfacesDelegate connectInterfaces { private get; set; }

	public Action<GameObject> addToSpatialHash { get; set; }
	public Action<GameObject> removeFromSpatialHash { get; set; }

	enum PrimitiveCreationStates
	{
		StartPoint,
		EndPoint,
		Freeform,
	}

	void Start()
	{
		m_ToolMenu = instantiateMenuUI(rayOrigin, m_MenuPrefab);
		var createPrimitiveMenu = m_ToolMenu.GetComponent<CreatePrimitiveMenu>();
		connectInterfaces(createPrimitiveMenu, rayOrigin);
		createPrimitiveMenu.selectPrimitive = SetSelectedPrimitive;
	}

	public void ProcessInput(ActionMapInput input, Action<InputControl> consumeControl)
	{
		var standardInput = (Standard)input;

		switch (m_State)
		{
			case PrimitiveCreationStates.StartPoint:
				{
					HandleStartPoint(standardInput, consumeControl);
					break;
				}
			case PrimitiveCreationStates.EndPoint:
				{
					UpdatePositions();
					SetScalingForObjectType();
					CheckForTriggerRelease(standardInput, consumeControl);
					break;
				}
			case PrimitiveCreationStates.Freeform:
				{
					UpdatePositions();
					UpdateFreeformScale();
					CheckForTriggerRelease(standardInput, consumeControl);
					break;
				}
		}
	}

	void SetSelectedPrimitive(PrimitiveType type, bool isFreeform)
	{
		m_SelectedPrimitiveType = type;
		m_Freeform = isFreeform;
	}

	void HandleStartPoint(Standard standardInput, Action<InputControl> consumeControl)
	{
		if (standardInput.action.wasJustPressed)
		{
			m_CurrentGameObject = GameObject.CreatePrimitive(m_SelectedPrimitiveType);
			
			// Set starting minimum scale (don't allow zero scale object to be created)
			const float kMinScale = 0.0025f;
			m_CurrentGameObject.transform.localScale = Vector3.one * kMinScale;
			m_StartPoint = rayOrigin.position + rayOrigin.forward * kDrawDistance;
			m_CurrentGameObject.transform.position = m_StartPoint;

			m_State = m_Freeform ? PrimitiveCreationStates.Freeform : PrimitiveCreationStates.EndPoint;

			addToSpatialHash(m_CurrentGameObject);

			consumeControl(standardInput.action);
		}
	}

	void SetScalingForObjectType()
	{
		var corner = (m_EndPoint - m_StartPoint).magnitude;

		// it feels better to scale these primitives vertically with the drawpoint
		if (m_SelectedPrimitiveType == PrimitiveType.Capsule || m_SelectedPrimitiveType == PrimitiveType.Cylinder || m_SelectedPrimitiveType == PrimitiveType.Cube)
			m_CurrentGameObject.transform.localScale = Vector3.one * corner * 0.5f;
		else
			m_CurrentGameObject.transform.localScale = Vector3.one * corner;
	}

	void UpdatePositions()
	{
		m_EndPoint = rayOrigin.position + rayOrigin.forward * kDrawDistance;
		m_CurrentGameObject.transform.position = (m_StartPoint + m_EndPoint) * 0.5f;
	}

	void UpdateFreeformScale()
	{
		var maxCorner = Vector3.Max(m_StartPoint, m_EndPoint);
		var minCorner = Vector3.Min(m_StartPoint, m_EndPoint);
		m_CurrentGameObject.transform.localScale = (maxCorner - minCorner);
	}

	void CheckForTriggerRelease(Standard standardInput, Action<InputControl> consumeControl)
	{
		// Ready for next object to be created
		if (standardInput.action.wasJustReleased)
		{
			m_State = PrimitiveCreationStates.StartPoint;

			consumeControl(standardInput.action);
		}
	}

	void OnDestroy()
	{
		U.Object.Destroy(m_ToolMenu);
	}
}
