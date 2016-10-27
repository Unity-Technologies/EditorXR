using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Helpers;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Extensions;
using UnityObject = UnityEngine.Object;

public class AssetGridItem : DraggableListItem<AssetData>, IPlaceObjects, ISpatialHash
{
	private const float kPreviewDuration = 0.1f;

	private const float kRotateSpeed = 50f;

	[SerializeField]
	private Text m_Text;

	[SerializeField]
	private BaseHandle m_Handle;

	[SerializeField]
	private Image m_TextPanel;

	[SerializeField]
	private Renderer m_Cube;

	[SerializeField]
	private Renderer m_Sphere;

	[HideInInspector]
	[SerializeField] // Serialized so that this remains set after cloning
	private GameObject m_Icon;

	private GameObject m_IconPrefab;

	[HideInInspector]
	[SerializeField] // Serialized so that this remains set after cloning
	private Transform m_PreviewObject;

	private bool m_Setup;
	private float m_PreviewFade;
	private Vector3 m_PreviewPrefabScale;
	private Vector3 m_PreviewTargetScale;

	private Coroutine m_TransitionCoroutine;

	private Material m_TextureMaterial;

	public Action<UnityObject> addObjectToSpatialHash { get; set; }
	public Action<UnityObject> removeObjectFromSpatialHash { get; set; }

	public GameObject icon
	{
		private get
		{
			if (m_Icon)
				return m_Icon;
			return m_Cube.gameObject;
		}
		set
		{
			m_Cube.gameObject.SetActive(false);
			m_Sphere.gameObject.SetActive(false);

			if (m_IconPrefab == value)
			{
				m_Icon.SetActive(true);
				return;
			}

			if (m_Icon)
				U.Object.Destroy(m_Icon);

			m_IconPrefab = value;
			m_Icon = U.Object.Instantiate(m_IconPrefab, transform, false);
			m_Icon.transform.localPosition = Vector3.up * 0.5f;
			m_Icon.transform.localRotation = Quaternion.AngleAxis(90, Vector3.down);
			m_Icon.transform.localScale = Vector3.one;
		}
	}

	public Material material
	{
		set
		{
			m_Sphere.sharedMaterial = value;
			m_Sphere.gameObject.SetActive(true);
			m_Cube.gameObject.SetActive(false);
			if (m_Icon)
				m_Icon.gameObject.SetActive(false);
		}
	}

	public Texture texture
	{
		set
		{
			m_Sphere.gameObject.SetActive(true);
			m_Cube.gameObject.SetActive(false);
			if (m_Icon)
				m_Icon.gameObject.SetActive(false);
			if (!value)
			{
				m_Sphere.sharedMaterial.mainTexture = null;
				return;
			}
			if (m_TextureMaterial)
				U.Object.Destroy(m_TextureMaterial);

			m_TextureMaterial = new Material(Shader.Find("Standard")) { mainTexture = value };
			m_Sphere.sharedMaterial = m_TextureMaterial;
		}
	}

	public Texture fallbackTexture
	{
		set
		{
			if (value)
				value.wrapMode = TextureWrapMode.Clamp;

			m_Cube.sharedMaterial.mainTexture = value;
			m_Cube.gameObject.SetActive(true);
			m_Sphere.gameObject.SetActive(false);

			if (m_Icon)
				m_Icon.gameObject.SetActive(false);
		}
	}

	public Action<Transform, Vector3> placeObject { private get; set; }

	public override void Setup(AssetData listData)
	{
		base.Setup(listData);

		// First time setup
		if (!m_Setup)
		{
			// Cube material might change, so we always instance it
			U.Material.GetMaterialClone(m_Cube);

			m_Handle.dragStarted += OnDragStarted;
			m_Handle.dragging += OnDragging;
			m_Handle.dragEnded += OnDragEnded;

			m_Handle.hoverStarted += OnHoverStarted;
			m_Handle.hoverEnded += OnHoverEnded;

			m_Handle.getDropObject += GetDropObject;

			m_Setup = true;
		}

		InstantiatePreview();

		m_Text.text = listData.name;

		// HACK: We need to kick the canvasRenderer to update the mesh properly
		m_Text.gameObject.SetActive(false);
		m_Text.gameObject.SetActive(true);

		m_PreviewFade = 0;
	}

