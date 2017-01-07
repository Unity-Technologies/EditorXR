using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.EditorVR.Extensions;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Tools;

namespace UnityEngine.Experimental.EditorVR.Menus
{
	public class MainMenuUI : MonoBehaviour, IInstantiateUI
	{
		public class ButtonData
		{
			public string name;
			public string sectionName;
			public string description;
		}

		private enum RotationState
		{
			AtRest,
			Snapping,
		}

		private enum VisibilityState
		{
			Hidden,
			Visible,
			TransitioningIn,
			TransitioningOut
		}

		[SerializeField] private MainMenuButton m_ButtonTemplatePrefab;
		[SerializeField] private Transform[] m_MenuFaceContainers;
		[SerializeField] private MainMenuFace m_MenuFacePrefab;
		[SerializeField] private Transform m_MenuFaceRotationOrigin;
		[SerializeField] private SkinnedMeshRenderer m_MenuFrameRenderer;
		[SerializeField] private Transform m_AlternateMenu;

		public int targetFaceIndex
		{
			get { return m_TargetFaceIndex; }
			set
			{
				m_Direction = (int)Mathf.Sign(value - m_TargetFaceIndex);

				// Loop around both ways
				if (value < 0)
					value += faceCount;
				m_TargetFaceIndex = value % faceCount;
			}
		}
		private int m_TargetFaceIndex;

		public Dictionary<string, List<Transform>> faceButtons { get { return m_FaceButtons; } }
		private readonly Dictionary<string, List<Transform>> m_FaceButtons = new Dictionary<string, List<Transform>>();

		private const float kFaceRotationSnapAngle = 90f;
		private const int kFaceCount = 4;
		private const float kDefaultSnapSpeed = 10f;
		private const float kRotationEpsilon = 1f;

		private readonly string kUncategorizedFaceName = "Uncategorized";
		private readonly Color kMenuFacesHiddenColor = new Color(1f, 1f, 1f, 0.5f);

		private VisibilityState m_VisibilityState = VisibilityState.Visible;
		private RotationState m_RotationState;
		private MainMenuFace[] m_MenuFaces;
		private Material m_MenuFacesMaterial;
		private Color m_MenuFacesColor;
		private Transform m_MenuOrigin;
		private Transform m_AlternateMenuOrigin;
		private Vector3 m_AlternateMenuOriginOriginalLocalScale;
		private Coroutine m_VisibilityCoroutine;
		Coroutine m_FrameRevealCoroutine;
		int m_Direction;

		Transform[] m_MenuFaceContentTransforms;
		Vector3[] m_MenuFaceContentOriginalLocalPositions;
		Vector3[] m_MenuFaceContentOffsetLocalPositions;
		Vector3 m_MenuFaceContentOriginalLocalScale;
		Vector3 m_MenuFaceContentHiddenLocalScale;

		public Transform menuOrigin
		{
			get { return m_MenuOrigin; }
			set
			{
				m_MenuOrigin = value;
				transform.SetParent(menuOrigin);
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
				transform.localScale = Vector3.one;
			}
		}

		public Transform alternateMenuOrigin
		{
			get { return m_AlternateMenuOrigin; }
			set
			{
				m_AlternateMenuOrigin = value;
				m_AlternateMenu.SetParent(m_AlternateMenuOrigin);
				m_AlternateMenu.localPosition = Vector3.zero;
				m_AlternateMenu.localRotation = Quaternion.identity;
				m_AlternateMenuOriginOriginalLocalScale = m_AlternateMenu.localScale;
			}
		}

		public InstantiateUIDelegate instantiateUI { private get; set; }

		public float targetRotation { get; set; }

		public int faceCount { get { return m_MenuFaces.Length; } }

		public bool visible
		{
			get { return m_VisibilityState == VisibilityState.Visible || m_VisibilityState == VisibilityState.TransitioningIn; }
			set
			{
				switch (m_VisibilityState)
				{
					case VisibilityState.TransitioningOut:
					case VisibilityState.Hidden:
						if (value)
						{
							this.StopCoroutine(ref m_VisibilityCoroutine);
							gameObject.SetActive(true);
							m_VisibilityCoroutine = StartCoroutine(AnimateShow());
						}
						return;
					case VisibilityState.TransitioningIn:
					case VisibilityState.Visible:
						if (!value)
						{
							this.StopCoroutine(ref m_VisibilityCoroutine);
							m_VisibilityCoroutine = StartCoroutine(AnimateHide());
						}
						return;
				}
			}
		}

