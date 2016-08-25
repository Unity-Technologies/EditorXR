using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.InputNew;
using UnityEngine.UI;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Menus
{
	public class MainMenu : MonoBehaviour, IMainMenu, IInstantiateUI, ICustomActionMap, ICustomRay, ILockableRay
	{
		public ActionMap actionMap
		{
			get
			{
				return m_MainMenuActionMap;
			}
		}
		[SerializeField]
		private ActionMap m_MainMenuActionMap;

		public ActionMapInput actionMapInput
		{
			get { return m_MainMenuInput; }
			set { m_MainMenuInput = (MainMenuInput) value; }
		}
		[SerializeField]
		private MainMenuInput m_MainMenuInput;

		// HACK: As of now Awake/Start get called together, so we have to cache the value and apply it later
		public Transform alternateMenuOrigin
		{
			get
			{
				return m_AlternateMenuOrigin;
			}
			set
			{
				m_AlternateMenuOrigin = value;
				if (m_MainMenuUI)
					m_MainMenuUI.alternateMenuOrigin = value;
			}
		}
		private Transform m_AlternateMenuOrigin;

		// HACK: As of now Awake/Start get called together, so we have to cache the value and apply it later
		public Transform menuOrigin
		{
			get { return m_MenuOrigin; }
			set
			{
				m_MenuOrigin = value;
				if (m_MainMenuUI)
					m_MainMenuUI.menuOrigin = value;
			}
		}
		private Transform m_MenuOrigin;
		
		[SerializeField]
		private MainMenuUI m_MainMenuPrefab;

		private MainMenuUI m_MainMenuUI;
		private float m_RotationInputStartTime;
		private float m_RotationInputStartValue;
		private float m_RotationInputIdleTime;
		private float m_LastRotationInput;
		
		public Func<GameObject, GameObject> instantiateUI { private get; set; }
		public Transform rayOrigin { private get; set; }
		public Action hideDefaultRay { private get; set; }
		public Action showDefaultRay { private get; set; }
		public Action<object> lockRay { private get; set; }
		public Action<object> unlockRay { private get; set; }
		public List<Type> menuTools { private get; set; }
		public Func<Node, Type, bool> selectTool { private get; set; }
		public Node? node { private get; set; }
		public Action setup { get { return Setup; } }

		public bool visible
		{
			get { return m_MainMenuUI.visible; }
			set
			{
				if (m_MainMenuUI.visible != value)
				{
					m_MainMenuUI.visible = value;
					if (value)
					{
						hideDefaultRay();
						lockRay(this);
					}
					else
					{
						unlockRay(this);
						showDefaultRay();
					}
				}
			}
		}

		public void Setup()
		{
			m_MainMenuUI = instantiateUI(m_MainMenuPrefab.gameObject).GetComponent<MainMenuUI>();
			m_MainMenuUI.instantiateUI = instantiateUI;
			m_MainMenuUI.alternateMenuOrigin = alternateMenuOrigin;
			m_MainMenuUI.menuOrigin = menuOrigin;
			m_MainMenuUI.Setup();

			CreateToolButtons(menuTools);
		}

		private void Update()
		{
			var rotationInput = m_MainMenuInput.rotate.rawValue;
			if (Mathf.Approximately(rotationInput, m_LastRotationInput) && Mathf.Approximately(rotationInput, 0f))
			{
				m_RotationInputIdleTime += Time.unscaledDeltaTime;
			}
			else
			{
				const float kFlickDeltaThreshold = 0.5f;
				const float kRotationInputIdleDurationThreshold = 0.05f; // Limits how often a flick can happen

				// Track values for a new rotation when input has changed
				if (m_RotationInputIdleTime > kRotationInputIdleDurationThreshold)
				{
					m_RotationInputStartTime = Time.realtimeSinceStartup;
					// Low sampling can affect our latch value, so sometimes the last rotation is a better choice because
					// the current rotation may be high by the time it is sampled
					m_RotationInputStartValue = Mathf.Abs(rotationInput) < Mathf.Abs(m_LastRotationInput) ? rotationInput : m_LastRotationInput;
				}

				const float kFlickDurationThreshold = 0.3f;

				// Perform a quick single face rotation if a quick flick of the input axis occurred
				float flickRotation = rotationInput - m_RotationInputStartValue;
				if (Mathf.Abs(flickRotation) >= kFlickDeltaThreshold
					&& (Time.realtimeSinceStartup - m_RotationInputStartTime) < kFlickDurationThreshold)
				{
					m_MainMenuUI.targetFaceIndex = m_MainMenuUI.targetFaceIndex + (int) Mathf.Sign(flickRotation);

					// Don't allow another flick until rotation resets
					m_RotationInputStartTime = 0f;
				}
				else
				{
					const float kRotationSpeed = 250;

					// Otherwise, apply manual rotation to the main menu faces
					m_MainMenuUI.targetRotation += rotationInput * kRotationSpeed * Time.unscaledDeltaTime;
				}

				// Reset the idle time if we are no longer idle (i.e. rotation is happening)
				m_RotationInputIdleTime = 0f;
			}

			m_LastRotationInput = rotationInput;
		}

		private void OnDisable()
		{
			unlockRay(this);
		}

		private void OnDestroy()
		{
			U.Object.Destroy(m_MainMenuUI.gameObject);

			unlockRay(this);
			showDefaultRay();
		}

		private void CreateToolButtons(List<Type> toolTypes)
		{
			foreach (var type in toolTypes)
			{
				var buttonData = new MainMenuUI.ButtonData();
				buttonData.name = type.Name;

				var customMenuAttribute = (MainMenuItemAttribute)type.GetCustomAttributes(typeof(MainMenuItemAttribute), false).FirstOrDefault();
				if (customMenuAttribute != null)
				{
					buttonData.name = customMenuAttribute.name;
					buttonData.sectionName = customMenuAttribute.sectionName;
					buttonData.description = customMenuAttribute.description;
				}

				m_MainMenuUI.CreateToolButton(buttonData, (b) =>
				{
					b.onClick.RemoveAllListeners();
					b.onClick.AddListener(() =>
					{
						if (visible)
							selectTool(node.Value, type);
					});
					b.onClick.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
				});
			}

			m_MainMenuUI.SetupMenuFaces();
		}
	}
}