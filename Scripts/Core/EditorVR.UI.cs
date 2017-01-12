#if !UNITY_EDITORVR
#pragma warning disable 67, 414, 649
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.UI;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEditor.Experimental.EditorVR
{
	internal partial class EditorVR : MonoBehaviour
	{
		[SerializeField]
		Camera m_EventCameraPrefab;

		[SerializeField]
		KeyboardMallet m_KeyboardMalletPrefab;

		[SerializeField]
		KeyboardUI m_NumericKeyboardPrefab;

		[SerializeField]
		KeyboardUI m_StandardKeyboardPrefab;

		readonly Dictionary<Transform, KeyboardMallet> m_KeyboardMallets = new Dictionary<Transform, KeyboardMallet>();
		KeyboardUI m_NumericKeyboard;
		KeyboardUI m_StandardKeyboard;

		readonly List<IManipulatorVisibility> m_ManipulatorVisibilities = new List<IManipulatorVisibility>();
		readonly HashSet<ISetManipulatorsVisible> m_ManipulatorsHiddenRequests = new HashSet<ISetManipulatorsVisible>();

		Camera m_EventCamera;

		DragAndDropModule m_DragAndDropModule;

#if UNITY_EDITORVR
		void CreateEventSystem()
		{
			// Create event system, input module, and event camera
			U.Object.AddComponent<EventSystem>(gameObject);

			m_InputModule = U.Object.AddComponent<MultipleRayInputModule>(gameObject);
			m_InputModule.getPointerLength = GetPointerLength;

			if (m_CustomPreviewCamera != null)
				m_InputModule.layerMask |= m_CustomPreviewCamera.hmdOnlyLayerMask;

			m_EventCamera = U.Object.Instantiate(m_EventCameraPrefab.gameObject, transform).GetComponent<Camera>();
			m_EventCamera.enabled = false;
			m_InputModule.eventCamera = m_EventCamera;

			m_InputModule.rayEntered += m_DragAndDropModule.OnRayEntered;
			m_InputModule.rayExited += m_DragAndDropModule.OnRayExited;
			m_InputModule.dragStarted += m_DragAndDropModule.OnDragStarted;
			m_InputModule.dragEnded += m_DragAndDropModule.OnDragEnded;

			m_InputModule.preProcessRaycastSource = PreProcessRaycastSource;

			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				// Create ui action map input for device.
				if (deviceData.uiInput == null)
				{
					deviceData.uiInput = CreateActionMapInput(m_InputModule.actionMap, device);
					deviceData.directSelectInput = CreateActionMapInput(m_DirectSelectActionMap, device);
				}

				// Add RayOrigin transform, proxy and ActionMapInput references to input module list of sources
				m_InputModule.AddRaycastSource(proxy, rayOriginPair.Key, deviceData.uiInput, rayOriginPair.Value, source =>
				{
					foreach (var miniWorld in m_MiniWorlds)
					{
						var targetObject = source.hoveredObject ? source.hoveredObject : source.draggedObject;
						if (miniWorld.Contains(source.rayOrigin.position))
						{
							if (targetObject && !targetObject.transform.IsChildOf(miniWorld.miniWorldTransform.parent))
								return false;
						}
					}

					return true;
				});
			}, false);
		}

		KeyboardUI SpawnNumericKeyboard()
		{
			if (m_StandardKeyboard != null)
				m_StandardKeyboard.gameObject.SetActive(false);

			// Check if the prefab has already been instantiated
			if (m_NumericKeyboard == null)
				m_NumericKeyboard = U.Object.Instantiate(m_NumericKeyboardPrefab.gameObject, U.Camera.GetViewerPivot()).GetComponent<KeyboardUI>();

			return m_NumericKeyboard;
		}

		KeyboardUI SpawnAlphaNumericKeyboard()
		{
			if (m_NumericKeyboard != null)
				m_NumericKeyboard.gameObject.SetActive(false);

			// Check if the prefab has already been instantiated
			if (m_StandardKeyboard == null)
				m_StandardKeyboard = U.Object.Instantiate(m_StandardKeyboardPrefab.gameObject, U.Camera.GetViewerPivot()).GetComponent<KeyboardUI>();

			return m_StandardKeyboard;
		}

		void UpdateKeyboardMallets()
		{
			foreach (var proxy in m_Proxies)
			{
				proxy.hidden = !proxy.active;
				if (proxy.active)
				{
					foreach (var rayOrigin in proxy.rayOrigins.Values)
					{
						var malletVisible = true;
						var numericKeyboardNull = false;
						var standardKeyboardNull = false;

						if (m_NumericKeyboard != null)
							malletVisible = m_NumericKeyboard.ShouldShowMallet(rayOrigin);
						else
							numericKeyboardNull = true;

						if (m_StandardKeyboard != null)
							malletVisible = malletVisible || m_StandardKeyboard.ShouldShowMallet(rayOrigin);
						else
							standardKeyboardNull = true;

						if (numericKeyboardNull && standardKeyboardNull)
							malletVisible = false;

						var mallet = m_KeyboardMallets[rayOrigin];

						if (mallet.visible != malletVisible)
						{
							mallet.visible = malletVisible;
							var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
							if (dpr)
							{
								if (malletVisible)
									dpr.Hide();
								else
									dpr.Show();
							}
						}

						// TODO remove this after physics are in
						mallet.CheckForKeyCollision();
					}
				}
			}
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
					inputField.spawnKeyboard = SpawnNumericKeyboard;
				else if (inputField is StandardInputField)
					inputField.spawnKeyboard = SpawnAlphaNumericKeyboard;
			}

			foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
				ConnectInterfaces(mb);

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
#endif
	}
}
