using System.Collections;
using System.Collections.Generic;
using System.IO;
using ListView;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;
using Object = UnityEngine.Object;

public class AssetGridItem : ListViewItem<AssetData>
{
	private const float kMagnetizeDuration = 0.5f;
	private const float kPreviewDuration = 0.1f;
	private const float kGrowDuration = 0.5f;

	private const float kRotateSpeed = 50f;

	private const float kInstantiateFOVDifference = 20f;

	//TODO: replace with a GrabOrigin transform once menu PR lands
	private readonly Vector3 kGrabPositionOffset = new Vector3(0f, 0.02f, 0.03f);
	private readonly Quaternion kGrabRotationOffset = Quaternion.AngleAxis(30f, Vector3.left);

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
	private Transform m_PreviewObject;

	private Transform m_Icon;

	private bool m_Setup;
	private Transform m_GrabbedObject;
	private float m_GrabLerp;
	private float m_PreviewFade;
	private Vector3 m_PreviewPrefabScale;
	private Vector3 m_PreviewTargetScale;
	private Bounds? m_PreviewTotalBounds;

	private Coroutine m_TransitionCoroutine;

	private Transform icon
	{
		get
		{
			if (m_Icon)
				return m_Icon;
			return m_Cube.transform;
		}
	}

	public override void Setup(AssetData listData)
	{
		base.Setup(listData);
		// First time setup
		if (!m_Setup)
		{
			// Cube material might change, so we always instance it
			U.Material.GetMaterialClone(m_Cube);

			m_Handle.handleDragging += GrabBegin;
			m_Handle.handleDrag += GrabDrag;
			m_Handle.handleDragged += GrabEnd;

			m_Handle.hovering += OnBeginHover;
			m_Handle.hovered += OnEndHover;

			m_Setup = true;
		}

		InstantiatePreview();

		m_Text.text = listData.name;
		m_PreviewFade = 0;
	}

	public void UpdateTransforms(float scale)
	{
		transform.localScale = Vector3.one * scale;

		var cameraTransform = U.Camera.GetMainCamera().transform;

		//Rotate text toward camera
		var eyeVector3 = Quaternion.Inverse(transform.parent.rotation) * cameraTransform.forward;
		eyeVector3.x = 0;
		if (Vector3.Dot(eyeVector3, Vector3.forward) > 0)
			m_TextPanel.transform.localRotation = Quaternion.LookRotation(eyeVector3, Vector3.up);
		else
			m_TextPanel.transform.localRotation = Quaternion.LookRotation(eyeVector3, Vector3.down);

		//Handle preview fade
		if (m_PreviewObject)
		{
			if (m_PreviewFade == 0)
			{
				m_PreviewObject.gameObject.SetActive(false);
				icon.gameObject.SetActive(true);
				icon.localScale = Vector3.one;
			}
			else if (m_PreviewFade == 1)
			{
				m_PreviewObject.gameObject.SetActive(true);
				icon.gameObject.SetActive(false);
				m_PreviewObject.transform.localScale = m_PreviewTargetScale;
			}
			else
			{
				icon.gameObject.SetActive(true);
				m_PreviewObject.gameObject.SetActive(true);
				icon.localScale = Vector3.one * (1 - m_PreviewFade);
				m_PreviewObject.transform.localScale = Vector3.Lerp(Vector3.zero, m_PreviewTargetScale, m_PreviewFade);
			}
		}

		if (m_Sphere.gameObject.activeInHierarchy)
			m_Sphere.transform.Rotate(Vector3.up, kRotateSpeed * Time.unscaledDeltaTime, Space.Self);
	}

	private void InstantiatePreview()
	{
		if(m_PreviewObject)
			U.Object.Destroy(m_PreviewObject.gameObject);
		if (!data.preview)
			return;

		m_PreviewObject = Instantiate(data.preview).transform;
		m_PreviewObject.position = Vector3.zero;
		m_PreviewObject.rotation = Quaternion.identity;

		m_PreviewPrefabScale = m_PreviewObject.localScale;

		//Normalize total scale to 1
		m_PreviewTotalBounds = GetTotalBounds(m_PreviewObject);

		//Don't show a preview if there are no renderers
		if (m_PreviewTotalBounds == null)
		{
			U.Object.Destroy(m_PreviewObject.gameObject);
			return;
		}

		m_PreviewObject.SetParent(transform, false);

		m_PreviewTargetScale = m_PreviewPrefabScale * (1 / m_PreviewTotalBounds.Value.size.Max());
		m_PreviewObject.localPosition = Vector3.up * 0.5f;

		m_PreviewObject.gameObject.SetActive(false);
		m_PreviewObject.localScale = Vector3.zero;
	}

	private void GrabBegin(BaseHandle baseHandle, HandleEventData eventData)
	{
		var clone = (GameObject) Instantiate(gameObject, transform.position, transform.rotation, transform.parent);
		var cloneItem = clone.GetComponent<AssetGridItem>();

		if (cloneItem.m_PreviewObject)
		{
			cloneItem.m_Cube.gameObject.SetActive(false);
			cloneItem.m_PreviewObject.gameObject.SetActive(true);
			cloneItem.m_PreviewObject.transform.localScale = m_PreviewTargetScale;
		}

		m_GrabbedObject = clone.transform;
		m_GrabLerp = 0;
		StartCoroutine(Magnetize());
	}

	private IEnumerator Magnetize()
	{
		var startTime = Time.realtimeSinceStartup;
		var currTime = 0f;
		while (currTime < kMagnetizeDuration)
		{
			currTime = Time.realtimeSinceStartup - startTime;
			m_GrabLerp = currTime / kMagnetizeDuration;
			yield return null;
		}
		m_GrabLerp = 1;
	}

