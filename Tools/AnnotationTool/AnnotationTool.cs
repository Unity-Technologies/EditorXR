#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR;

[MainMenuItem("Annotation", "Create", "Draw in 3D")]
public class AnnotationTool : MonoBehaviour, ITool, ICustomActionMap, IUsesRayOrigin, IRayVisibilitySettings,
	IUsesRayOrigins, IInstantiateUI, IUsesMenuOrigins, IUsesCustomMenuOrigins, IUsesViewerScale, IUsesSpatialHash,
	IIsHoveringOverUI
{
	public const float TipDistance = 0.05f;
	public const float MinBrushSize = 0.0025f;
	public const float MaxBrushSize = 0.05f;
	const float k_MinDistance = 0.003f;

	[SerializeField]
	ActionMap m_ActionMap;

	[SerializeField]
	Material m_AnnotationMaterial;

	[SerializeField]
	GameObject m_BrushSizePrefab;

	[SerializeField]
	GameObject m_ColorPickerActivatorPrefab;

	Action<float> onBrushSizeChanged { set; get; }

	const int k_InitialListSize = 1024; // Pre-allocate lists to avoid GC

	List<Vector3> m_Points = new List<Vector3>(k_InitialListSize);
	List<Vector3> m_UpVectors = new List<Vector3>(k_InitialListSize);
	List<float> m_Widths = new List<float>(k_InitialListSize);
	float m_Length;

	MeshFilter m_CurrentMeshFilter;
	Color m_ColorToUse = Color.white;
	Mesh m_CurrentMesh;
	Matrix4x4 m_WorldToLocalMesh;

	ColorPickerUI m_ColorPicker;
	BrushSizeUI m_BrushSizeUI;

	Transform m_AnnotationHolder;

	AnnotationPointer m_AnnotationPointer;
	Vector3 m_OriginalAnnotationPointerLocalScale;
	Coroutine m_AnnotationPointerVisibilityCoroutine;
	bool m_WasOverUI;

	GameObject m_ColorPickerActivator;

	float m_BrushSize = MinBrushSize;

	public Transform rayOrigin { private get; set; }
	public List<Transform> otherRayOrigins { private get; set; }

	public Transform menuOrigin { private get; set; }
	public Transform alternateMenuOrigin { private get; set; }

	public Func<Transform, Transform> customMenuOrigin { private get; set; }
	public Func<Transform, Transform> customAlternateMenuOrigin { private get; set; }

	public ActionMap actionMap
	{
		get { return m_ActionMap; }
	}

	void OnDestroy()
	{
		if (rayOrigin)
			this.RemoveRayVisibilitySettings(rayOrigin, this);

		if (m_ColorPicker)
			ObjectUtils.Destroy(m_ColorPicker.gameObject);
		if (m_BrushSizeUI)
			ObjectUtils.Destroy(m_BrushSizeUI.gameObject);
		if (m_ColorPickerActivator)
			ObjectUtils.Destroy(m_ColorPickerActivator);

		if (m_AnnotationPointer)
			ObjectUtils.Destroy(m_AnnotationPointer.gameObject);
	}

	void Start()
	{
		this.AddRayVisibilitySettings(rayOrigin, this, false, false);

		m_AnnotationPointer = ObjectUtils.CreateGameObjectWithComponent<AnnotationPointer>(rayOrigin, false);
		m_OriginalAnnotationPointerLocalScale = m_AnnotationPointer.transform.localScale;
		CheckBrushSizeUI();

		if (m_ColorPickerActivator == null)
		{
			m_ColorPickerActivator = this.InstantiateUI(m_ColorPickerActivatorPrefab);
			var otherAltMenu = customAlternateMenuOrigin(otherRayOrigins[0]);

			m_ColorPickerActivator.transform.SetParent(otherAltMenu.GetComponentInChildren<MainMenuActivator>().transform);
			m_ColorPickerActivator.transform.localRotation = Quaternion.identity;
			m_ColorPickerActivator.transform.localPosition = Vector3.right * 0.05f;
			m_ColorPickerActivator.transform.localScale = Vector3.one;

			var activator = m_ColorPickerActivator.GetComponentInChildren<ColorPickerActivator>();

			m_ColorPicker = activator.GetComponentInChildren<ColorPickerUI>(true);
			m_ColorPicker.onHideCalled = HideColorPicker;
			m_ColorPicker.toolRayOrigin = rayOrigin;
			m_ColorPicker.onColorPicked = OnColorPickerValueChanged;

			activator.rayOrigin = otherRayOrigins.First();
			activator.showColorPicker = ShowColorPicker;
			activator.hideColorPicker = HideColorPicker;

			activator.undoButtonClick += Undo.PerformUndo;
			activator.redoButtonClick += Undo.PerformRedo;
		}
	}

	void CheckBrushSizeUI()
	{
		if (m_BrushSizeUI == null)
		{
			var brushSizeUi = this.InstantiateUI(m_BrushSizePrefab);
			m_BrushSizeUI = brushSizeUi.GetComponent<BrushSizeUI>();

			var trans = brushSizeUi.transform;
			var scale = brushSizeUi.transform.localScale;
			trans.SetParent(alternateMenuOrigin, false);
			trans.localPosition = Vector3.zero;
			trans.localRotation = Quaternion.Euler(-90, 0, 0);
			trans.localScale = scale;

			m_BrushSizeUI.onValueChanged = (val) => 
			{
				m_BrushSize = Mathf.Lerp(MinBrushSize, MaxBrushSize, val);
				m_AnnotationPointer.Resize(m_BrushSize);
			};
			onBrushSizeChanged = m_BrushSizeUI.ChangeSliderValue;
		}
	}

	void ShowColorPicker(Transform otherRayOrigin)
	{
		if (!m_ColorPicker.enabled)
			m_ColorPicker.Show();

		m_AnnotationPointer.gameObject.SetActive(false);
	}

	void HideColorPicker()
	{
		if (m_ColorPicker && m_ColorPicker.enabled)
		{
			m_ColorPicker.Hide();
			m_AnnotationPointer.gameObject.SetActive(true);
		}
	}

	void OnColorPickerValueChanged(Color color)
	{
		m_ColorToUse = color;

		color.a = .75f;
		m_AnnotationPointer.SetColor(color);

		m_BrushSizeUI.OnBrushColorChanged(color);
	}

	void HandleBrushSize(float value)
	{
		if (m_AnnotationPointer != null)
		{
			if (VRSettings.loadedDeviceName == "OpenVR") // For vive controllers, use 1:1 touchpad setting.
			{
				m_BrushSize = Mathf.Lerp(MinBrushSize, MaxBrushSize, (value + 1) / 2f);
			}
			else // For touch and hydra, let the thumbstick gradually modify the width.
			{
				m_BrushSize += value * Time.unscaledDeltaTime * .1f;
				m_BrushSize = Mathf.Clamp(m_BrushSize, MinBrushSize, MaxBrushSize);
			}

			if (m_BrushSizeUI && onBrushSizeChanged != null)
			{
				var ratio = Mathf.InverseLerp(MinBrushSize, MaxBrushSize, m_BrushSize);
				onBrushSizeChanged(ratio);
			}

			m_AnnotationPointer.Resize(m_BrushSize);
		}
	}

	void SetupAnnotation()
	{
		SetupHolder();

		m_Points.Clear();
		m_UpVectors.Clear();
		m_Widths.Clear();
		m_Length = 0;

		var go = new GameObject("Annotation " + m_AnnotationHolder.childCount);

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
		var upVector = rayOrigin.up;
		var viewerScale = this.GetViewerScale();
		var worldPoint = rayOrigin.position + rayOrigin.forward * TipDistance * viewerScale;
		var localPoint = m_WorldToLocalMesh.MultiplyPoint3x4(worldPoint);

		if (m_Points.Count > 0)
		{
			var lastPoint = m_Points.Last();
			localPoint = Vector3.Lerp(lastPoint, localPoint, 0.5f);
			var distance = (localPoint - lastPoint).magnitude;
			if (distance < k_MinDistance * viewerScale)
				return;

			m_Length += distance;
		}

		var brushSize = m_BrushSize * viewerScale;
		InterpolatePointsIfNeeded(localPoint, upVector, brushSize);
		
		m_Points.Add(localPoint);
		m_UpVectors.Add(upVector);
		m_Widths.Add(brushSize);

		PointsToMesh();
	}

	void InterpolatePointsIfNeeded(Vector3 localPoint, Vector3 upVector, float brushSize)
	{
		if (m_Points.Count > 1)
		{
			var lastPoint = m_Points.Last();
			var distance = Vector3.Distance(lastPoint, localPoint);

			if (distance > brushSize * .5f)
			{
				var halfPoint = (lastPoint + localPoint) / 2f;
				m_Points.Add(halfPoint);

				var halfUp = (m_UpVectors.Last() + upVector) / 2f;
				m_UpVectors.Add(halfUp);

				var halfRadius = (m_Widths.Last() + brushSize) / 2f;
				m_Widths.Add(halfRadius);
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
		var distance = 0f;
		var lastPoint = m_Points[0];
		for (var i = 1; i < m_Points.Count; i++)
		{
			var point = m_Points[i];
			var segment = point - lastPoint;

			var width = m_Widths[i];

			width *= Math.Min(Mathf.Sqrt(distance / width), 1);
			var endDistance = m_Length - distance;
			width *= Math.Min(Mathf.Sqrt(endDistance / width), 1);

			var upVector = m_UpVectors[i];
			var top = point - upVector * width;
			var bottom = point + upVector * width;

			newVertices.Add(top);
			newVertices.Add(bottom);

			distance += segment.magnitude;
			lastPoint = point;
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
		CenterMesh();

		m_CurrentMesh.RecalculateBounds();
		m_CurrentMesh.RecalculateNormals();

		m_CurrentMesh.UploadMeshData(true);

		CenterHolder();

		var go = m_CurrentMeshFilter.gameObject;

		this.AddToSpatialHash(go);

		Undo.IncrementCurrentGroup();
		Undo.RegisterCreatedObjectUndo(go, "Create Annotation");
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

	public static int[] VerticesToPolygon(int upperLeft, int upperRight, int lowerLeft, int lowerRight, bool doubleSided = true)
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

		if (!Mathf.Approximately(annotationInput.changeBrushSize.value, 0))
		{
			HandleBrushSize(annotationInput.changeBrushSize.value);
			consumeControl(annotationInput.changeBrushSize);
			consumeControl(annotationInput.vertical);
		}

		if (annotationInput.draw.wasJustPressed)
		{
			SetupAnnotation();
			consumeControl(annotationInput.draw);
		}

		var isHeld = annotationInput.draw.isHeld;
		if (isHeld)
		{
			UpdateAnnotation();
			consumeControl(annotationInput.draw);
		}

		if (annotationInput.draw.wasJustReleased)
		{
			FinalizeMesh();
			consumeControl(annotationInput.draw);
		}

		if (isHeld)
			return;

		var isOverUI = this.IsHoveringOverUI(rayOrigin);
		if (isOverUI != m_WasOverUI)
		{
			this.RestartCoroutine(ref m_AnnotationPointerVisibilityCoroutine, SetAnnotationPointerVisibility(m_WasOverUI));
			if (m_WasOverUI)
				this.AddRayVisibilitySettings(rayOrigin, this, false, false);
			else
				this.RemoveRayVisibilitySettings(rayOrigin, this);

			m_WasOverUI = isOverUI;
		}
	}

	IEnumerator SetAnnotationPointerVisibility(bool visible)
	{
		if (!m_AnnotationPointer)
			yield break;

		const float transitionTime = 0.1875f;
		var annotationPointerTransform = m_AnnotationPointer.transform;
		var startTime = Time.time;
		var timeDiff = 0f;
		var currentScale = annotationPointerTransform.localScale;
		var targetScale = visible ? m_OriginalAnnotationPointerLocalScale : Vector3.zero;
		while (timeDiff < transitionTime)
		{
			annotationPointerTransform.localScale = Vector3.Lerp(currentScale, targetScale, timeDiff / transitionTime);
			timeDiff = Time.time - startTime;
			yield return null;
		}

		annotationPointerTransform.localScale = targetScale;
	}
}
#endif
