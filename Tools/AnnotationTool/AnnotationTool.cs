using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR;

[MainMenuItem("Annotation", "Create", "Draw in 3D")]
public class AnnotationTool : MonoBehaviour, ITool, ICustomActionMap, IUsesRayOrigin, ICustomRay, IUsesRayOrigins, IInstantiateUI, IUsesMenuOrigins, IUsesCustomMenuOrigins, IUsesRayLocking
{
	[SerializeField]
	ActionMap m_ActionMap;

	[SerializeField]
	Material m_AnnotationMaterial;

	[SerializeField]
	Material m_ConeMaterial;

	[SerializeField]
	GameObject m_BrushSizePrefab;

	[SerializeField]
	GameObject m_ColorPickerActivatorPrefab;

	Action<float> onBrushSizeChanged { set; get; }

	const int k_InitialListSize = 32767;

	List<Vector3> m_Points = new List<Vector3>(k_InitialListSize);
	List<Vector3> m_Forwards = new List<Vector3>(k_InitialListSize);
	List<float> m_Widths = new List<float>(k_InitialListSize);
	List<Vector3> m_Rights = new List<Vector3>(k_InitialListSize);

	MeshFilter m_CurrentMeshFilter;
	Color m_ColorToUse = Color.white;
	Mesh m_CurrentMesh;
	Matrix4x4 m_WorldToLocalMesh;

	Material m_ConeMaterialInstance;
	ColorPickerUI m_ColorPicker;
	BrushSizeUI m_BrushSizeUi;

	Transform m_AnnotationHolder;

	bool m_IsRayHidden;
	bool m_IsValidStroke;

	Mesh m_CustomPointerMesh;
	GameObject m_CustomPointerObject;

	GameObject m_ColorPickerActivator;

	const float k_TopMinRadius = 0.0025f;
	const float k_TopMaxRadius = 0.05f;
	const float k_BottomRadius = 0.01f;
	const float k_TipDistance = 0.05f;
	const int k_Sides = 16;

	float m_CurrentRadius = k_TopMinRadius;

	List<GameObject> m_UndoList = new List<GameObject>();

	public Transform rayOrigin { private get; set; }
	public List<Transform> otherRayOrigins { private get; set; }

	public Transform menuOrigin { set; private get; }
	public Transform alternateMenuOrigin { set; private get; }

	public Func<Transform, Transform> customMenuOrigin { private get; set; }
	public Func<Transform, Transform> customAlternateMenuOrigin { private get; set; }

	public ActionMap actionMap
	{
		get { return m_ActionMap; }
	}

	void OnDestroy()
	{
		if (m_IsRayHidden)
		{
			this.UnlockRay(rayOrigin, this);
			this.ShowDefaultRay(rayOrigin);
		}

		if (m_ColorPicker)
			ObjectUtils.Destroy(m_ColorPicker.gameObject);
		if (m_BrushSizeUi)
			ObjectUtils.Destroy(m_BrushSizeUi.gameObject);
		if (m_ColorPickerActivator)
			ObjectUtils.Destroy(m_ColorPickerActivator);

		if (m_CustomPointerObject)
			DestroyImmediate(m_CustomPointerObject);
	}
	
	void Update()
	{
		HideRay();
		HandleRayOrigins();
	}

	void HideRay()
	{
		if (!m_IsRayHidden)
		{
			this.HideDefaultRay(rayOrigin);
			this.LockRay(rayOrigin, this);
			m_IsRayHidden = true;
		}
	}

