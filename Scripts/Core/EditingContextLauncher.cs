using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine.Assertions;
using UnityEngine.InputNew;
using Valve.VR.InteractionSystem;

namespace UnityEditor.Experimental.EditorVR.Core
{
	public class EditingContextLauncher : MonoBehaviour
	{
		static EditingContextLauncher s_Instance;
		static InputManager s_InputManager;

		/// <summary>
		/// The context stack.  We hold game objects.  But all are expected to have a MonoBehavior that implements IEditingContext.
		/// </summary>
		List<IEditingContext> m_ContextStack = new List<IEditingContext>();
		
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

			var types = ObjectUtils.GetImplementationsOfInterface(typeof(IEditingContext));
			var searchString = "t: " + string.Join(" t: ", types.Select(t => t.FullName).ToArray());
			Debug.Log(searchString);
			var assets = AssetDatabase.FindAssets(searchString);

			assets.ForEach(a => Debug.Log(AssetDatabase.GUIDToAssetPath(a)));

			var defaultAsset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(assets[0]));
			var defaultContext = defaultAsset as IEditingContext;

			// for now, we leave the key binding in place and explicilty push EditorVR onto the stack.
			s_Instance.PushEditingContext(defaultContext);
		}

		static void OnVRViewDisabled()
		{
			ObjectUtils.Destroy(s_Instance.gameObject);
			ObjectUtils.Destroy(s_InputManager.gameObject);
		}

		[MenuItem("Window/EditorVR %e", false)]
		public static void ShowEditorVR()
		{
			// Using a utility window improves performance by saving from the overhead of DockArea.OnGUI()
			EditorWindow.GetWindow<VRView>(true, "EditorVR", true);
		}

		[MenuItem("Window/EditorVR %e", true)]
		public static bool ShouldShowEditorVR()
		{
			return PlayerSettings.virtualRealitySupported;
		}

		void OnDisable()
		{
			while (PeekEditingContext() != null)
				PopEditingContext();
		}

		/// <summary>
		/// Peek at the current editing context.
		/// </summary>
		/// <returns>The current editing context</returns>
		IEditingContext PeekEditingContext()
		{
			if (m_ContextStack.Count == 0)
				return null;

			return m_ContextStack[m_ContextStack.Count - 1];
		}

		//public GameObject PushEditingContext<T, C>(C config) where T : ScriptableObject, IEditingContext<C>
		//{
		//	var newContext = PushEditingContext<T>();
		//	newContext.GetComponent<T>().Configure(config);
		//	return newContext;
		//}

		public void PushEditingContext(IEditingContext context)
		{
			//if there is a current context, we subvert and deactivate it.
			var previousContext = PeekEditingContext();

			if (previousContext != null)
				previousContext.OnSuspendContext();
			
			//create the new context and add it to the stack.
			context.Setup();
			m_ContextStack.Add(context);
		}

		public void PopEditingContext()
		{
			IEditingContext poppedContext = PeekEditingContext();
			if (poppedContext != null)
			{
				poppedContext.Teardown();
				m_ContextStack.RemoveAt(m_ContextStack.Count - 1);
			}

			IEditingContext resumedContext = PeekEditingContext();
			if (resumedContext != null)
				resumedContext.OnResumeContext();
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
