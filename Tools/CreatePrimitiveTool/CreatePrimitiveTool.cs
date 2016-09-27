using System;
using UnityEngine;
using UnityEditor.VR;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;
using UnityEngine.InputNew;

[MainMenuItem("Primitive", "Primitive", "Create primitives in the scene")]
public class CreatePrimitiveTool : MonoBehaviour, ITool, IStandardActionMap, IRay, IInstantiateUI
{
	[SerializeField]
	private Canvas CanvasPrefab;
	private Canvas m_ToolCanvas;

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

	private enum cubeCreationStates
	{
		pointA,
		pointB,
		pointC,
		pointD,
	}

	cubeCreationStates cube_state = cubeCreationStates.pointA;
	GameObject current = null;

	Vector3 a = Vector3.zero;
	Vector3 b = Vector3.zero;
	Vector3 c = Vector3.zero;
	Vector3 d = Vector3.zero;
	Vector3 ab = Vector3.zero;
	Vector3 bc = Vector3.zero;

	GameObject m_drawSphere;
	float kDrawSphereDistance = 1.5f;
	float kDrawSphereSize = 0.025f;

	[SerializeField]
	PrimitiveType m_SelectedPrimitive = PrimitiveType.Cube;

	void Awake()
	{
		m_drawSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        m_drawSphere.GetComponent<MeshRenderer>().sharedMaterial.color = Color.white;
		m_drawSphere.transform.position = new Vector3(1f,1f,1f);
		m_drawSphere.transform.localScale = new Vector3(kDrawSphereSize,kDrawSphereSize,kDrawSphereSize);
	}

	void Update()
	{
		m_drawSphere.transform.position = rayOrigin.position + rayOrigin.forward * kDrawSphereDistance;
		switch(cube_state)
		{
			case cubeCreationStates.pointA:
				{
					if(standardInput.action.wasJustPressed)
					{
						current = GameObject.CreatePrimitive(m_SelectedPrimitive);
						current.transform.localScale = new Vector3(0.01f,0.01f,0.01f);
						current.transform.position = rayOrigin.position + rayOrigin.forward * kDrawSphereDistance;
						a = rayOrigin.position + rayOrigin.forward * kDrawSphereDistance;
						cube_state = cubeCreationStates.pointB;
						return;
					}
					break;
				}

			case cubeCreationStates.pointB:
				{
					b = rayOrigin.position + rayOrigin.forward * kDrawSphereDistance;
					current.transform.LookAt(b);
					Vector3 temp_scale = current.transform.localScale;
					temp_scale.z = Vector3.Distance(a,b);
					current.transform.position = a + ((b - a).normalized * Vector3.Distance(a,b) / 2.0f);
					current.transform.localScale = temp_scale;

					if(standardInput.action.wasJustPressed)
					{
						b = rayOrigin.position + rayOrigin.forward * kDrawSphereDistance;
						ab = current.transform.position;
                        cube_state = cubeCreationStates.pointC;
					}
					break;
				}

			case cubeCreationStates.pointC:
				{
					c = rayOrigin.position + rayOrigin.forward * kDrawSphereDistance;
					Vector3 temp_scale = current.transform.localScale;

					Vector3 t = Quaternion.Euler(0.0f,90.0f,0.0f) * (b - a);
					temp_scale.x = Vector3.Distance(b,c) * Mathf.Sign(Vector3.Dot(t,(c - b)));

					current.transform.position = ab + (current.transform.right * Mathf.Sign(Vector3.Dot(t,(c - b))) * Vector3.Distance(b,c) / 2.0f);
					current.transform.localScale = temp_scale;

					if(standardInput.action.wasJustPressed)
					{
						c = rayOrigin.position + rayOrigin.forward * kDrawSphereDistance;
						bc = current.transform.position;
                        cube_state = cubeCreationStates.pointD;
					}

					break;
				}
			case cubeCreationStates.pointD:
				{
					d = rayOrigin.position + rayOrigin.forward * kDrawSphereDistance;
					Vector3 temp_scale = current.transform.localScale;

					Vector3 t = Quaternion.Euler(0.0f,90.0f,0.0f) * (c - b);
					temp_scale.y = Vector3.Distance(c,d) * Mathf.Sign(Vector3.Dot(t,(d - c)));
					
					current.transform.position = bc + (current.transform.up * Mathf.Sign(Vector3.Dot(t,(d - c))) * Vector3.Distance(c,d) / 2.0f);
					current.transform.localScale = temp_scale;

					if(standardInput.action.wasJustPressed)
					{
						//d = rayOrigin.position + rayOrigin.forward * 2f;
						cube_state = cubeCreationStates.pointA;
						a = Vector3.zero;
						b = Vector3.zero;
						c = Vector3.zero;
						d = Vector3.zero;
						ab = Vector3.zero;
						bc = Vector3.zero;
					}
					break;
				}

		}



		//if(standardInput.action.wasJustPressed)
		//{
		//	if(m_ToolCanvas == null)
		//	{
		//		var go = instantiateUI(CanvasPrefab.gameObject);
		//		m_ToolCanvas = go.GetComponent<Canvas>();
		//	}
		//	m_ToolCanvas.transform.position = rayOrigin.position + rayOrigin.forward * 5f;
		//	m_ToolCanvas.transform.rotation = Quaternion.LookRotation(m_ToolCanvas.transform.position - VRView.viewerCamera.transform.position);
		//}
	}
	void OnDestroy()
	{
		if(m_drawSphere != null)
			U.Object.Destroy(m_drawSphere);

		if (m_ToolCanvas != null)
			U.Object.Destroy(m_ToolCanvas.gameObject);
	}
}