	void HandleRayOrigins()
	{
		if (rayOrigin != null)
		{
			GenerateCustomPointer();
			CheckBrushSizeUi();

			if (m_ColorPickerActivator == null)
			{
				m_ColorPickerActivator = this.InstantiateUI(m_ColorPickerActivatorPrefab);
				var otherAltMenu = customAlternateMenuOrigin(otherRayOrigins[0]);
				
				m_ColorPickerActivator.transform.SetParent(otherAltMenu.GetComponentInChildren<MainMenuActivator>().transform);
				m_ColorPickerActivator.transform.localRotation = Quaternion.identity;
				m_ColorPickerActivator.transform.localPosition = Vector3.right * 0.05f;

				var activator = m_ColorPickerActivator.GetComponent<ColorPickerActivator>();

				m_ColorPicker = activator.GetComponentInChildren<ColorPickerUI>(true);
				m_ColorPicker.onHideCalled = HideColorPicker;
				m_ColorPicker.toolRayOrigin = rayOrigin;
				m_ColorPicker.onColorPicked = OnColorPickerValueChanged;

				activator.rayOrigin = otherRayOrigins.First();
				activator.showColorPicker = ShowColorPicker;
				activator.hideColorPicker = HideColorPicker;
			}
		}
	}

	void UndoLast()
	{
		if (m_UndoList.Count > 0)
		{
			var first = m_UndoList.Last();
			DestroyImmediate(first);
			m_UndoList.RemoveAt(m_UndoList.Count - 1);

			// Clean up after the removed annotations if necessary.
			if (m_AnnotationHolder.childCount == 0)
			{
				var root = m_AnnotationHolder.parent;
				var index = m_AnnotationHolder.GetSiblingIndex();
				DestroyImmediate(m_AnnotationHolder.gameObject);

				if (root.childCount == 0)
					DestroyImmediate(root.gameObject);
				else
				{
					if (index > 0)
						m_AnnotationHolder = root.GetChild(index - 1);
					else if (index < root.childCount)
						m_AnnotationHolder = root.GetChild(index);
				}
			}
		}
	}

	void CheckBrushSizeUi()
	{
		if (m_BrushSizeUi == null)
		{
			var brushSizeUi = this.InstantiateUI(m_BrushSizePrefab);
			m_BrushSizeUi = brushSizeUi.GetComponent<BrushSizeUI>();

			var trans = brushSizeUi.transform;
			trans.SetParent(alternateMenuOrigin);
			trans.localPosition = Vector3.zero;
			trans.localRotation = Quaternion.Euler(-90, 0, 0);

			m_BrushSizeUi.onValueChanged = (val) => 
			{
				m_CurrentRadius = Mathf.Lerp(k_TopMinRadius, k_TopMaxRadius, val);
				ResizePointer();
			};
			onBrushSizeChanged = m_BrushSizeUi.ChangeSliderValue;
		}
	}

	void ShowColorPicker(Transform otherRayOrigin)
	{
		if (m_IsValidStroke)
			return;

		if (!m_ColorPicker.enabled)
			m_ColorPicker.Show();

		this.UnlockRay(rayOrigin, this);
		this.ShowDefaultRay(rayOrigin);
		m_CustomPointerObject.SetActive(false);
	}

	void HideColorPicker()
	{
		if (m_ColorPicker && m_ColorPicker.enabled)
		{
			m_ColorPicker.Hide();
			this.HideDefaultRay(rayOrigin);
			this.LockRay(rayOrigin, this);
			m_CustomPointerObject.SetActive(true);
		}
	}

	void OnColorPickerValueChanged(Color newColor)
	{
		m_ColorToUse = newColor;

		newColor.a = .75f;
		m_ConeMaterialInstance.SetColor("_EmissionColor", newColor);

		m_BrushSizeUi.OnBrushColorChanged(newColor);
	}

	void HandleBrushSize(float value)
	{
		if (m_CustomPointerMesh != null)
		{
			if (VRSettings.loadedDeviceName == "OpenVR") // For vive controllers, use 1:1 touchpad setting.
			{
				m_CurrentRadius = Mathf.Lerp(k_TopMinRadius, k_TopMaxRadius, (value + 1) / 2f);
			}
			else // For touch and hydra, let the thumbstick gradually modifiy the width.
			{
				m_CurrentRadius += value * Time.unscaledDeltaTime * .1f;
				m_CurrentRadius = Mathf.Clamp(m_CurrentRadius, k_TopMinRadius, k_TopMaxRadius);
			}

			if (m_BrushSizeUi && onBrushSizeChanged != null)
			{
				var ratio = Mathf.InverseLerp(k_TopMinRadius, k_TopMaxRadius, m_CurrentRadius);
				onBrushSizeChanged(ratio);
			}

			ResizePointer();
		}
	}

