using System;
using System.Collections;
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

	private const float kMagnetizeDuration = 0.75f;
	private readonly Vector3 kGrabOffset = new Vector3(0, 0.02f, 0.03f);

	[SerializeField]
	private Text m_Text;

	[SerializeField]
	private DirectHandle m_Cube;

	[SerializeField]
	private Material m_NoClipCubeMaterial;

	private Renderer m_CubeRenderer;
	private bool m_Setup;

	private Transform m_GrabbedObject;
	private float m_GrabLerp;

	public override void Setup(AssetData listData)
	{
		base.Setup(listData);
		// First time setup
		if (!m_Setup)
		{
			// Cube material might change, so we always instance it
			m_CubeRenderer = m_Cube.GetComponent<Renderer>();
			U.Material.GetMaterialClone(m_CubeRenderer);

			m_Cube.onHandleBeginDrag += GrabBegin;
			m_Cube.onHandleDrag += GrabDrag;
			m_Cube.onHandleEndDrag += GrabEnd;

			m_Setup = true;
		}

		m_Text.text = Path.GetFileNameWithoutExtension(listData.path);

		var assetPath = data.path.Substring(data.path.IndexOf("Assets"));
		var cachedIcon = AssetDatabase.GetCachedIcon(assetPath);
		if (cachedIcon)
		{
			m_CubeRenderer.sharedMaterial.mainTexture = cachedIcon;
		}
	}

	public void SwapMaterials(Material textMaterial)
	{
		m_Text.material = textMaterial;
	}

	public void UpdateTransforms()
	{
		var cameraTransform = U.Camera.GetMainCamera().transform;

		Vector3 eyeVector3 = Quaternion.Inverse(transform.parent.rotation) * cameraTransform.forward;
		eyeVector3.x = 0;
		if (Vector3.Dot(eyeVector3, Vector3.forward) > 0)
			m_Text.transform.localRotation = Quaternion.LookRotation(eyeVector3, Vector3.up);
		else
			m_Text.transform.localRotation = Quaternion.LookRotation(eyeVector3, Vector3.down);
	}

	public void GetMaterials(out Material textMaterial)
	{
		textMaterial = Object.Instantiate(m_Text.material);
	}

	public void Clip(Bounds bounds, Matrix4x4 parentMatrix)
	{
		m_CubeRenderer.sharedMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_CubeRenderer.sharedMaterial.SetVector("_ClipExtents", bounds.extents);
	}

	private void GrabBegin(BaseHandle baseHandle, HandleDragEventData eventData)
	{
		var clone = (GameObject) Instantiate(gameObject, transform.position, transform.rotation, transform.parent);
		var cloneItem = clone.GetComponent<AssetGridItem>();
		cloneItem.m_Cube.GetComponent<Renderer>().sharedMaterial = m_NoClipCubeMaterial;
		cloneItem.m_Text.material = null;

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

	private void GrabDrag(BaseHandle baseHandle, HandleDragEventData eventData)
	{
		var rayTransform = eventData.rayOrigin.transform;
		m_GrabbedObject.transform.position = Vector3.Lerp(m_GrabbedObject.transform.position, rayTransform.position + rayTransform.rotation * kGrabOffset, m_GrabLerp);
		m_GrabbedObject.transform.rotation = Quaternion.Lerp(m_GrabbedObject.transform.rotation, rayTransform.rotation, m_GrabLerp);
	}

	private void GrabEnd(BaseHandle baseHandle, HandleDragEventData eventData)
	{
		U.Object.Destroy(m_GrabbedObject.gameObject);
	}

	private void OnDestroy()
	{
		if (m_CubeRenderer)
			U.Object.Destroy(m_CubeRenderer.sharedMaterial);
	}
}