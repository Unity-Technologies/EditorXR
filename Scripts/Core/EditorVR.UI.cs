#if UNITY_EDITORVR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.UI;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEditor.Experimental.EditorVR
{
	partial class EditorVR
	{
		[SerializeField]
		Camera m_EventCameraPrefab;

		readonly List<IManipulatorVisibility> m_ManipulatorVisibilities = new List<IManipulatorVisibility>();
		readonly HashSet<ISetManipulatorsVisible> m_ManipulatorsHiddenRequests = new HashSet<ISetManipulatorsVisible>();

		Camera m_EventCamera;

		void InitializeInputModule()
		{
			// Create event system, input module, and event camera
			U.Object.AddComponent<EventSystem>(gameObject);

			m_InputModule = AddModule<MultipleRayInputModule>();
			m_InputModule.getPointerLength = m_DirectSelection.GetPointerLength;

			if (m_CustomPreviewCamera != null)
				m_InputModule.layerMask |= m_CustomPreviewCamera.hmdOnlyLayerMask;

			m_EventCamera = U.Object.Instantiate(m_EventCameraPrefab.gameObject, transform).GetComponent<Camera>();
			m_EventCamera.enabled = false;
			m_InputModule.eventCamera = m_EventCamera;

			m_InputModule.preProcessRaycastSource = PreProcessRaycastSource;
		}

		GameObject InstantiateUI(GameObject prefab, Transform parent = null, bool worldPositionStays = true)
		{
			var go = U.Object.Instantiate(prefab);
			go.transform.SetParent(parent ? parent : transform, worldPositionStays);
			foreach (var canvas in go.GetComponentsInChildren<Canvas>())
				canvas.worldCamera = m_EventCamera;

			foreach (var inputField in go.GetComponentsInChildren<InputField>())
			{
				if (inputField is NumericInputField)
					inputField.spawnKeyboard = m_KeyboardModule.SpawnNumericKeyboard;
				else if (inputField is StandardInputField)
					inputField.spawnKeyboard = m_KeyboardModule.SpawnAlphaNumericKeyboard;
			}

			foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
				m_Interfaces.ConnectInterfaces(mb);

			return go;
		}

		void SetManipulatorsVisible(ISetManipulatorsVisible setter, bool visible)
		{
			if (visible)
				m_ManipulatorsHiddenRequests.Remove(setter);
			else
				m_ManipulatorsHiddenRequests.Add(setter);
		}

		void UpdateManipulatorVisibilites()
		{
			var manipulatorsVisible = m_ManipulatorsHiddenRequests.Count == 0;
			foreach (var mv in m_ManipulatorVisibilities)
				mv.manipulatorVisible = manipulatorsVisible;
		}
	}
}
#endif
