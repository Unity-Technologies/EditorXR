#if UNITY_EDITOR && UNITY_EDITORVR
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Core
{
	class EditingContextLauncher : MonoBehaviour
	{
		const string k_DefaultContext = "EditorVR.DefaultContext";

		internal static EditingContextLauncher s_Instance;
		static InputManager s_InputManager;

		List<IEditingContext> m_ContextStack = new List<IEditingContext>();

		List<IEditingContext> m_AvailableContexts = new List<IEditingContext>();
		string[] m_ContextNames;
		int m_SelectedContextIndex;

		IEditingContext defaultContext
		{
			get
			{
				var context = m_AvailableContexts.First();

				var defaultContextName = EditorPrefs.GetString(k_DefaultContext, string.Empty);
				if (!string.IsNullOrEmpty(defaultContextName))
				{
					var foundContext = m_AvailableContexts.Find(c => c.name == defaultContextName);
					if (foundContext != null)
						context = foundContext;
				}

				return context;
			}
			set
			{
				EditorPrefs.SetString(k_DefaultContext, value.name);
			}
		}


		static EditingContextLauncher()
		{
			VRView.viewEnabled += OnVRViewEnabled;
			VRView.viewDisabled += OnVRViewDisabled;
		}

		static void OnVRViewEnabled()
		{
			InitializeInputManager();
			s_Instance = ObjectUtils.CreateGameObjectWithComponent<EditingContextLauncher>();
		}

		static void OnVRViewDisabled()
		{
			ObjectUtils.Destroy(s_Instance.gameObject);
			ObjectUtils.Destroy(s_InputManager.gameObject);
		}

		[MenuItem("Window/EditorVR %e", false)]
		static void ShowEditorVR()
		{
			// Using a utility window improves performance by saving from the overhead of DockArea.OnGUI()
			EditorWindow.GetWindow<VRView>(true, "EditorVR", true);
		}

		[MenuItem("Window/EditorVR %e", true)]
		static bool ShouldShowEditorVR()
		{
			return PlayerSettings.virtualRealitySupported;
		}

		void OnEnable()
		{
			m_AvailableContexts = GetAllEditingContexts();
			m_ContextNames = m_AvailableContexts.Select(c => c.name).ToArray();

			if (m_AvailableContexts.Count > 1)
				VRView.afterOnGUI += OnVRViewGUI;
		}

		void OnDisable()
		{
			defaultContext = m_AvailableContexts[m_SelectedContextIndex];

			VRView.afterOnGUI -= OnVRViewGUI;

			PopAllEditingContexts();
		}

		void Start()
		{
			string errorMessage;
			var launchContext = defaultContext;
			if (PushEditingContext(launchContext, out errorMessage))
				m_SelectedContextIndex = m_AvailableContexts.IndexOf(launchContext);
			else
			{
				Debug.LogError(errorMessage);
				VRView.activeView.Close();
			}
		}

		void OnVRViewGUI(EditorWindow window)
		{
			var view = (VRView)window;
			GUILayout.BeginArea(view.guiRect);
			{
				GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				{
					m_SelectedContextIndex = EditorGUILayout.Popup(string.Empty, m_SelectedContextIndex, m_ContextNames);
					if (GUI.changed)
					{
						SetEditingContext(m_AvailableContexts[m_SelectedContextIndex]);
						GUIUtility.ExitGUI();
					}
					GUILayout.FlexibleSpace();
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndArea();
		}

		internal IEditingContext PeekEditingContext()
		{
			if (m_ContextStack.Count == 0)
				return null;

			return m_ContextStack[m_ContextStack.Count - 1];
		}

		internal bool PushEditingContext(IEditingContext context, out string errorMessage)
		{
			errorMessage = null;

			//if there is a current context, we subvert and deactivate it.
			var currentContext = PeekEditingContext();

			if (currentContext != null)
			{
				if (!currentContext.OnSuspendContext(out errorMessage))
					return false;
			}

			//create the new context and add it to the stack.
			context.Setup();
			m_ContextStack.Add(context);

			m_SelectedContextIndex = m_AvailableContexts.IndexOf(context);		

			return true;
		}

		internal void PopEditingContext()
		{
			IEditingContext poppedContext = PeekEditingContext();
			if (poppedContext != null)
			{
				poppedContext.Dispose();
				m_ContextStack.RemoveAt(m_ContextStack.Count - 1);
			}

			IEditingContext resumedContext = PeekEditingContext();
			if (resumedContext != null)
				resumedContext.OnResumeContext();
		}

		internal void SetEditingContext(IEditingContext context)
		{
			string errorMessage; // Popping from stack cannot be prevented
			PopAllEditingContexts();
			PushEditingContext(context, out errorMessage);
		}

		internal void PopAllEditingContexts()
		{
			while (PeekEditingContext() != null)
				PopEditingContext();
		}

		internal static List<IEditingContext> GetAllEditingContexts()
		{
			var types = ObjectUtils.GetImplementationsOfInterface(typeof(IEditingContext));
			var searchString = "t: " + string.Join(" t: ", types.Select(t => t.FullName).ToArray());
			var assets = AssetDatabase.FindAssets(searchString);

			var allContexts = new List<IEditingContext>();
			foreach (var asset in assets)
			{
				var assetPath = AssetDatabase.GUIDToAssetPath(asset);
				var context = AssetDatabase.LoadMainAssetAtPath(assetPath) as IEditingContext;
				allContexts.Add(context);
			};

			return allContexts;
		}

		static void InitializeInputManager()
		{
			// HACK: InputSystem has a static constructor that is relied upon for initializing a bunch of other components, so
			// in edit mode we need to handle lifecycle explicitly
			var managers = Resources.FindObjectsOfTypeAll<InputManager>();
			foreach (var m in managers)
			{
				ObjectUtils.Destroy(m.gameObject);
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
			s_InputManager.gameObject.hideFlags = ObjectUtils.hideFlags;
			ObjectUtils.SetRunInEditModeRecursively(s_InputManager.gameObject, true);

			// These components were allocating memory every frame and aren't currently used in EditorVR
			ObjectUtils.Destroy(s_InputManager.GetComponent<JoystickInputToEvents>());
			ObjectUtils.Destroy(s_InputManager.GetComponent<MouseInputToEvents>());
			ObjectUtils.Destroy(s_InputManager.GetComponent<KeyboardInputToEvents>());
			ObjectUtils.Destroy(s_InputManager.GetComponent<TouchInputToEvents>());
		}
	}
}
#endif