		private int currentFaceIndex
		{
			get
			{
				// Floating point can leave us close to our actual rotation, but not quite (179.3438,
				// which effectively we want to treat as 180)
				return GetActualFaceIndexForRotation(Mathf.Ceil(currentRotation));
			}
		}

		private float currentRotation
		{
			get { return m_MenuFaceRotationOrigin.localRotation.eulerAngles.y; }
		}

		private void Awake()
		{
			m_MenuFacesMaterial = U.Material.GetMaterialClone(m_MenuFaceRotationOrigin.GetComponent<MeshRenderer>());
			m_MenuFacesColor = m_MenuFacesMaterial.color;
		}

		public void Setup()
		{
			m_MenuFaceContentTransforms = new Transform[kFaceCount];
			m_MenuFaceContentOffsetLocalPositions = new Vector3[kFaceCount];
			m_MenuFaceContentOriginalLocalPositions = new Vector3[kFaceCount];
			m_MenuFaces = new MainMenuFace[kFaceCount];
			for (var faceCount = 0; faceCount < kFaceCount; ++faceCount)
			{
				// Add faces to the menu
				var faceTransform = instantiateUI(m_MenuFacePrefab.gameObject).transform;
				faceTransform.SetParent(m_MenuFaceContainers[faceCount]);
				faceTransform.localRotation = Quaternion.identity;
				faceTransform.localScale = Vector3.one;
				faceTransform.localPosition = Vector3.zero;
				var face = faceTransform.GetComponent<MainMenuFace>();
				m_MenuFaces[faceCount] = face;

				// Cache Face content reveal values
				m_MenuFaceContentTransforms[faceCount] = faceTransform;
				m_MenuFaceContentOriginalLocalPositions[faceCount] = faceTransform.localPosition;
				m_MenuFaceContentOffsetLocalPositions[faceCount] = new Vector3(faceTransform.localPosition.x, faceTransform.localPosition.y, faceTransform.localPosition.z - 0.15f); // a position offset slightly in front of the menu face original position
			}

			m_MenuFaceContentOriginalLocalScale = m_MenuFaceContentTransforms[0].localScale;
			m_MenuFaceContentHiddenLocalScale = new Vector3(0f, m_MenuFaceContentOriginalLocalScale.y * 0.5f, m_MenuFaceContentOriginalLocalScale.z);

			transform.localScale = Vector3.zero;
			m_AlternateMenu.localScale = Vector3.zero;
		}

		private void Update()
		{
			if (m_VisibilityState == VisibilityState.TransitioningIn || m_VisibilityState == VisibilityState.TransitioningOut)
				return;

			if (m_VisibilityState == VisibilityState.Hidden)
				return;

			// Allow any snaps to finish before resuming any other operations
			if (m_RotationState == RotationState.Snapping)
				return;

			var faceIndex = currentFaceIndex;

			if (faceIndex != targetFaceIndex)
				StartCoroutine(SnapToFace(faceIndex + m_Direction, kDefaultSnapSpeed));
		}

		private void OnDestroy()
		{
			foreach (var face in m_MenuFaces)
				U.Object.Destroy(face.gameObject);
		}

		public void CreateFaceButton(ButtonData buttonData, Action<MainMenuButton> buttonCreationCallback)
		{
			var button = U.Object.Instantiate(m_ButtonTemplatePrefab.gameObject);
			button.name = buttonData.name;
			MainMenuButton mainMenuButton = button.GetComponent<MainMenuButton>();
			buttonCreationCallback(mainMenuButton);

			if (string.IsNullOrEmpty(buttonData.sectionName))
				buttonData.sectionName = kUncategorizedFaceName;

			mainMenuButton.SetData(buttonData.name, buttonData.description);

			var found = m_FaceButtons.Any(x => x.Key == buttonData.sectionName);
			if (found)
			{
				var kvp = m_FaceButtons.First(x => x.Key == buttonData.sectionName);
				kvp.Value.Add(button.transform);
			}
			else
			{
				m_FaceButtons.Add(buttonData.sectionName, new List<Transform>() { button.transform });
			}
		}

		public void SetupMenuFaces()
		{
			int position = 0;
			foreach (var faceButtons in m_FaceButtons)
			{
				m_MenuFaces[position].SetFaceData(faceButtons.Key, faceButtons.Value,
					UnityBrandColorScheme.GetRandomGradient());
				++position;
			}
		}

		private int GetClosestFaceIndexForRotation(float rotation)
		{
			return Mathf.RoundToInt(rotation / kFaceRotationSnapAngle) % faceCount;
		}