	public void UpdateTransforms(float scale)
	{
		transform.localScale = Vector3.one * scale;

		m_TextPanel.transform.localRotation = U.Camera.LocalRotateTowardCamera(transform.parent.rotation);

		// Handle preview fade
		if (m_PreviewObject)
		{
			if (m_PreviewFade == 0)
			{
				m_PreviewObject.gameObject.SetActive(false);
				icon.SetActive(true);
				icon.transform.localScale = Vector3.one;
			}
			else if (m_PreviewFade == 1)
			{
				m_PreviewObject.gameObject.SetActive(true);
				icon.SetActive(false);
				m_PreviewObject.transform.localScale = m_PreviewTargetScale;
			}
			else
			{
				icon.SetActive(true);
				m_PreviewObject.gameObject.SetActive(true);
				icon.transform.localScale = Vector3.one * (1 - m_PreviewFade);
				m_PreviewObject.transform.localScale = Vector3.Lerp(Vector3.zero, m_PreviewTargetScale, m_PreviewFade);
			}
		}

		if (m_Sphere.gameObject.activeInHierarchy)
			m_Sphere.transform.Rotate(Vector3.up, kRotateSpeed * Time.unscaledDeltaTime, Space.Self);

		if (data.type == "Scene")
		{
			icon.transform.rotation =
				Quaternion.LookRotation(icon.transform.position - U.Camera.GetMainCamera().transform.position, Vector3.up);
		}
	}

	private void InstantiatePreview()
	{
		if (m_PreviewObject)
			U.Object.Destroy(m_PreviewObject.gameObject);
		if (!data.preview)
			return;

		m_PreviewObject = Instantiate(data.preview).transform;
		m_PreviewObject.position = Vector3.zero;
		m_PreviewObject.rotation = Quaternion.identity;

		m_PreviewPrefabScale = m_PreviewObject.localScale;

		// Normalize total scale to 1
		var previewTotalBounds = U.Object.GetTotalBounds(m_PreviewObject);

		// Don't show a preview if there are no renderers
		if (previewTotalBounds == null)
		{
			U.Object.Destroy(m_PreviewObject.gameObject);
			return;
		}

		m_PreviewObject.SetParent(transform, false);

		m_PreviewTargetScale = m_PreviewPrefabScale * (1 / previewTotalBounds.Value.size.MaxComponent());
		m_PreviewObject.localPosition = Vector3.up * 0.5f;

		m_PreviewObject.gameObject.SetActive(false);
		m_PreviewObject.localScale = Vector3.zero;
	}

	protected override void OnDragStarted(BaseHandle baseHandle, HandleEventData eventData)
	{
		base.OnDragStarted(baseHandle, eventData);

		var clone = (GameObject)Instantiate(gameObject, transform.position, transform.rotation, transform.parent);
		var cloneItem = clone.GetComponent<AssetGridItem>();

		if (cloneItem.m_PreviewObject)
		{
			cloneItem.m_Cube.gameObject.SetActive(false);
			if (cloneItem.m_Icon)
				cloneItem.m_Icon.gameObject.SetActive(false);
			cloneItem.m_PreviewObject.gameObject.SetActive(true);
			cloneItem.m_PreviewObject.transform.localScale = m_PreviewTargetScale;

			// Destroy label
			U.Object.Destroy(cloneItem.m_TextPanel.gameObject);
		}

		m_DragObject = clone.transform;

		// Disable any SmoothMotion that may be applied to a cloned Asset Grid Item now referencing input device p/r/s
		var smoothMotion = clone.GetComponent<SmoothMotion>();
		if (smoothMotion != null)
			smoothMotion.enabled = false;

		StartCoroutine(AnimateToPreviewScale());
	}