	void ResizePointer()
	{
		var vertices = m_CustomPointerMesh.vertices;
		for (var i = k_Sides; i < k_Sides * 2; i++)
		{
			var angle = (i / (float)k_Sides) * Mathf.PI * 2f;
			var xPos = Mathf.Cos(angle) * m_CurrentRadius;
			var yPos = Mathf.Sin(angle) * m_CurrentRadius;

			var point = new Vector3(xPos, yPos, k_TipDistance);
			vertices[i] = point;
		}
		m_CustomPointerMesh.vertices = vertices;
	}

	void GenerateCustomPointer()
	{
		if (m_CustomPointerMesh != null)
			return;

		m_CustomPointerMesh = new Mesh();
		m_CustomPointerMesh.vertices = GeneratePointerVertices();
		m_CustomPointerMesh.triangles = GeneratePointerTriangles();

		m_CustomPointerObject = new GameObject("CustomPointer");

		m_CustomPointerObject.AddComponent<MeshFilter>().sharedMesh = m_CustomPointerMesh;
		
		m_ConeMaterialInstance = Instantiate(m_ConeMaterial);
		m_CustomPointerObject.AddComponent<MeshRenderer>().sharedMaterial = m_ConeMaterialInstance;

		var pointerTrans = m_CustomPointerObject.transform;
		pointerTrans.SetParent(rayOrigin);

		pointerTrans.localPosition = Vector3.zero;
		pointerTrans.localScale = Vector3.one;
		pointerTrans.localRotation = Quaternion.identity;
	}

	static Vector3[] GeneratePointerVertices()
	{
		var points = new List<Vector3>();

		for (var capIndex = 0; capIndex < 2; capIndex++)
		{
			float radius = capIndex == 0 ? k_BottomRadius : Mathf.Lerp(k_TopMaxRadius, k_TopMinRadius, capIndex);

			for (var i = 0; i < k_Sides; i++)
			{
				var angle = (i / (float)k_Sides) * Mathf.PI * 2f;
				var xPos = Mathf.Cos(angle) * radius;
				var yPos = Mathf.Sin(angle) * radius;

				var point = new Vector3(xPos, yPos, capIndex * k_TipDistance);
				points.Add(point);
			}
		}
		points.Add(new Vector3(0, 0, 0));
		points.Add(new Vector3(0, 0, k_TipDistance));

		return points.ToArray();
	}

	static int[] GeneratePointerTriangles()
	{
		var triangles = new List<int>();

		GeneratePointerSideTriangles(triangles);
		GeneratePointerCapsTriangles(triangles);

		return triangles.ToArray();
	}

	static void GeneratePointerSideTriangles(List<int> triangles)
	{
		for (var i = 1; i < k_Sides; i++)
		{
			var lowerLeft = i - 1;
			var lowerRight = i;
			var upperLeft = i + k_Sides - 1;
			var upperRight = i + k_Sides;

			var sideTriangles = VerticesToPolygon(upperRight, upperLeft, lowerRight, lowerLeft);
			triangles.AddRange(sideTriangles);
		}

		// Finish the side with a polygon that loops around from the end to the start vertices.
		int[] finishTriangles = VerticesToPolygon(k_Sides, k_Sides * 2 - 1, 0, k_Sides - 1);
		triangles.AddRange(finishTriangles);
	}