		private int GetActualFaceIndexForRotation(float rotation)
		{
			return Mathf.FloorToInt(rotation / kFaceRotationSnapAngle) % faceCount;
		}
	
		private float GetRotationForFaceIndex(int faceIndex)
		{
			return faceIndex * kFaceRotationSnapAngle;
		}

		private IEnumerator SnapToFace(int faceIndex, float snapSpeed)
		{
			if (m_RotationState == RotationState.Snapping)
				yield break;

			m_RotationState = RotationState.Snapping;

			// When the user releases their input while rotating the menu, snap to the nearest face
			StartCoroutine(AnimateFrameRotationShapeChange(m_RotationState));

			foreach (var face in m_MenuFaces)
				face.EndVisuals();

			float rotation = currentRotation;
			float faceTargetRotation = GetRotationForFaceIndex(faceIndex);

			float smoothVelocity = 0f;
			float smoothSnapSpeed = 0.5f;
			while (Mathf.Abs(Mathf.DeltaAngle(rotation, faceTargetRotation)) > kRotationEpsilon)
			{
				smoothSnapSpeed = U.Math.SmoothDamp(smoothSnapSpeed, snapSpeed, ref smoothVelocity, 0.0625f, Mathf.Infinity, Time.unscaledDeltaTime);
				rotation = Mathf.LerpAngle(rotation, faceTargetRotation, Time.unscaledDeltaTime * smoothSnapSpeed);
				m_MenuFaceRotationOrigin.localRotation = Quaternion.Euler(new Vector3(0, rotation, 0));
				yield return null;
			}
			m_MenuFaceRotationOrigin.localRotation = Quaternion.Euler(new Vector3(0, faceTargetRotation, 0));

			// Target face index and rotation can be set separately, so both, must be kept in sync
			targetRotation = faceTargetRotation;

			m_RotationState = RotationState.AtRest;
		}