	private void GrabDrag(BaseHandle baseHandle, HandleEventData eventData)
	{
		var rayTransform = eventData.rayOrigin.transform;
		m_GrabbedObject.transform.position = Vector3.Lerp(m_GrabbedObject.transform.position, rayTransform.position + rayTransform.rotation * kGrabPositionOffset, m_GrabLerp);
		m_GrabbedObject.transform.rotation = Quaternion.Lerp(m_GrabbedObject.transform.rotation, rayTransform.rotation * kGrabRotationOffset, m_GrabLerp);
	}

	private void GrabEnd(BaseHandle baseHandle, HandleEventData eventData)
	{
		var gridItem = m_GrabbedObject.GetComponent<AssetGridItem>();
		if (gridItem.m_PreviewObject)
			StartCoroutine(PlaceObjectWithBounds(gridItem.m_PreviewObject));
		else
		{
			switch (data.type)
			{
				case "Prefab":
					PlaceObject(gridItem.transform.position, gridItem.transform.rotation);
					break;
				case "Model":
					PlaceObject(gridItem.transform.position, gridItem.transform.rotation);
					break;
			}
		}
		gridItem.m_Cube.sharedMaterial = null; // Drop material so it won't be destroyed (shared with cube in list)
		U.Object.Destroy(m_GrabbedObject.gameObject);
	}

	private void PlaceObject(Vector3 position, Quaternion rotation)
	{
		Instantiate(data.GetAsset(), position, rotation);
	}
	private IEnumerator PlaceObjectWithBounds(Transform obj)
	{
		float start = Time.realtimeSinceStartup;
		var currTime = 0f;

		obj.parent = null;
		var startScale = obj.localScale;
		var startPosition = obj.position;

		//var destinationPosition = obj.position;
		var camera = U.Camera.GetMainCamera();
		var camPosition = camera.transform.position;
		var forward = obj.position - camPosition;
		forward.y = 0;
		var perspective = camera.fieldOfView * 0.5f + kInstantiateFOVDifference;
		var distance = m_PreviewTotalBounds.Value.size.magnitude / Mathf.Tan(perspective * Mathf.Deg2Rad);
		var destinationPosition = obj.position;
		if(distance > forward.magnitude)
			destinationPosition = camPosition + forward.normalized * distance;

		while (currTime < kGrowDuration)
		{
			currTime = Time.realtimeSinceStartup - start;
			var t = currTime / kGrowDuration;
			var tSquared = t * t;
			obj.localScale = Vector3.Lerp(startScale, m_PreviewPrefabScale, tSquared);
			obj.position = Vector3.Lerp(startPosition, destinationPosition, tSquared);
			yield return null;
		}
		obj.localScale = m_PreviewPrefabScale;
		Selection.activeGameObject = obj.gameObject;
	}

	private void OnBeginHover(BaseHandle baseHandle, HandleEventData eventData)
	{
		if (gameObject.activeInHierarchy)
		{
			if(m_TransitionCoroutine != null)
				StopCoroutine(m_TransitionCoroutine);
			m_TransitionCoroutine = StartCoroutine(AnimatePreview(false));
		}
	}

	private void OnEndHover(BaseHandle baseHandle, HandleEventData eventData)
	{
		if (gameObject.activeInHierarchy)
		{
			if (m_TransitionCoroutine != null)
				StopCoroutine(m_TransitionCoroutine);
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

	private void OnDestroy()
	{
		U.Object.Destroy(m_Cube.sharedMaterial);
	}

	private static Bounds? GetTotalBounds(Transform t)
	{
		Bounds? bounds = null;
		var renderers = t.GetComponentsInChildren<Renderer>(true);
		foreach (var renderer in renderers)
		{
			if (bounds == null)
				bounds = renderer.bounds;
			else
			{
				Bounds b = bounds.Value;
				b.Encapsulate(renderer.bounds);
				bounds = b;
			}
		}
		return bounds;
	}

	public void SetIcon(GameObject iconModel)
	{
		if(m_Icon)
			U.Object.Destroy(m_Icon.gameObject);
		if (iconModel)
		{
			m_Icon = U.Object.Instantiate(iconModel, transform, false).transform;
			m_Icon.localPosition = Vector3.up * 0.5f;
			m_Icon.localRotation = Quaternion.AngleAxis(90, Vector3.down);
			m_Icon.localScale = Vector3.one;
			m_Cube.gameObject.SetActive(false);
		}

		switch (data.type)
		{
			case "Material":
				m_Sphere.gameObject.SetActive(true);
				icon.gameObject.SetActive(false);
				var material = data.GetAsset() as Material;
				if (material)
					m_Sphere.sharedMaterial = material;
				break;
			case "Texture2D":
				goto case "Texture";
			case "Texture":
				m_Sphere.gameObject.SetActive(true);
				icon.gameObject.SetActive(false);
				var texture = data.GetAsset() as Texture;
				if (texture)
					m_Sphere.sharedMaterial = new Material(Shader.Find("Standard")) { mainTexture = texture };
				break;
			default:
				m_Sphere.gameObject.SetActive(false);
				icon.gameObject.SetActive(true);
				if (m_Icon == null)
				{
					var cachedIcon = data.GetCachedIcon();
					if (cachedIcon)
					{
						cachedIcon.wrapMode = TextureWrapMode.Clamp;
						m_Cube.sharedMaterial.mainTexture = cachedIcon;
					}
					else
						m_Cube.sharedMaterial.mainTexture = null;
				}
				break;
		}
	}
}