	protected override void OnDragEnded(BaseHandle baseHandle, HandleEventData eventData)
	{
		var gridItem = m_DragObject.GetComponent<AssetGridItem>();

		if (gridItem.m_PreviewObject)
		{
			addObjectToSpatialHash(gridItem.m_PreviewObject);
			placeObject(gridItem.m_PreviewObject, m_PreviewPrefabScale);
		}
		else
		{
			switch (data.type)
			{
				case "Prefab":
					var prefab = U.Object.Instantiate((GameObject)data.asset, null, false, false);
					prefab.transform.position = gridItem.transform.position;
					prefab.transform.rotation = gridItem.transform.rotation;
					break;
				case "Model":
					var model = U.Object.Instantiate((GameObject)data.asset, null, false, false);
					model.transform.position = gridItem.transform.position;
					model.transform.rotation = gridItem.transform.rotation;
					break;
			}
		}

		StartCoroutine(AnimatedHide(m_DragObject.gameObject, gridItem.m_Cube, eventData.rayOrigin));
	}

	private void OnHoverStarted(BaseHandle baseHandle, HandleEventData eventData)
	{
		if (gameObject.activeInHierarchy)
		{
			this.StopCoroutine(ref m_TransitionCoroutine);
			m_TransitionCoroutine = StartCoroutine(AnimatePreview(false));
		}
	}

	private void OnHoverEnded(BaseHandle baseHandle, HandleEventData eventData)
	{
		if (gameObject.activeInHierarchy)
		{
			this.StopCoroutine(ref m_TransitionCoroutine);
			m_TransitionCoroutine = StartCoroutine(AnimatePreview(true));
		}
	}

	private IEnumerator AnimatePreview(bool @out)
	{
		var startVal = 0;
		var endVal = 1;
		if (@out)
		{
			startVal = 1;
			endVal = 0;
		}
		var startTime = Time.realtimeSinceStartup;
		while (Time.realtimeSinceStartup - startTime < kPreviewDuration)
		{
			m_PreviewFade = Mathf.Lerp(startVal, endVal, (Time.realtimeSinceStartup - startTime) / kPreviewDuration);
			yield return null;
		}
		m_PreviewFade = endVal;
	}

	object GetDropObject(BaseHandle handle)
	{
		return data.asset;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_Cube.sharedMaterial);
	}

	/// <summary>
	/// Animate the LocalScale of the asset towards a common/unified scale
	/// used when the asset is magnetized/attached to the proxy, after grabbing it from the asset grid
	/// </summary>
	IEnumerator AnimateToPreviewScale()
	{
		var currentLocalScale = m_DragObject.localScale;
		const float smallerLocalScaleMultiplier = 0.125f;
		var targetLocalScale = Vector3.one * smallerLocalScaleMultiplier;
		var currentTime = 0f;
		var currentVelocity = 0f;
		const float kDuration = 1f;
		while (currentTime < kDuration - 0.05f)
		{
			if (m_DragObject == null)
				yield break; // Exit coroutine if m_GrabbedObject is destroyed before the loop is finished

			currentTime = U.Math.SmoothDamp(currentTime, kDuration, ref currentVelocity, 0.5f, Mathf.Infinity, Time.unscaledDeltaTime);
			m_DragObject.localScale = Vector3.Lerp(currentLocalScale, targetLocalScale, currentTime);
			yield return null;
		}
	}

	IEnumerator AnimatedHide(GameObject itemToHide, Renderer cubeRenderer, Transform rayOrigin)
	{
		var itemTransform = itemToHide.transform;
		var currentScale = itemTransform.localScale;
		var targetScale = Vector3.zero;
		var transitionAmount = Time.unscaledDeltaTime;
		var transitionAddMultiplier = 6;
		while (transitionAmount < 1)
		{
			itemTransform.localScale = Vector3.Lerp(currentScale, targetScale, transitionAmount);
			transitionAmount += Time.unscaledDeltaTime * transitionAddMultiplier;
			yield return null;
		}

		cubeRenderer.sharedMaterial = null; // Drop material so it won't be destroyed (shared with cube in list)
		U.Object.Destroy(itemToHide);
	}
}