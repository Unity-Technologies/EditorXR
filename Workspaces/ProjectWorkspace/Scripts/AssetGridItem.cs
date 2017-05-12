#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	sealed class AssetGridItem : DraggableListItem<AssetData, string>, IPlaceSceneObject, IUsesSpatialHash, IUsesViewerBody
	{
		const float k_PreviewDuration = 0.1f;
		const float k_MaxPreviewScale = 0.2f;
		const float k_RotateSpeed = 50f;
		const float k_TransitionDuration = 0.1f;
		const int k_PreviewRenderQueue = 9200;

		[SerializeField]
		Text m_Text;

		[SerializeField]
		BaseHandle m_Handle;

		[SerializeField]
		Image m_TextPanel;

		[SerializeField]
		Renderer m_Cube;

		[SerializeField]
		Renderer m_Sphere;

		[HideInInspector]
		[SerializeField] // Serialized so that this remains set after cloning
		GameObject m_Icon;

		GameObject m_IconPrefab;

		[HideInInspector]
		[SerializeField] // Serialized so that this remains set after cloning
		Transform m_PreviewObjectTransform;

		bool m_Setup;
		Vector3 m_PreviewPrefabScale;
		Vector3 m_PreviewTargetScale;
		Vector3 m_GrabPreviewTargetScale;
		Vector3 m_GrabPreviewPivotOffset;
		Transform m_PreviewObjectClone;

		Coroutine m_PreviewCoroutine;
		Coroutine m_VisibilityCoroutine;

		Material m_SphereMaterial;

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
					ObjectUtils.Destroy(m_Icon);

				m_IconPrefab = value;
				m_Icon = ObjectUtils.Instantiate(m_IconPrefab, transform, false);
				m_Icon.transform.localPosition = Vector3.up * 0.5f;
				m_Icon.transform.localRotation = Quaternion.AngleAxis(90, Vector3.down);
				m_Icon.transform.localScale = Vector3.one;
			}
		}

		public Material material
		{
			set
			{
				if (m_SphereMaterial)
					ObjectUtils.Destroy(m_SphereMaterial);

				m_SphereMaterial = Instantiate(value);
				m_SphereMaterial.renderQueue = k_PreviewRenderQueue;
				m_Sphere.sharedMaterial = m_SphereMaterial;
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

				if (m_SphereMaterial)
					ObjectUtils.Destroy(m_SphereMaterial);

				m_SphereMaterial = new Material(Shader.Find("Standard")) { mainTexture = value };
				m_SphereMaterial.renderQueue = k_PreviewRenderQueue;
				m_Sphere.sharedMaterial = m_SphereMaterial;
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

		public float scaleFactor { private get; set; }

		public override void Setup(AssetData listData)
		{
			base.Setup(listData);

			m_PreviewCoroutine = null;
			m_VisibilityCoroutine = null;

			// First time setup
			if (!m_Setup)
			{
				// Cube material might change, so we always instance it
				MaterialUtils.GetMaterialClone(m_Cube);

				m_Handle.dragStarted += OnDragStarted;
				m_Handle.dragging += OnDragging;
				m_Handle.dragEnded += OnDragEnded;

				m_Handle.hoverStarted += OnHoverStarted;
				m_Handle.hoverEnded += OnHoverEnded;

				m_Handle.getDropObject = GetDropObject;

				m_Setup = true;
			}

			InstantiatePreview();

			icon.transform.localScale = Vector3.one;

			if (m_PreviewObjectTransform)
				m_PreviewObjectTransform.localScale = Vector3.zero;

			m_Text.text = listData.name;
		}

		public void UpdateTransforms(float scale)
		{
			scaleFactor = scale;

			// Don't scale the item while changing visibility because this would conflict with AnimateVisibility
			if (m_VisibilityCoroutine != null)
				return;

			transform.localScale = Vector3.one * scale;

			m_TextPanel.transform.localRotation = CameraUtils.LocalRotateTowardCamera(transform.parent.rotation);

			if (m_Sphere.gameObject.activeInHierarchy)
				m_Sphere.transform.Rotate(Vector3.up, k_RotateSpeed * Time.deltaTime, Space.Self);

			if (data.type == "Scene")
			{
				icon.transform.rotation =
					Quaternion.LookRotation(icon.transform.position - CameraUtils.GetMainCamera().transform.position, Vector3.up);
			}
		}

		void InstantiatePreview()
		{
			if (m_PreviewObjectTransform)
				ObjectUtils.Destroy(m_PreviewObjectTransform.gameObject);

			if (!data.preview)
				return;

			m_PreviewObjectTransform = Instantiate(data.preview).transform;

			m_PreviewObjectTransform.position = Vector3.zero;
			m_PreviewObjectTransform.rotation = Quaternion.identity;

			m_PreviewPrefabScale = m_PreviewObjectTransform.localScale;

			// Normalize total scale to 1
			var previewTotalBounds = ObjectUtils.GetBounds(m_PreviewObjectTransform);

			// Don't show a preview if there are no renderers
			if (previewTotalBounds.size == Vector3.zero)
			{
				ObjectUtils.Destroy(m_PreviewObjectTransform.gameObject);
				return;
			}

			// Turn off expensive render settings
			foreach (var renderer in m_PreviewObjectTransform.GetComponentsInChildren<Renderer>())
			{
				renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				renderer.receiveShadows = false;
				renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
				renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
			}

			// Turn off lights
			foreach (var light in m_PreviewObjectTransform.GetComponentsInChildren<Light>())
			{
				light.enabled = false;
			}

			var pivotOffset = m_PreviewObjectTransform.position - previewTotalBounds.center;
			m_PreviewObjectTransform.SetParent(transform, false);

			var maxComponent = previewTotalBounds.size.MaxComponent();
			var scaleFactor = 1 / maxComponent;
			m_PreviewTargetScale = m_PreviewPrefabScale * scaleFactor;
			m_PreviewObjectTransform.localPosition = pivotOffset * scaleFactor + Vector3.up * 0.5f;

			// Object will preview at the same size when grabbed
			m_GrabPreviewTargetScale = Vector3.one * maxComponent;
			var previewExtents = previewTotalBounds.extents;
			m_GrabPreviewPivotOffset = pivotOffset;

			// If bounds are greater than offset, set to bounds
			if (previewExtents.y > m_GrabPreviewPivotOffset.y)
				m_GrabPreviewPivotOffset.y = previewExtents.y;

			if (previewExtents.z > m_GrabPreviewPivotOffset.z)
				m_GrabPreviewPivotOffset.z = previewExtents.z;

			if (maxComponent > k_MaxPreviewScale)
			{
				// Object will be preview at the maximum scale
				m_GrabPreviewTargetScale = Vector3.one * k_MaxPreviewScale;
				m_GrabPreviewPivotOffset = pivotOffset * scaleFactor + (Vector3.up + Vector3.forward) * 0.5f * k_MaxPreviewScale;
			}

			m_PreviewObjectTransform.gameObject.SetActive(false);
			m_PreviewObjectTransform.localScale = Vector3.zero;
		}

		protected override void OnDragStarted(BaseHandle handle, HandleEventData eventData)
		{
			base.OnDragStarted(handle, eventData);

			var clone = (GameObject)Instantiate(gameObject, transform.position, transform.rotation, transform.parent);
			var cloneItem = clone.GetComponent<AssetGridItem>();

			if (cloneItem.m_PreviewObjectTransform)
			{
				m_PreviewObjectClone = cloneItem.m_PreviewObjectTransform;

#if UNITY_EDITOR
				var originalPosition = m_PreviewObjectClone.position;
				var originalRotation = m_PreviewObjectClone.rotation;
				var originalScale = m_PreviewObjectClone.localScale;
				var restoreParent = m_PreviewObjectClone.parent;
				m_PreviewObjectClone.SetParent(null); // HACK: MergePrefab deactivates the root transform when calling ConnectGameObjectToPrefab, which is EditorVR in this case
				m_PreviewObjectClone = PrefabUtility.ConnectGameObjectToPrefab(m_PreviewObjectClone.gameObject, data.preview).transform;
				m_PreviewObjectClone.SetParent(restoreParent);
				m_PreviewObjectClone.position = originalPosition;
				m_PreviewObjectClone.rotation = originalRotation;
				m_PreviewObjectClone.localScale = originalScale;
				cloneItem.m_PreviewObjectTransform = m_PreviewObjectClone;
#endif

				cloneItem.m_Cube.gameObject.SetActive(false);

				if (cloneItem.m_Icon)
					cloneItem.m_Icon.gameObject.SetActive(false);

				m_PreviewObjectClone.gameObject.SetActive(true);
				m_PreviewObjectClone.localScale = m_PreviewTargetScale;

				// Destroy label
				ObjectUtils.Destroy(cloneItem.m_TextPanel.gameObject);
			}

			m_DragObject = clone.transform;

			// Disable any SmoothMotion that may be applied to a cloned Asset Grid Item now referencing input device p/r/s
			var smoothMotion = clone.GetComponent<SmoothMotion>();
			if (smoothMotion != null)
				smoothMotion.enabled = false;

			StartCoroutine(ShowGrabbedObject());
		}

		protected override void OnDragEnded(BaseHandle handle, HandleEventData eventData)
		{
			var gridItem = m_DragObject.GetComponent<AssetGridItem>();

			if (!this.IsOverShoulder(eventData.rayOrigin))
			{
				if (gridItem.m_PreviewObjectTransform)
				{
					this.PlaceSceneObject(gridItem.m_PreviewObjectTransform, m_PreviewPrefabScale);
				}
				else
				{
					switch (data.type)
					{
						case "Prefab":
						case "Model":
#if UNITY_EDITOR
							var go = (GameObject)PrefabUtility.InstantiatePrefab(data.asset);
							var transform = go.transform;
							transform.position = gridItem.transform.position;
							transform.rotation = MathUtilsExt.ConstrainYawRotation(gridItem.transform.rotation);
#else
							var go = (GameObject)Instantiate(data.asset, gridItem.transform.position, gridItem.transform.rotation);
#endif

							this.AddToSpatialHash(go);
							break;
					}
				}
			}

			StartCoroutine(HideGrabbedObject(m_DragObject.gameObject, gridItem.m_Cube));
		}

		void OnHoverStarted(BaseHandle handle, HandleEventData eventData)
		{
			if (m_PreviewObjectTransform && gameObject.activeInHierarchy)
			{
				this.StopCoroutine(ref m_PreviewCoroutine);
				m_PreviewCoroutine = StartCoroutine(AnimatePreview(false));
			}
		}

		void OnHoverEnded(BaseHandle handle, HandleEventData eventData)
		{
			if (m_PreviewObjectTransform && gameObject.activeInHierarchy)
			{
				this.StopCoroutine(ref m_PreviewCoroutine);
				m_PreviewCoroutine = StartCoroutine(AnimatePreview(true));
			}
		}

		IEnumerator AnimatePreview(bool @out)
		{
			icon.SetActive(true);
			m_PreviewObjectTransform.gameObject.SetActive(true);

			var iconTransform = icon.transform;
			var currentIconScale = iconTransform.localScale;
			var targetIconScale = @out ? Vector3.one : Vector3.zero;

			var currentPreviewScale = m_PreviewObjectTransform.localScale;
			var targetPreviewScale = @out ? Vector3.zero : m_PreviewTargetScale;

			var startTime = Time.realtimeSinceStartup;
			while (Time.realtimeSinceStartup - startTime < k_PreviewDuration)
			{
				var t = (Time.realtimeSinceStartup - startTime) / k_PreviewDuration;

				icon.transform.localScale = Vector3.Lerp(currentIconScale, targetIconScale, t);
				m_PreviewObjectTransform.transform.localScale = Vector3.Lerp(currentPreviewScale, targetPreviewScale, t);
				yield return null;
			}

			m_PreviewObjectTransform.transform.localScale = targetPreviewScale;
			icon.transform.localScale = targetIconScale;

			m_PreviewObjectTransform.gameObject.SetActive(!@out);
			icon.SetActive(@out);

			m_PreviewCoroutine = null;
		}

		public void SetVisibility(bool visible, Action<AssetGridItem> callback = null)
		{
			this.StopCoroutine(ref m_VisibilityCoroutine);
			m_VisibilityCoroutine = StartCoroutine(AnimateVisibility(visible, callback));
		}

		IEnumerator AnimateVisibility(bool visible, Action<AssetGridItem> callback)
		{
			var currentTime = 0f;

			// Item should always be at a scale of zero before becoming visible
			if (visible)
				transform.localScale = Vector3.zero;

			var currentScale = transform.localScale;
			var targetScale = visible ? Vector3.one * scaleFactor : Vector3.zero;

			while (currentTime < k_TransitionDuration)
			{
				currentTime += Time.deltaTime;
				transform.localScale = Vector3.Lerp(currentScale, targetScale, currentTime / k_TransitionDuration);
				yield return null;
			}

			transform.localScale = targetScale;

			if (callback != null)
				callback(this);

			m_VisibilityCoroutine = null;
		}

		object GetDropObject(BaseHandle handle)
		{
			return data.asset;
		}

		void OnDestroy()
		{
			if (m_SphereMaterial)
				ObjectUtils.Destroy(m_SphereMaterial);

			ObjectUtils.Destroy(m_Cube.sharedMaterial);
		}

		// Animate the LocalScale of the asset towards a common/unified scale
		// used when the asset is magnetized/attached to the proxy, after grabbing it from the asset grid
		IEnumerator ShowGrabbedObject()
		{
			var currentLocalScale = m_DragObject.localScale;
			var currentPreviewOffset = Vector3.zero;

			if (m_PreviewObjectClone)
				currentPreviewOffset = m_PreviewObjectClone.localPosition;

			var currentTime = 0f;
			var currentVelocity = 0f;
			const float kDuration = 1f;

			while (currentTime < kDuration - 0.05f)
			{
				if (m_DragObject == null)
					yield break; // Exit coroutine if m_GrabbedObject is destroyed before the loop is finished

				currentTime = MathUtilsExt.SmoothDamp(currentTime, kDuration, ref currentVelocity, 0.5f, Mathf.Infinity, Time.deltaTime);
				m_DragObject.localScale = Vector3.Lerp(currentLocalScale, m_GrabPreviewTargetScale, currentTime);

				if (m_PreviewObjectClone)
					m_PreviewObjectClone.localPosition = Vector3.Lerp(currentPreviewOffset, m_GrabPreviewPivotOffset, currentTime);

				yield return null;
			}

			m_DragObject.localScale = m_GrabPreviewTargetScale;
		}

		static IEnumerator HideGrabbedObject(GameObject itemToHide, Renderer cubeRenderer)
		{
			var itemTransform = itemToHide.transform;
			var currentScale = itemTransform.localScale;
			var targetScale = Vector3.zero;
			var transitionAmount = Time.deltaTime;
			var transitionAddMultiplier = 6;
			while (transitionAmount < 1)
			{
				itemTransform.localScale = Vector3.Lerp(currentScale, targetScale, transitionAmount);
				transitionAmount += Time.deltaTime * transitionAddMultiplier;
				yield return null;
			}

			cubeRenderer.sharedMaterial = null; // Drop material so it won't be destroyed (shared with cube in list)
			ObjectUtils.Destroy(itemToHide);
		}
	}
}
#endif
