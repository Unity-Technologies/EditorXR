#if UNITY_EDITOR && UNITY_EDITORVR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR
{
	partial class EditorVR
	{
		[SerializeField]
		Camera m_EventCameraPrefab;

		class UI : Nested
		{
			internal List<IManipulatorVisibility> manipulatorVisibilities { get { return m_ManipulatorVisibilities; } }
			readonly List<IManipulatorVisibility> m_ManipulatorVisibilities = new List<IManipulatorVisibility>();

			readonly HashSet<ISetManipulatorsVisible> m_ManipulatorsHiddenRequests = new HashSet<ISetManipulatorsVisible>();

			internal Camera eventCamera { get; private set; }

			internal void Initialize()
			{
				// Create event system, input module, and event camera
				ObjectUtils.AddComponent<EventSystem>(evr.gameObject);

				var inputModule = evr.AddModule<MultipleRayInputModule>();
				evr.m_InputModule = inputModule;
				evr.m_InputModule.getPointerLength = evr.m_DirectSelection.GetPointerLength;

				if (evr.m_CustomPreviewCamera != null)
					evr.m_InputModule.layerMask |= evr.m_CustomPreviewCamera.hmdOnlyLayerMask;

				eventCamera = ObjectUtils.Instantiate(evr.m_EventCameraPrefab.gameObject, evr.transform).GetComponent<Camera>();
				eventCamera.enabled = false;
				inputModule.eventCamera = eventCamera;

				inputModule.preProcessRaycastSource = evr.m_Rays.PreProcessRaycastSource;
			}

			internal GameObject InstantiateUI(GameObject prefab, Transform parent = null, bool worldPositionStays = true)
			{
				var go = ObjectUtils.Instantiate(prefab);
				go.transform.SetParent(parent ? parent : evr.transform, worldPositionStays);
				foreach (var canvas in go.GetComponentsInChildren<Canvas>())
					canvas.worldCamera = eventCamera;

				var keyboardModule = evr.m_KeyboardModule;
				foreach (var inputField in go.GetComponentsInChildren<InputField>())
				{
					if (inputField is NumericInputField)
						inputField.spawnKeyboard = keyboardModule.SpawnNumericKeyboard;
					else if (inputField is StandardInputField)
						inputField.spawnKeyboard = keyboardModule.SpawnAlphaNumericKeyboard;
				}

				foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
					evr.m_Interfaces.ConnectInterfaces(mb);

				return go;
			}

			internal void SetManipulatorsVisible(ISetManipulatorsVisible setter, bool visible)
			{
				if (visible)
					m_ManipulatorsHiddenRequests.Remove(setter);
				else
					m_ManipulatorsHiddenRequests.Add(setter);
			}

			internal void UpdateManipulatorVisibilites()
			{
				var manipulatorsVisible = m_ManipulatorsHiddenRequests.Count == 0;
				foreach (var mv in m_ManipulatorVisibilities)
					mv.manipulatorVisible = manipulatorsVisible;
			}
		}
	}
}
#endif
