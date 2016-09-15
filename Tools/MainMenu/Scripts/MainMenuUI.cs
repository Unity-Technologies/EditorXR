using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Extensions;

namespace UnityEngine.VR.Menus
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
			Rotating,
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
		[SerializeField] private Transform m_InputArrowLeft;
		[SerializeField] private Transform m_InputArrowRight;
		[SerializeField] private MeshRenderer m_InputHighlightLeft;
		[SerializeField] private MeshRenderer m_InputHighlightRight;
		[SerializeField] private MeshRenderer m_InputOuterBorder;
		[SerializeField] private Transform[] m_MenuFaceContainers;
		[SerializeField] private Transform m_MenuFacePositionTarget;
		[SerializeField] private MainMenuFace m_MenuFacePrefab;
		[SerializeField] private Transform m_MenuFaceRotationOrigin;
		[SerializeField] private SkinnedMeshRenderer m_MenuFrameRenderer;
		[SerializeField] private Transform m_AlternateMenu;

		/// <summary>
		/// The object used to activate/deactivate the main menu
		/// </summary>
		[SerializeField]
		private MainMenuActivator m_MainMenuActivator;

		public int targetFaceIndex
		{
			get { return m_TargetFaceIndex; }
			set
			{
				// Loop around both ways
				if (value < 0)
					value += faceCount;
				m_TargetFaceIndex = value % faceCount;
			}
		}
		private int m_TargetFaceIndex;

		private const float kFaceRotationSnapAngle = 90f;
		private const int kFaceCount = 4;
		private const float kDefaultSnapSpeed = 10f;
		private const float kRotationEpsilon = 1f;

		private readonly string kUncategorizedFaceName = "Uncategorized";
		private readonly string kRotationHighlightColorProperty = "_Color";
		private readonly string kRotationHighlightTopProperty = "_ColorTop";
		private readonly string kRotationHighlightBottomProperty = "_ColorBottom";
		private readonly Color kMenuFacesHiddenColor = new Color(1f, 1f, 1f, 0.5f);

		private VisibilityState m_VisibilityState;
		private RotationState m_RotationState;
		private Material m_RotationHighlightLeftMaterial;
		private Material m_RotationHighlightRightMaterial;
		private Material m_OuterBorderMaterial;
		private List<MainMenuFace> m_MenuFaces;
		private Material m_MenuFacesMaterial;
		private Color m_MenuFacesColor;
		private Dictionary<string, List<Transform>> m_FaceButtons;
		private List<Transform> m_UncategorizedButtons;
		private Transform m_MenuOrigin;
		private Transform m_AlternateMenuOrigin;
		private Vector3 m_AlternateMenuOriginOriginalLocalScale;
		private float m_RotationRate;
		private float m_LastTargetRotation;
		private Coroutine m_VisibilityCoroutine;
		private Transform m_ShowHideUI;

		private Coroutine m_ActivatorMoveCoroutine;
		private Transform m_ActivatorTransform;
		private Vector3 m_OriginalActivatorLocalPosition;
		private Vector3 m_AlternateActivatorLocalPosition;

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

				m_ShowHideUI = m_MainMenuActivator.transform;
				m_ShowHideUI.SetParent(m_MenuOrigin); // Now that the desired world position is set, change parent to the menu origin, so no unintended transform actions are performed when showing/hiding
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
				m_AlternateMenu.localScale = Vector3.one;
				m_AlternateMenuOriginOriginalLocalScale = m_AlternateMenuOrigin.localScale;
			}
		}

		public Func<GameObject, GameObject> instantiateUI { private get; set; }

		public Action menuButtonSelected { set { m_MainMenuActivator.performActivation = value; } }

		public float targetRotation { get; set; }

		public int faceCount { get { return m_MenuFaces.Count; } }

		public bool visible
		{
			get { return m_VisibilityState == VisibilityState.Visible; }
			set
			{
				switch (m_VisibilityState)
				{
					case VisibilityState.TransitioningOut:
					case VisibilityState.Hidden:
						if (value)
						{
							StopCoroutine(ref m_VisibilityCoroutine);
							m_VisibilityCoroutine = StartCoroutine(AnimateShow());
						}
						return;
					case VisibilityState.TransitioningIn:
					case VisibilityState.Visible:
						if (!value)
						{
							StopCoroutine(ref m_VisibilityCoroutine);
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
			m_OuterBorderMaterial = U.Material.GetMaterialClone(m_InputOuterBorder);
			m_OuterBorderMaterial.SetColor(kRotationHighlightTopProperty, UnityBrandColorScheme.light);
			m_OuterBorderMaterial.SetColor(kRotationHighlightBottomProperty, UnityBrandColorScheme.light);
			m_RotationHighlightLeftMaterial = U.Material.GetMaterialClone(m_InputHighlightLeft);
			m_RotationHighlightRightMaterial = U.Material.GetMaterialClone(m_InputHighlightRight);
			m_RotationHighlightLeftMaterial.SetColor(kRotationHighlightColorProperty, Color.clear);
			m_RotationHighlightRightMaterial.SetColor(kRotationHighlightColorProperty, Color.clear);
			m_MenuFacesMaterial = U.Material.GetMaterialClone(m_MenuFaceRotationOrigin.GetComponent<MeshRenderer>());
			m_MenuFacesColor = m_MenuFacesMaterial.color;
		}

		// HACK: Cannot have this in Start because Awake/Start gets called together current in ExecuteInEditMode and
		// we need to make use of instantiateUI
		public void Setup()
		{
			if (m_FaceButtons == null)
			{
				m_FaceButtons = new Dictionary<string, List<Transform>>();
				m_UncategorizedButtons = new List<Transform>();
				m_FaceButtons.Add(kUncategorizedFaceName, m_UncategorizedButtons);
			}

			m_MenuFaces = new List<MainMenuFace>();
			for (var faceCount = 0; faceCount < kFaceCount; ++faceCount)
			{
				// Add faces to the menu
				var faceTransform = instantiateUI(m_MenuFacePrefab.gameObject).transform;
				faceTransform.SetParent(m_MenuFaceContainers[faceCount]);
				faceTransform.localRotation = Quaternion.identity;
				faceTransform.localScale = Vector3.one;
				faceTransform.localPosition = Vector3.zero;
				var face = faceTransform.GetComponent<MainMenuFace>();
				m_MenuFaces.Add(face);
			}

			m_ActivatorTransform = m_MainMenuActivator.transform;
			m_OriginalActivatorLocalPosition = m_ActivatorTransform.localPosition;
			m_AlternateActivatorLocalPosition = m_OriginalActivatorLocalPosition + Vector3.down * 0.1f;

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

			// If target rotation isn't being set any longer, then ignore seeking and simply snap
			var targetRotationDelta = targetRotation - m_LastTargetRotation;
			if (m_RotationRate > 0f && Mathf.Approximately(targetRotationDelta, 0f))
			{
				var rotation = currentRotation;
				var closestFaceIndex = GetClosestFaceIndexForRotation(rotation);
				var faceIndexRotation = GetRotationForFaceIndex(closestFaceIndex);

				if (Mathf.Abs(faceIndexRotation - rotation) > kRotationEpsilon)
				{
					targetFaceIndex = closestFaceIndex;
					StartCoroutine(SnapToFace(targetFaceIndex, kDefaultSnapSpeed * 0.5f)); // Slower snap for non-flick
					return;
				}
			}

			// Setting a target face takes precedence over manual rotation
			if (faceIndex != targetFaceIndex)
			{
				m_RotationHighlightLeftMaterial.SetColor(kRotationHighlightColorProperty, Color.clear);
				m_RotationHighlightRightMaterial.SetColor(kRotationHighlightColorProperty, Color.clear);

				var direction = (int)Mathf.Sign(Mathf.DeltaAngle(GetRotationForFaceIndex(faceIndex), GetRotationForFaceIndex(m_TargetFaceIndex)));
				StartCoroutine(SnapToFace(faceIndex + direction, kDefaultSnapSpeed));
			}
			else
			{
				float rotation = currentRotation;
				float deltaRotation = Mathf.DeltaAngle(rotation, targetRotation);
				if (Mathf.Abs(deltaRotation) > 0f)
				{
					if (m_RotationState != RotationState.Rotating)
					{
						m_RotationState = RotationState.Rotating;

						foreach (var face in m_MenuFaces)
							face.BeginVisuals();

						StartCoroutine(AnimateFrameRotationShapeChange(RotationState.Rotating));
					}

					int direction = (int) Mathf.Sign(deltaRotation);

					m_RotationHighlightLeftMaterial.SetColor(kRotationHighlightColorProperty, direction > 0 ? Color.white : Color.clear);
					m_RotationHighlightRightMaterial.SetColor(kRotationHighlightColorProperty, direction < 0 ? Color.white : Color.clear);

					const float kRotationRateMax = 10f;
					const float kRotationSpeed = 15f;
					m_RotationRate = Mathf.Min(m_RotationRate + Time.unscaledDeltaTime * kRotationSpeed, kRotationRateMax);
					m_MenuFaceRotationOrigin.Rotate(Vector3.up, deltaRotation * m_RotationRate * Time.unscaledDeltaTime);

					// Target face index and rotation can be set separately, so both, must be kept in sync
					targetFaceIndex = currentFaceIndex;
				}
				else
				{
					m_RotationState = RotationState.AtRest;

					// Allow for the smooth resumption of rotation if rotation is resumed before snapping is stopped
					m_RotationRate = Mathf.Max(m_RotationRate - Time.unscaledDeltaTime, 0f);
				}
			}

			m_LastTargetRotation = targetRotation;
		}

		private void OnDestroy()
		{
			foreach (var face in m_MenuFaces)
				U.Object.Destroy(face.gameObject);
		}

		public void CreateToolButton(ButtonData buttonData, Action<MainMenuButton> buttonCreationCallback)
		{
			var button = U.Object.Instantiate(m_ButtonTemplatePrefab.gameObject);
			button.name = buttonData.name;
			MainMenuButton mainMenuButton = button.GetComponent<MainMenuButton>();
			buttonCreationCallback(mainMenuButton);

			if (buttonData.sectionName != null)
			{
				mainMenuButton.SetData(buttonData.name, buttonData.description);

				var found = m_FaceButtons.Any(x => x.Key == buttonData.sectionName);
				if (found)
				{
					var kvp = m_FaceButtons.First(x => x.Key == buttonData.sectionName);
					kvp.Value.Add(button.transform);
				}
				else
				{
					m_FaceButtons.Add(buttonData.sectionName, new List<Transform>() {button.transform});
				}
			}
			else
			{
				m_UncategorizedButtons.Add(button.transform);
				mainMenuButton.SetData(buttonData.name, string.Empty);
			}
		}

		public void SetupMenuFaces()
		{
			int position = 0;
			foreach (var faceNameToButtons in m_FaceButtons)
			{
				m_MenuFaces[position].SetFaceData(faceNameToButtons.Key, faceNameToButtons.Value,
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
				smoothSnapSpeed = Mathf.SmoothDamp(smoothSnapSpeed, snapSpeed, ref smoothVelocity, 0.0625f, Mathf.Infinity, Time.unscaledDeltaTime);
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

			StartCoroutine(AnimateFrameReveal(m_VisibilityState));

			const float kTargetScale = 1f;
			
			float scale = 0f;
			float smoothVelocity = 0f;
			while (!Mathf.Approximately(scale, kTargetScale))
			{
				transform.localScale = Vector3.one * scale;
				m_AlternateMenu.localScale = m_AlternateMenuOriginOriginalLocalScale * scale;
				scale = Mathf.SmoothDamp(scale, kTargetScale, ref smoothVelocity, 0.125f, Mathf.Infinity, Time.unscaledDeltaTime);
				yield return null;
			}

			m_VisibilityState = VisibilityState.Visible;
			transform.localScale = Vector3.one;
			m_AlternateMenu.localScale = m_AlternateMenuOriginOriginalLocalScale;

			m_VisibilityCoroutine = null;
		}

		private IEnumerator AnimateHide()
		{
			if (m_VisibilityCoroutine != null)
				yield break;

			m_VisibilityState = VisibilityState.TransitioningOut;

			foreach (var face in m_MenuFaces)
				face.Hide();

			StartCoroutine(AnimateFrameReveal(m_VisibilityState));

			const float kTargetScale = 0f;
			float scale = transform.localScale.x;
			float smoothVelocity = 0f;
			while (!Mathf.Approximately(scale, kTargetScale))
			{
				transform.localScale = Vector3.one * scale;
				m_AlternateMenu.localScale = m_AlternateMenuOriginOriginalLocalScale * scale;
				scale = Mathf.SmoothDamp(scale, kTargetScale, ref smoothVelocity, 0.06875f, Mathf.Infinity, Time.unscaledDeltaTime);
				yield return null;
			}

			m_VisibilityState = VisibilityState.Hidden;
			transform.localScale = Vector3.zero;
			m_AlternateMenu.localScale = Vector3.zero;

			float snapRotation = GetRotationForFaceIndex(GetClosestFaceIndexForRotation(currentRotation));
			m_MenuFaceRotationOrigin.localRotation = Quaternion.Euler(new Vector3(0, snapRotation, 0)); // set intended target rotation
			m_RotationState = RotationState.AtRest;

			m_VisibilityCoroutine = null;
		}

		private IEnumerator AnimateFrameRotationShapeChange(RotationState rotationState)
		{
			float smoothTime = rotationState == RotationState.Rotating ? 0.5f : 0.0375f; // slower when rotating, faster when snapping
			float currentBlendShapeWeight = m_MenuFrameRenderer.GetBlendShapeWeight(0);
			float targetWeight = rotationState == RotationState.Rotating ? 100f : 0f;
			float smoothVelocity = 0f;
			while (m_RotationState == rotationState && !Mathf.Approximately(currentBlendShapeWeight, targetWeight))
			{
				currentBlendShapeWeight = Mathf.SmoothDamp(currentBlendShapeWeight, targetWeight, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
				m_MenuFrameRenderer.SetBlendShapeWeight(0, currentBlendShapeWeight);
				yield return null;
			}

			if (m_RotationState == rotationState)
				m_MenuFrameRenderer.SetBlendShapeWeight(0, targetWeight);
		}

		private IEnumerator AnimateFrameReveal(VisibilityState visibilityState)
		{
			m_MenuFrameRenderer.SetBlendShapeWeight(1, 100f);
			float smoothTime = visibilityState == VisibilityState.TransitioningIn ? 0.1875f : 0.09375f; // slower if transitioning in
			const float zeroStartBlendShapePadding = 20f; // start the blendShape at a point slightly above the full hidden value for better visibility
			float currentBlendShapeWeight = m_MenuFrameRenderer.GetBlendShapeWeight(1);
			float targetWeight = visibilityState == VisibilityState.TransitioningIn ? 0f : 100f;
			float smoothVelocity = 0f;
			const float kLerpEmphasisWeight = 0.25f;
			currentBlendShapeWeight = currentBlendShapeWeight > 0 ? currentBlendShapeWeight : zeroStartBlendShapePadding;

			while (m_VisibilityState != VisibilityState.Hidden && !Mathf.Approximately(currentBlendShapeWeight, targetWeight))
			{
				currentBlendShapeWeight = Mathf.SmoothDamp(currentBlendShapeWeight, targetWeight, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
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
		}

		private bool m_ActivatorButtonMoveAway;
		public bool activatorButtonMoveAway
		{
			get { return m_ActivatorButtonMoveAway; }
			set
			{
				if (m_ActivatorButtonMoveAway == value)
					return;

				m_ActivatorButtonMoveAway = value;

				if (m_ActivatorMoveCoroutine != null)
					StopCoroutine(m_ActivatorMoveCoroutine);

				m_ActivatorMoveCoroutine = StartCoroutine(AnimateMoveActivatorButton(m_ActivatorButtonMoveAway));
			}
		}

		private IEnumerator AnimateMoveActivatorButton(bool moveAway = true)
		{
			Debug.LogError("Move Activator Button out of the way of the radial menu here");
			
			float amount = 0f;
			Vector3 currentPosition = m_ActivatorTransform.localPosition;
			Vector3 targetPosition = moveAway == true ? m_AlternateActivatorLocalPosition : m_OriginalActivatorLocalPosition;
			float speed = (currentPosition.z / targetPosition.z) * (moveAway ? 10 : 3); // perform faster is returning to original position

			while (amount < 1f)
			{
				amount += Time.unscaledDeltaTime * speed;
				m_ActivatorTransform.localPosition = Vector3.Lerp(currentPosition, targetPosition, Mathf.SmoothStep(0f, 1f, amount));
				yield return null;
			}

			m_ActivatorTransform.localPosition = targetPosition;
		}
	}
}