	static void GeneratePointerCapsTriangles(List<int> triangles)
	{
		// Generate the bottom circle cap.
		for (var i = 1; i < k_Sides; i++)
		{
			var lowerLeft = i - 1;
			var lowerRight = i;
			const int upperLeft = k_Sides * 2;
			
			triangles.Add(upperLeft);
			triangles.Add(lowerRight);
			triangles.Add(lowerLeft);
		}

		// Close the bottom circle cap with a start-end loop triangle.
		triangles.Add(k_Sides * 2);
		triangles.Add(0);
		triangles.Add(k_Sides - 1);

		// Generate the top circle cap.
		for (var i = k_Sides + 1; i < k_Sides * 2; i++)
		{
			var lowerLeft = i - 1;
			var lowerRight = i;
			const int upperLeft = k_Sides * 2 + 1;

			triangles.Add(lowerLeft);
			triangles.Add(lowerRight);
			triangles.Add(upperLeft);
		}

		// Close the top circle cap with a start-end loop triangle.
		triangles.Add(k_Sides * 2 - 1);
		triangles.Add(k_Sides);
		triangles.Add(k_Sides * 2 + 1);
	}

	bool SetupAnnotation()
	{
		m_IsValidStroke = m_CustomPointerObject.activeSelf;
		if (!m_IsValidStroke)
			return false;

		SetupHolder();

		m_Points.Clear();
		m_Forwards.Clear();
		m_Widths.Clear();
		m_Rights.Clear();

		var go = new GameObject("Annotation " + m_AnnotationHolder.childCount);
		m_UndoList.Add(go);

		var goTrans = go.transform;
		goTrans.SetParent(m_AnnotationHolder);
		goTrans.position = rayOrigin.position;

		m_CurrentMeshFilter = go.AddComponent<MeshFilter>();
		var mRenderer = go.AddComponent<MeshRenderer>();

		var matToUse = Instantiate(m_AnnotationMaterial);
		matToUse.SetColor("_EmissionColor", m_ColorToUse);
		mRenderer.sharedMaterial = matToUse;

		m_WorldToLocalMesh = goTrans.worldToLocalMatrix;

		m_CurrentMesh = new Mesh();
		m_CurrentMesh.name = "Annotation";

		return true;
	}

	void SetupHolder()
	{
		var mainHolder = GameObject.Find("Annotations") ?? new GameObject("Annotations");
		var mainHolderTrans = mainHolder.transform;

		var newSession = GetNewSessionHolder(mainHolderTrans);
		if (!newSession)
			newSession = new GameObject("Group " + mainHolderTrans.childCount);

		m_AnnotationHolder = newSession.transform;
		m_AnnotationHolder.SetParent(mainHolder.transform);
	}

	GameObject GetNewSessionHolder(Transform mainHolderTrans)
	{
		const float kGroupingDistance = .3f;
		GameObject newSession = null;

		for (var i = 0; i < mainHolderTrans.childCount; i++)
		{
			var child = mainHolderTrans.GetChild(i);
			child.name = "Group " + i;

			if (newSession == null)
			{
				var renderers = child.GetComponentsInChildren<MeshRenderer>();
				if (renderers.Length > 0)
				{
					var bound = renderers[0].bounds;
					for (var r = 1; r < renderers.Length; r++)
						bound.Encapsulate(renderers[r].bounds);

					if (bound.Contains(rayOrigin.position))
						newSession = child.gameObject;
					else if (bound.SqrDistance(rayOrigin.position) < kGroupingDistance)
						newSession = child.gameObject;

					if (newSession)
						break;
				}
			}
		}

		return newSession;
	}
	
	void UpdateAnnotation()
	{
		var rayForward = rayOrigin.forward;
		var rayRight = rayOrigin.right;
		var worldPoint = rayOrigin.position + rayForward * k_TipDistance;
		var localPoint = m_WorldToLocalMesh.MultiplyPoint3x4(worldPoint);

		if (m_Points.Count > 0)
		{
			var lastPoint = m_Points.Last();
			var velocity = (localPoint - lastPoint) / Time.unscaledDeltaTime;
			if (velocity.magnitude < m_CurrentRadius)
				return;
		}

		InterpolatePointsIfNeeded(localPoint, rayForward, rayRight);
		
		m_Points.Add(localPoint);
		m_Forwards.Add(rayForward);
		m_Widths.Add(m_CurrentRadius);
		m_Rights.Add(rayRight);

		PointsToMesh();
	}

