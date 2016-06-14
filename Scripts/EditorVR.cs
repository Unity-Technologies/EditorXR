using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Assertions;
using UnityEngine.InputNew;
using UnityEngine.VR.Proxies;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.VR;
#endif

[InitializeOnLoad]
public class EditorVR : MonoBehaviour
{
	public const HideFlags kDefaultHideFlags = HideFlags.DontSave;

	[SerializeField]
	private GameObject m_PlayerInputPrefab;
	private PlayerInput m_PlayerInput;

	void Awake()
	{
		m_PlayerInput = U.InstantiateAndSetActive(m_PlayerInputPrefab, transform).GetComponent<PlayerInput>();
		IProxy proxy = U.CreateGameObjectWithComponent<SixenseProxy>(transform);  //TODO change to proxy interface
		proxy.TrackedObjectInput = m_PlayerInput.GetActions<TrackedObject>();
	}

#if UNITY_EDITOR
	private static EditorVR s_Instance;
	private static InputManager s_InputManager;

	static EditorVR()
	{
		EditorVRView.onEnable += OnEVREnabled;
		EditorVRView.onDisable += OnEVRDisabled;
	}

	private static void OnEVREnabled()
	{
		// HACK: InputSystem has a static constructor that is relied upon for initializing a bunch of other components, so
		//	in edit mode we need to handle lifecycle explicitly
		InputManager[] managers = Resources.FindObjectsOfTypeAll<InputManager>();
		foreach (var m in managers)
		{
			U.Destroy(m.gameObject);
		}

		managers = Resources.FindObjectsOfTypeAll<InputManager>();
		if (managers.Length == 0)
		{
			// Attempt creating object hierarchy via an implicit static constructor call by touching the class
			InputSystem.ExecuteEvents();
			managers = Resources.FindObjectsOfTypeAll<InputManager>();

			if (managers.Length == 0)
			{
				typeof(InputSystem).TypeInitializer.Invoke(null, null);
				managers = Resources.FindObjectsOfTypeAll<InputManager>();
			}			
		}
		Assert.IsTrue(managers.Length == 1, "Only one InputManager should be active; Count: " + managers.Length);

		s_InputManager = managers[0];
		s_InputManager.gameObject.hideFlags = kDefaultHideFlags;
		U.SetRunInEditModeRecursively(s_InputManager.gameObject, true);

		s_Instance = U.CreateGameObjectWithComponent<EditorVR>();		
	}

	private static void OnEVRDisabled()
	{
		U.Destroy(s_Instance.gameObject);
		U.Destroy(s_InputManager.gameObject);
	}
#endif
}