		private IEnumerator AnimateShow()
		{
			if (m_VisibilityCoroutine != null)
				yield break;

			m_VisibilityState = VisibilityState.TransitioningIn;

			foreach (var face in m_MenuFaces)
				face.Show();

			if (m_FrameRevealCoroutine != null)
				StopCoroutine(m_FrameRevealCoroutine);

			m_FrameRevealCoroutine = StartCoroutine(AnimateFrameReveal(m_VisibilityState));

			for (int i = 0; i < m_MenuFaceContainers.Length; ++i)
				StartCoroutine(AnimateFaceReveal(i));

			const float kTargetScale = 1f;
			const float kSmoothTime = 0.125f;
			var scale = 0f;
			var smoothVelocity = 0f;
			var currentDuration = 0f;
			while (currentDuration < kSmoothTime)
			{
				scale = U.Math.SmoothDamp(scale, kTargetScale, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
				currentDuration += Time.unscaledDeltaTime;
				transform.localScale = Vector3.one * scale;
				m_AlternateMenu.localScale = m_AlternateMenuOriginOriginalLocalScale * scale;
				yield return null;
			}

			m_VisibilityState = VisibilityState.Visible;

			m_VisibilityCoroutine = null;
		}

		private IEnumerator AnimateHide()
		{
			if (m_VisibilityCoroutine != null)
				yield break;

			m_VisibilityState = VisibilityState.TransitioningOut;

			foreach (var face in m_MenuFaces)
				face.Hide();

			if (m_FrameRevealCoroutine != null)
				StopCoroutine(m_FrameRevealCoroutine);

			m_FrameRevealCoroutine = StartCoroutine(AnimateFrameReveal(m_VisibilityState));

			const float kTargetScale = 0f;
			const float kSmoothTime = 0.06875f;
			var scale = transform.localScale.x;
			var smoothVelocity = 0f;
			var currentDuration = 0f;
			while (currentDuration < kSmoothTime)
			{
				scale = U.Math.SmoothDamp(scale, kTargetScale, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
				currentDuration += Time.unscaledDeltaTime;
				transform.localScale = Vector3.one * scale;
				m_AlternateMenu.localScale = m_AlternateMenuOriginOriginalLocalScale * scale;
				yield return null;
			}

			gameObject.SetActive(false);

			m_VisibilityState = VisibilityState.Hidden;

			float snapRotation = GetRotationForFaceIndex(GetClosestFaceIndexForRotation(currentRotation));
			m_MenuFaceRotationOrigin.localRotation = Quaternion.Euler(new Vector3(0, snapRotation, 0)); // set intended target rotation
			m_RotationState = RotationState.AtRest;

			m_VisibilityCoroutine = null;
		}

		private IEnumerator AnimateFrameRotationShapeChange(RotationState rotationState)
		{
			var smoothTime = 0.0375f;
			var currentBlendShapeWeight = m_MenuFrameRenderer.GetBlendShapeWeight(0);
			var targetWeight = 0f;
			var smoothVelocity = 0f;
			var currentDuration = 0f;
			while (m_RotationState == rotationState && currentDuration < smoothTime)
			{
				currentBlendShapeWeight = U.Math.SmoothDamp(currentBlendShapeWeight, targetWeight, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
				currentDuration += Time.unscaledDeltaTime;
				m_MenuFrameRenderer.SetBlendShapeWeight(0, currentBlendShapeWeight);
				yield return null;
			}

			if (m_RotationState == rotationState)
				m_MenuFrameRenderer.SetBlendShapeWeight(0, targetWeight);
		}

		private IEnumerator AnimateFrameReveal(VisibilityState visibilityState)
		{
			m_MenuFrameRenderer.SetBlendShapeWeight(1, 100f);
			const float zeroStartBlendShapePadding = 20f; // start the blendShape at a point slightly above the full hidden value for better visibility
			const float kLerpEmphasisWeight = 0.25f;
			var smoothTime = visibilityState == VisibilityState.TransitioningIn ? 0.1875f : 0.09375f; // slower if transitioning in
			var currentBlendShapeWeight = m_MenuFrameRenderer.GetBlendShapeWeight(1);
			var targetWeight = visibilityState == VisibilityState.TransitioningIn ? 0f : 100f;
			var smoothVelocity = 0f;
			currentBlendShapeWeight = currentBlendShapeWeight > 0 ? currentBlendShapeWeight : zeroStartBlendShapePadding;

			var currentDuration = 0f;
			while (m_VisibilityState != VisibilityState.Hidden && currentDuration < smoothTime)
			{
				currentBlendShapeWeight = U.Math.SmoothDamp(currentBlendShapeWeight, targetWeight, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
				currentDuration += Time.unscaledDeltaTime;
				m_MenuFrameRenderer.SetBlendShapeWeight(1, currentBlendShapeWeight * currentBlendShapeWeight);
				m_MenuFacesMaterial.color = Color.Lerp(m_MenuFacesColor, kMenuFacesHiddenColor, currentBlendShapeWeight * kLerpEmphasisWeight);
				yield return null;
			}

			if (m_VisibilityState == visibilityState)
			{
				m_MenuFrameRenderer.SetBlendShapeWeight(1, targetWeight);
				m_MenuFacesMaterial.color = targetWeight > 0 ? m_MenuFacesColor : kMenuFacesHiddenColor;
			}

			if (m_VisibilityState == VisibilityState.Hidden)
				m_MenuFrameRenderer.SetBlendShapeWeight(0, 0);

			m_FrameRevealCoroutine = null;
		}

		IEnumerator AnimateFaceReveal(int faceIndex)
		{
			var targetScale = m_MenuFaceContentOriginalLocalScale;
			var targetPosition = m_MenuFaceContentOriginalLocalPositions[faceIndex];
			var currentScale = m_MenuFaceContentHiddenLocalScale; // Custom initial scale
			var currentPosition = m_MenuFaceContentOffsetLocalPositions[faceIndex]; // start the face in the cached original target position
			var faceTransform = m_MenuFaceContentTransforms[faceIndex];

			faceTransform.localScale = currentScale;
			faceTransform.localPosition = currentPosition;

			const float kSmoothTime = 0.1f;
			var currentDelay = 0f;
			var delayTarget = 0.5f + (faceIndex * 0.1f); // delay duration before starting the face reveal
			while (currentDelay < delayTarget) // delay the reveal of each face slightly more than the previous
			{
				currentDelay += Time.unscaledDeltaTime;
				yield return null;
			}

			var smoothVelocity = Vector3.zero;
			while (!Mathf.Approximately(currentScale.x, targetScale.x))
			{
				currentScale = Vector3.SmoothDamp(currentScale, targetScale, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
				currentPosition = Vector3.Lerp(currentPosition, targetPosition, Mathf.Pow(currentScale.x / targetScale.x, 2)); // lerp the position with extra emphasis on the beginning transition
				faceTransform.localScale = currentScale;
				faceTransform.localPosition = currentPosition;
				yield return null;
			}

			faceTransform.localScale = targetScale;
			faceTransform.localPosition = targetPosition;
		}
	}
}