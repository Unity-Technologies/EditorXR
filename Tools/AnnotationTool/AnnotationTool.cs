using UnityEngine;
using System.Collections;
using UnityEngine.VR.Tools;
using System;
using UnityEngine.InputNew;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.VR.Utilities;

[MainMenuItem("Annotation", "Tools", "Draw in da spaaaaaace")]
public class AnnotationTool : MonoBehaviour, ITool, ICustomActionMap, IRay, ICustomRay, IOtherRay, IInstantiateUI
{
	public Transform rayOrigin { private get; set; }
	public Transform otherRayOrigin { private get; set; }

	public Action showDefaultRay { get; set; }
	public Action hideDefaultRay { get; set; }

	public ActionMap actionMap
	{
		get { return m_ActionMap; }
	}
	[SerializeField]
	private ActionMap m_ActionMap;

	public ActionMapInput actionMapInput { get { return m_AnnotationInput; } set { m_AnnotationInput = (AnnotationInput)value; } }

	public Func<GameObject, GameObject> instantiateUI { private get; set; }

	private AnnotationInput m_AnnotationInput;

	private const int kInitialListSize = 65535;

	private List<Vector3> m_Points = new List<Vector3>(kInitialListSize);
	private List<Vector3> m_Forwards = new List<Vector3>(kInitialListSize);

	private MeshFilter m_CurrentMeshFilter;
	private Color m_ColorToUse = Color.white;
	private Mesh m_CurrentMesh;
	private Matrix4x4 m_WorldToLocalMesh;

	[SerializeField]
	Material m_AnnotationMaterial;

	[SerializeField]
	Material m_ConeMaterial;

	[SerializeField]
	GameObject m_ColorPickerPrefab;
	GameObject m_ColorPicker;

	Transform m_AnnotationHolder;

	bool m_RayHidden;

	Mesh m_CustomPointer;

	const float kTopMinRadius = 0.001f;
	const float kTopMaxRadius = 0.05f;
	const float kBottomRadius = 0.01f;
	const float kTipDistance = 0.05f;
	const int kSides = 16;

	float m_CurrentRadius = kTopMinRadius;

	void OnDestroy()
	{
		if (m_RayHidden && showDefaultRay != null)
		{
			// HACK: In the previous dev version, hideDefaultRay also disabled the cone.
			var proxyRay = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
			if (proxyRay)
				proxyRay.transform.Find("Cone").gameObject.SetActive(true);

			showDefaultRay();
		}

		U.Object.Destroy(m_ColorPicker);
	}
	
	void Update()
	{
		if (!m_RayHidden)
		{
			if (hideDefaultRay != null)
			{
				hideDefaultRay();

				// HACK: In the previous dev version, hideDefaultRay also disabled the cone.
				var proxyRay = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
				if (proxyRay)
					proxyRay.transform.Find("Cone").gameObject.SetActive(false);

				m_RayHidden = true;
			}
		}

		if (rayOrigin != null)
		{
			GenerateCustomPointer();

			if (otherRayOrigin != null)
				CheckColorPicker();
		}

		if (m_AnnotationInput.draw.wasJustPressed)
			SetupAnnotation();
		if (m_AnnotationInput.draw.isHeld)
			UpdateAnnotation();

		if (m_AnnotationInput.changeBrushSize.value != 0)
			HandleBrushSize();
	}

	private void CheckColorPicker()
	{
		var distance = Vector3.Distance(rayOrigin.position, otherRayOrigin.position);
		float dot = Vector3.Dot(otherRayOrigin.right, rayOrigin.position - otherRayOrigin.position);
		if (distance < .325f && dot > 0)
		{
			if (m_ColorPicker == null)
			{
				m_ColorPicker = instantiateUI(m_ColorPickerPrefab);
				var colorPicker = m_ColorPicker.GetComponent<ColorPickerUI>();
				colorPicker.toolRayOrigin = rayOrigin;
				colorPicker.onColorPicked = HandleColoring;

				var pickerTransform = m_ColorPicker.transform;
				pickerTransform.SetParent(otherRayOrigin);
				pickerTransform.localPosition = m_ColorPickerPrefab.transform.localPosition;
				pickerTransform.localRotation = Quaternion.identity;
			}

			if (!m_ColorPicker.activeSelf)
				m_ColorPicker.SetActive(true);
		}
		else if (m_ColorPicker && m_ColorPicker.activeSelf)
		{
			m_ColorPicker.SetActive(false);
		}
	}

