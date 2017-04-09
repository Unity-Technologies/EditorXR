using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputNew;
using Valve.VR.InteractionSystem;

namespace UnityEditor.Experimental.EditorVR.Core
{
	public class EditingContextLauncher : MonoBehaviour
	{
		const string k_DefaultContext = "EditorVR.DefaultContext";

		static EditingContextLauncher s_Instance;
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
					var foundContext = m_AvailableContexts.Find(c => ((Object)c).name == defaultContextName);
					if (foundContext != null)
						context = foundContext;
				}

				return context;
			}
			set
			{
				EditorPrefs.SetString(k_DefaultContext, ((Object)value).name); 
			}
		}


		static EditingContextLauncher()
		{
			VRView.viewEnabled += OnVRViewEnabled;
			VRView.viewDisabled += OnVRViewDisabled;
		}

		static void OnVRViewEnabled()
		{
			ObjectUtils.hideFlags = HideFlags.DontSave;

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
			var types = ObjectUtils.GetImplementationsOfInterface(typeof(IEditingContext));
			var searchString = "t: " + string.Join(" t: ", types.Select(t => t.FullName).ToArray());
			var assets = AssetDatabase.FindAssets(searchString);

			assets.ForEach(a =>
			{
				var assetPath = AssetDatabase.GUIDToAssetPath(a);
				var context = AssetDatabase.LoadMainAssetAtPath(assetPath) as IEditingContext;
				m_AvailableContexts.Add(context);
			});

			m_ContextNames = m_AvailableContexts.Select(c => ((Object)c).name).ToArray();

			var launchContext = defaultContext;
			PushEditingContext(launchContext);
			m_SelectedContextIndex = m_AvailableContexts.IndexOf(launchContext);

			if (m_AvailableContexts.Count > 1)
				VRView.afterOnGUI += OnVRViewGUI;
		}

		void OnDisable()
		{
			defaultContext = m_AvailableContexts[m_SelectedContextIndex];

			VRView.afterOnGUI -= OnVRViewGUI;

			PopAllEditingContexts();
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
						PopAllEditingContexts();
						PushEditingContext(m_AvailableContexts[m_SelectedContextIndex]);
						GUIUtility.ExitGUI();
					}
					GUILayout.FlexibleSpace();
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndArea();
		}

		IEditingContext PeekEditingContext()
		{
			if (m_ContextStack.Count == 0)
				return null;

			return m_ContextStack[m_ContextStack.Count - 1];
		}

		void PushEditingContext(IEditingContext context)
		{
			//if there is a current context, we subvert and deactivate it.
			var previousContext = PeekEditingContext();

			if (previousContext != null)
				previousContext.OnSuspendContext();
			
			//create the new context and add it to the stack.
			context.Setup();
			m_ContextStack.Add(context);
		}

		void PopEditingContext()
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

		void PopAllEditingContexts()
		{
			while (PeekEditingContext() != null)
				PopEditingContext();
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