	void InterpolatePointsIfNeeded(Vector3 localPoint, Vector3 rayForward, Vector3 rayRight)
	{
		if (m_Points.Count > 1)
		{
			var lastPoint = m_Points.Last();
			var distance = Vector3.Distance(lastPoint, localPoint);

			if (distance > m_CurrentRadius * .5f)
			{
				var halfPoint = (lastPoint + localPoint) / 2f;
				m_Points.Add(halfPoint);

				var halfForward = (m_Forwards.Last() + rayForward) / 2f;
				m_Forwards.Add(halfForward);

				var halfRadius = (m_Widths.Last() + m_CurrentRadius) / 2f;
				m_Widths.Add(halfRadius);

				var halfRight = (m_Rights.Last() + rayRight) / 2f;
				m_Rights.Add(halfRight);
			}
		}
	}
	
	void PointsToMesh()
	{
		if (m_Points.Count < 4)
			return;

		if (m_CurrentMesh == null)
			m_CurrentMesh = new Mesh();

		var newVertices = new List<Vector3>();
		var newTriangles = new List<int>();
		var newUvs = new List<Vector2>();

		LineToPlane(newVertices);
		SmoothPlane(newVertices);

		newVertices.RemoveRange(0, Mathf.Min(newVertices.Count, 4));
		if (newVertices.Count > 4)
			newVertices.RemoveRange(newVertices.Count - 4, 4);

		TriangulatePlane(newTriangles, newVertices.Count);
		CalculateUvs(newUvs, newVertices);
		
		m_CurrentMesh.Clear();

		m_CurrentMesh.vertices = newVertices.ToArray();
		m_CurrentMesh.triangles = newTriangles.ToArray();
		m_CurrentMesh.uv = newUvs.ToArray();

		m_CurrentMesh.UploadMeshData(false);

		m_CurrentMeshFilter.sharedMesh = m_CurrentMesh;
	}

	void LineToPlane(List<Vector3> newVertices)
	{
		var prevDirection = (m_Points[1] - m_Points[0]).normalized;

		for (var i = 1; i < m_Points.Count; i++)
		{
			var nextPoint = m_Points[i];
			var thisPoint = m_Points[i - 1];
			var direction = (nextPoint - thisPoint).normalized;

			// For optimization, ignore inner points of an almost straight line.
			// The last point is an exception, it is required for a smooth drawing experience.
			if (Vector3.Angle(prevDirection, direction) < 1f && i < m_Points.Count - 1 && i > 1)
				continue;

			var ratio = Mathf.Abs(Vector3.Dot(direction, m_Forwards[i - 1]));
			var cross1 = m_Rights[i - 1].normalized;
			var cross2 = Vector3.Cross(direction, m_Forwards[i - 1]).normalized;
			var cross = Vector3.Lerp(cross1, cross2, 1 - ratio).normalized;

			var lowWidth = Mathf.Min((newVertices.Count / 2) * 0.1f, 1);
			var highWidth = Mathf.Min((m_Points.Count - (i + 3)) * 0.25f, 1);
			var unclampedWidth = m_Widths[i - 1] * Mathf.Clamp01(i < m_Points.Count / 2f ? lowWidth : highWidth);
			var width = Mathf.Clamp(unclampedWidth, k_TopMinRadius, k_TopMaxRadius);

			var left = thisPoint - cross * width;
			var right = thisPoint + cross * width;

			newVertices.Add(left);
			newVertices.Add(right);

			prevDirection = direction;
		}
	}
	
	static void SmoothPlane(List<Vector3> newVertices)
	{
		const float kSmoothRatio = 0.75f;
		for (var side = 0; side < 2; side++)
		{
			for (var i = 4; i < newVertices.Count - 4 - side; i++)
			{
				var average = (newVertices[i - 4 + side] + newVertices[i - 2 + side] + newVertices[i + 2 + side] + newVertices[i + 4 + side]) / 4f;
				var dynamicSmooth = 1 / Vector3.Distance(newVertices[i + side], average);
				newVertices[i + side] = Vector3.Lerp(newVertices[i + side], average, kSmoothRatio * dynamicSmooth);
			}
		}
	}