	private void HandleColoring(Color newColor)
	{
		m_ColorToUse = newColor;
	}

	private void HandleBrushSize()
	{
		if (m_CustomPointer != null)
		{
			var sign = m_AnnotationInput.changeBrushSize.value;
			m_CurrentRadius += sign * Time.unscaledDeltaTime * .1f;
			m_CurrentRadius = Mathf.Clamp(m_CurrentRadius, kTopMinRadius, kTopMaxRadius);

			var vertices = m_CustomPointer.vertices;
			for (int i = kSides; i < kSides * 2; i++)
			{
				float xPos = Mathf.Cos(i / 16f * Mathf.PI * 2) * m_CurrentRadius;
				float yPos = Mathf.Sin(i / 16f * Mathf.PI * 2) * m_CurrentRadius;
				Vector3 point = new Vector3(xPos, yPos, kTipDistance);
				vertices[i] = point;
			}
			m_CustomPointer.vertices = vertices;
		}
	}

	private void GenerateCustomPointer()
	{
		if (m_CustomPointer != null)
			return;

		m_CustomPointer = new Mesh();
		m_CustomPointer.vertices = GenerateVertices();
		m_CustomPointer.triangles = GenerateTriangles();

		var customPointer = new GameObject("CustomPointer");

		customPointer.AddComponent<MeshFilter>().sharedMesh = m_CustomPointer;
		customPointer.AddComponent<MeshRenderer>().sharedMaterial = m_ConeMaterial;

		var pointerTrans = customPointer.transform;
		pointerTrans.SetParent(rayOrigin);

		pointerTrans.localPosition = Vector3.zero;
		pointerTrans.localScale = Vector3.one;
		pointerTrans.localRotation = Quaternion.identity;
	}

	private Vector3[] GenerateVertices()
	{
		List<Vector3> points = new List<Vector3>();

		for (int capIndex = 0; capIndex < 2; capIndex++)
		{
			float radius = capIndex == 0 ? kBottomRadius : Mathf.Lerp(kTopMaxRadius, kTopMinRadius, capIndex);

			for (int i = 0; i < kSides; i++)
			{
				float xPos = Mathf.Cos(i / 16f * Mathf.PI * 2) * radius;
				float yPos = Mathf.Sin(i / 16f * Mathf.PI * 2) * radius;
				Vector3 point = new Vector3(xPos, yPos, capIndex * kTipDistance);
				points.Add(point);
			}
		}
		points.Add(new Vector3(0, 0, 0));
		points.Add(new Vector3(0, 0, kTipDistance));

		return points.ToArray();
	}

	private int[] GenerateTriangles()
	{
		List<int> triangles = new List<int>();

		GenerateSide(triangles);
		GenerateCaps(triangles);

		return triangles.ToArray();
	}

	private void GenerateSide(List<int> triangles)
	{
		for (int i = 1; i < kSides; i++)
		{
			int lowerLeft = i - 1;
			int lowerRight = i;
			int upperLeft = i + kSides - 1;
			int upperRight = i + kSides;

			triangles.Add(lowerLeft);
			triangles.Add(lowerRight);
			triangles.Add(upperLeft);

			triangles.Add(lowerRight);
			triangles.Add(upperRight);
			triangles.Add(upperLeft);
		}

		// Finish the side with a polygon that loops around from the end to the start vertices.
		triangles.Add(kSides - 1);
		triangles.Add(0);
		triangles.Add(kSides * 2 - 1);

		triangles.Add(0);
		triangles.Add(kSides);
		triangles.Add(kSides * 2 - 1);
	}

