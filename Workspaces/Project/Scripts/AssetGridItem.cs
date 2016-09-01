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
	private Material m_NoClipCubeMaterial;

	[SerializeField]
	private Renderer m_Cube;

	[SerializeField] // Serialized so that this remains set after cloning
	private Transform m_PreviewObject;

	private bool m_Setup;
	private Transform m_GrabbedObject;
	private Material m_GrabMaterial;
	private float m_GrabLerp;
	private float m_PreviewFade;
	private Vector3 m_PreviewPrefabScale;
	private Vector3 m_PreviewTargetScale;
	private Bounds? m_PreviewTotalBounds;

	private Coroutine m_TransitionCoroutine;

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

		m_Text.text = Path.GetFileNameWithoutExtension(listData.path);

		var assetPath = AssetData.GetPathRelativeToAssets(data.path);
		var cachedIcon = AssetDatabase.GetCachedIcon(assetPath);
		if (cachedIcon)
		{
			cachedIcon.wrapMode = TextureWrapMode.Clamp;
			m_Cube.sharedMaterial.mainTexture = cachedIcon;
		}
	}

	public void SwapMaterials(Material textMaterial)
	{
		m_Text.material = textMaterial;
		m_TextPanel.material = textMaterial;
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
				m_Cube.gameObject.SetActive(true);
				m_Cube.transform.localScale = Vector3.one;
			}
			else if (m_PreviewFade == 1)
			{
				m_PreviewObject.gameObject.SetActive(true);
				m_Cube.gameObject.SetActive(false);
				m_PreviewObject.transform.localScale = m_PreviewTargetScale;
			}
			else
			{
				m_Cube.gameObject.SetActive(true);
				m_PreviewObject.gameObject.SetActive(true);
				m_Cube.transform.localScale = Vector3.one * (1 - m_PreviewFade);
				m_PreviewObject.transform.localScale = Vector3.Lerp(Vector3.zero, m_PreviewTargetScale, m_PreviewFade);
			}
		}
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

	public void GetMaterials(out Material textMaterial)
	{
		textMaterial = Object.Instantiate(m_Text.material);
	}

	public void Clip(Bounds bounds, Matrix4x4 parentMatrix)
	{
		m_Cube.sharedMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_Cube.sharedMaterial.SetVector("_ClipExtents", bounds.extents * 5);
	}

	private void GrabBegin(BaseHandle baseHandle, HandleEventData eventData)
	{
		var clone = (GameObject) Instantiate(gameObject, transform.position, transform.rotation, transform.parent);
		var cloneItem = clone.GetComponent<AssetGridItem>();
		if(m_GrabMaterial)
			U.Object.Destroy(m_GrabMaterial);

		var cubeRenderer = cloneItem.m_Cube.GetComponent<Renderer>();
		cubeRenderer.sharedMaterial = m_NoClipCubeMaterial;
		m_GrabMaterial = U.Material.GetMaterialClone(cubeRenderer.GetComponent<Renderer>());
		m_GrabMaterial.mainTexture = m_Cube.sharedMaterial.mainTexture;
		m_GrabMaterial.color = Color.white;

		if (cloneItem.m_PreviewObject)
		{
			m_Cube.gameObject.SetActive(false);
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
			StartCoroutine(GrowObject(gridItem.m_PreviewObject));
		U.Object.Destroy(m_GrabbedObject.gameObject);
	}

	private IEnumerator GrowObject(Transform obj)
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
		if(m_GrabMaterial)
			U.Object.Destroy(m_GrabMaterial);
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
}