	static void TriangulatePlane(List<int> newTriangles, int vertexCount)
	{
		for (var i = 3; i < vertexCount; i += 2)
		{
			var upperLeft = i - 1;
			var upperRight = i;
			var lowerLeft = i - 3;
			var lowerRight = i - 2;

			var triangles = VerticesToPolygon(upperLeft, upperRight, lowerLeft, lowerRight);
			newTriangles.AddRange(triangles);
		}
	}
	
	static void CalculateUvs(List<Vector2> newUvs, List<Vector3> newVertices)
	{
		for (var i = 0; i < newVertices.Count; i += 2)
		{
			newUvs.Add(new Vector2(0, i * 0.5f));
			newUvs.Add(new Vector2(1, i * 0.5f));
		}
	}

	void FinalizeMesh()
	{
		m_IsValidStroke = false;

		CenterMesh();

		m_CurrentMesh.RecalculateBounds();
		m_CurrentMesh.RecalculateNormals();

		m_CurrentMesh.UploadMeshData(true);

		CenterHolder();
	}

	void CenterMesh()
	{
		if (m_CurrentMesh == null || m_CurrentMesh.vertexCount == 0)
			return;

		var center = Vector3.zero;
		
		var vertices = m_CurrentMesh.vertices;

		for (var i = 0; i < m_CurrentMesh.vertexCount; i++)
		{
			center += vertices[i];
		}

		center /= m_CurrentMesh.vertexCount;

		for (var i = 0; i < m_CurrentMesh.vertexCount; i++)
		{
			vertices[i] -= center;
		}

		m_CurrentMesh.vertices = vertices;
		m_CurrentMeshFilter.transform.localPosition += center;
	}

	void CenterHolder()
	{
		if (m_AnnotationHolder == null || m_AnnotationHolder.childCount == 0)
			return;

		var childWorldPositions = new List<Vector3>();
		var center = Vector3.zero;

		for (var i = 0; i < m_AnnotationHolder.childCount; i++)
		{
			var worldPos = m_AnnotationHolder.GetChild(i).position;
			childWorldPositions.Add(worldPos);
			center += worldPos;
		}

		center /= m_AnnotationHolder.childCount;

		m_AnnotationHolder.localPosition += center;
		for (var i = 0; i < m_AnnotationHolder.childCount; i++)
		{
			m_AnnotationHolder.GetChild(i).position = childWorldPositions[i];
		}
	}

	static int[] VerticesToPolygon(int upperLeft, int upperRight, int lowerLeft, int lowerRight, bool doubleSided = false)
	{
		var triangleCount = doubleSided ? 12 : 6;
		var triangles = new int[triangleCount];
		var index = 0;

		triangles[index++] = upperLeft;
		triangles[index++] = lowerRight;
		triangles[index++] = lowerLeft;

		triangles[index++] = lowerRight;
		triangles[index++] = upperLeft;
		triangles[index++] = upperRight;

		if (doubleSided)
		{
			triangles[index++] = lowerLeft;
			triangles[index++] = lowerRight;
			triangles[index++] = upperLeft;

			triangles[index++] = upperRight;
			triangles[index++] = upperLeft;
			triangles[index] = lowerRight;
		}

		return triangles;
	}
	
	public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
	{
		var annotationInput = (AnnotationInput)input;

		if (annotationInput.draw.wasJustPressed)
		{
			if (SetupAnnotation())
				consumeControl(annotationInput.draw);
		}
		else if (m_IsValidStroke)
		{
			consumeControl(annotationInput.draw);

			if (annotationInput.draw.isHeld)
				UpdateAnnotation();
			else if (annotationInput.draw.wasJustReleased)
				FinalizeMesh();
		}
		else if (annotationInput.undo.wasJustPressed)
		{
			consumeControl(annotationInput.undo);
			UndoLast();
		}

		if (!Mathf.Approximately(annotationInput.changeBrushSize.value, 0))
		{
			HandleBrushSize(annotationInput.changeBrushSize.value);
			consumeControl(annotationInput.changeBrushSize);
			consumeControl(annotationInput.vertical);
		}
	}
}