	private void GenerateCaps(List<int> triangles)
	{
		// Generate the bottom circle cap.
		for (int i = 1; i < kSides; i++)
		{
			int lowLeft = i - 1;
			int lowRight = i;
			int upLeft = kSides * 2;

			triangles.Add(upLeft);
			triangles.Add(lowRight);
			triangles.Add(lowLeft);
		}

		// Close the bottom circle cap with a start-end loop triangle.
		triangles.Add(kSides * 2);
		triangles.Add(0);
		triangles.Add(kSides - 1);

		// Generate the top circle cap.
		for (int i = kSides + 1; i < kSides * 2; i++)
		{
			int lowLeft = i - 1;
			int lowRight = i;
			int upLeft = kSides * 2 + 1;

			triangles.Add(lowLeft);
			triangles.Add(lowRight);
			triangles.Add(upLeft);
		}

		// Close the top circle cap with a start-end loop triangle.
		triangles.Add(kSides * 2 - 1);
		triangles.Add(kSides);
		triangles.Add(kSides * 2 + 1);
	}

	private void SetupAnnotation()
	{
		SetupHolder();

		m_Points.Clear();
		m_Forwards.Clear();

		GameObject go = new GameObject("Annotation " + m_AnnotationHolder.childCount);

		Transform goTrans = go.transform;
		goTrans.SetParent(m_AnnotationHolder);
		goTrans.position = rayOrigin.position;

		m_CurrentMeshFilter = go.AddComponent<MeshFilter>();
		MeshRenderer mRenderer = go.AddComponent<MeshRenderer>();
		var matToUse = Instantiate(m_AnnotationMaterial);
		matToUse.SetColor("_EmissionColor", m_ColorToUse);
		mRenderer.sharedMaterial = matToUse;

		m_WorldToLocalMesh = goTrans.worldToLocalMatrix;

		m_CurrentMesh = new Mesh();
		PointsToMesh();
	}

	private void SetupHolder()
	{
		if (m_AnnotationHolder == null)
		{
			var mainHolder = GameObject.Find("Annotations");
			if (!mainHolder)
				mainHolder = new GameObject("Annotations");

			var newSession = new GameObject("Session " + DateTime.Now.ToString());
			m_AnnotationHolder = newSession.transform;
			m_AnnotationHolder.SetParent(mainHolder.transform);
		}
	}

	private void UpdateAnnotation()
	{
		Vector3 rayForward = rayOrigin.forward;
		Vector3 worldPoint = rayOrigin.position + rayForward * kTipDistance;
		Vector3 localPoint = m_WorldToLocalMesh.MultiplyPoint3x4(worldPoint);

		if (m_Points.Count < 1 || Vector3.Distance(m_Points.Last(), localPoint) >= (m_CurrentRadius * .25f))
		{
			m_Points.Add(localPoint);
			m_Forwards.Add(rayForward);

			PointsToMesh();
		}
	}

	private void PointsToMesh()
	{
		if (m_Points.Count < 2)
			return;

		if (m_CurrentMesh == null)
			m_CurrentMesh = new Mesh();

		List<Vector3> newVertices = new List<Vector3>(m_CurrentMesh.vertices);
		List<int> newTriangles = new List<int>(m_CurrentMesh.triangles);
		
		for (int i = m_CurrentMesh.vertexCount / 2 + 1; i < m_Points.Count; i++)
		{
			Vector3 direction = (m_Points[i - 1] - m_Points[i]).normalized;
			Vector3 cross = Vector3.Cross(direction, m_Forwards[i - 1]) * m_CurrentRadius;

			newVertices.Add(m_Points[i - 1] - cross);
			newVertices.Add(m_Points[i - 1] + cross);
		}

		for (int i = Mathf.Max(3, m_CurrentMesh.vertexCount - 1); i < newVertices.Count; i += 2)
		{
			newTriangles.Add(i - 1);
			newTriangles.Add(i - 2);
			newTriangles.Add(i - 3);

			newTriangles.Add(i - 2);
			newTriangles.Add(i - 1);
			newTriangles.Add(i);

			// Add the same triangles in reverse to get a two-sided mesh.
			newTriangles.Add(i - 3);
			newTriangles.Add(i - 2);
			newTriangles.Add(i - 1);

			newTriangles.Add(i);
			newTriangles.Add(i - 1);
			newTriangles.Add(i - 2);
		}

		m_CurrentMesh.vertices = newVertices.ToArray();
		m_CurrentMesh.triangles = newTriangles.ToArray();
		m_CurrentMesh.UploadMeshData(false);

		m_CurrentMeshFilter.sharedMesh = m_CurrentMesh;
	}

}
