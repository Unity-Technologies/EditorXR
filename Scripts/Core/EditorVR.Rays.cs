#if UNITY_EDITOR && UNITY_EDITORVR
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Manipulators;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	partial class EditorVR
	{
		[SerializeField]
		DefaultProxyRay m_ProxyRayPrefab;

		[SerializeField]
		ProxyExtras m_ProxyExtras;

		class Rays : Nested, IInterfaceConnector
		{
			internal delegate void ForEachProxyDeviceCallback(DeviceData deviceData);

			const float k_DefaultRayLength = 100f;

			internal Dictionary<Transform, DefaultProxyRay> defaultRays { get { return m_DefaultRays; } }
			readonly Dictionary<Transform, DefaultProxyRay> m_DefaultRays = new Dictionary<Transform, DefaultProxyRay>();

			readonly List<IProxy> m_Proxies = new List<IProxy>();

			StandardManipulator m_StandardManipulator;
			ScaleManipulator m_ScaleManipulator;

			internal Transform lastSelectionRayOrigin { get; private set; }

			public Rays()
			{
				ICustomRayMethods.showDefaultRay = ShowRay;
				ICustomRayMethods.hideDefaultRay = HideRay;

				IUsesRayLockingMethods.lockRay = LockRay;
				IUsesRayLockingMethods.unlockRay = UnlockRay;

				IForEachRayOriginMethods.forEachRayOrigin = ForEachRayOrigin;
				IGetFieldGrabOriginMethods.getFieldGrabOriginForRayOrigin = GetFieldGrabOriginForRayOrigin;
				IGetPreviewOriginMethods.getPreviewOriginForRayOrigin = GetPreviewOriginForRayOrigin;
				IUsesRaycastResultsMethods.getFirstGameObject = GetFirstGameObject;
			}

			internal override void OnDestroy()
			{
				foreach (var proxy in m_Proxies)
					ObjectUtils.Destroy(((MonoBehaviour)proxy).gameObject);
			}

			public void ConnectInterface(object obj, Transform rayOrigin = null)
			{
				if (rayOrigin)
				{
					var evrDeviceData = evr.m_DeviceData;

					var ray = obj as IUsesRayOrigin;
					if (ray != null)
						ray.rayOrigin = rayOrigin;

					var deviceData = evrDeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);

					var handedRay = obj as IUsesNode;
					if (handedRay != null && deviceData != null)
						handedRay.node = deviceData.node;

					var usesProxy = obj as IUsesProxyType;
					if (usesProxy != null && deviceData != null)
						usesProxy.proxyType = deviceData.proxy.GetType();

					var menuOrigins = obj as IUsesMenuOrigins;
					if (menuOrigins != null)
					{
						Transform mainMenuOrigin;
						var proxy = GetProxyForRayOrigin(rayOrigin);
						if (proxy != null && proxy.menuOrigins.TryGetValue(rayOrigin, out mainMenuOrigin))
						{
							menuOrigins.menuOrigin = mainMenuOrigin;
							Transform alternateMenuOrigin;
							if (proxy.alternateMenuOrigins.TryGetValue(rayOrigin, out alternateMenuOrigin))
								menuOrigins.alternateMenuOrigin = alternateMenuOrigin;
						}
					}
				}

				var selectionModule = obj as SelectionModule;
				if (selectionModule)
				{
					selectionModule.selected += SetLastSelectionRayOrigin; // when a selection occurs in the selection tool, call show in the alternate menu, allowing it to show/hide itself.
					selectionModule.getGroupRoot = GetGroupRoot;
					selectionModule.overrideSelectObject = OverrideSelectObject;
				}
			}

			public void DisconnectInterface(object obj)
			{
			}

			internal static void UpdateRayForDevice(DeviceData deviceData, Transform rayOrigin)
			{
				var mainMenu = deviceData.mainMenu;
				var customMenu = deviceData.customMenu;
				if (mainMenu.visible || (customMenu != null && customMenu.visible))
				{
					HideRay(rayOrigin);
					LockRay(rayOrigin, mainMenu);
				}
				else
				{
					UnlockRay(rayOrigin, mainMenu);
					ShowRay(rayOrigin);
				}
			}

			void SetLastSelectionRayOrigin(Transform rayOrigin)
			{
				lastSelectionRayOrigin = rayOrigin;
			}

			static GameObject GetGroupRoot(GameObject hoveredObject)
			{
				if (!hoveredObject)
					return null;

				var groupRoot = PrefabUtility.FindPrefabRoot(hoveredObject);

				return groupRoot;
			}

			static bool OverrideSelectObject(GameObject hoveredObject)
			{
				// The player head can hovered, but not selected (only directly manipulated)
				if (hoveredObject && hoveredObject.CompareTag(k_VRPlayerTag))
				{
					// Clear the selection so that we do not manipulate it when moving the player head
					Selection.activeObject = null;
					return true;
				}

				return false;
			}

			internal void CreateAllProxies()
			{
				var deviceInputModule = evr.GetModule<DeviceInputModule>();
				foreach (var proxyType in ObjectUtils.GetImplementationsOfInterface(typeof(IProxy)))
				{
					var proxy = (IProxy)ObjectUtils.CreateGameObjectWithComponent(proxyType, VRView.cameraRig, false);
					proxy.trackedObjectInput = deviceInputModule.trackedObjectInput;
					proxy.activeChanged += () => OnProxyActiveChanged(proxy);
					proxy.hidden = true;

					m_Proxies.Add(proxy);
				}
			}

			void OnProxyActiveChanged(IProxy proxy)
			{
				proxy.hidden = !proxy.active;

				if (proxy.active)
				{
					var evrDeviceData = evr.m_DeviceData;
					if (!evrDeviceData.Any(dd => dd.proxy == proxy))
					{
						var inputModule = evr.GetModule<MultipleRayInputModule>();
						var deviceInputModule = evr.GetModule<DeviceInputModule>();
						var keyboardModule = evr.GetModule<KeyboardModule>();
						var highlightModule = evr.GetModule<HighlightModule>();
						var workspaceModule = evr.GetModule<WorkspaceModule>();
						var intersectionModule = evr.GetModule<IntersectionModule>();
						var ui = evr.GetNestedModule<UI>();

						foreach (var rayOriginPair in proxy.rayOrigins)
						{
							var node = rayOriginPair.Key;

							var systemDevices = deviceInputModule.GetSystemDevices();
							var actionMap = inputModule.actionMap;
							for (int j = 0; j < systemDevices.Count; j++)
							{
								var device = systemDevices[j];

								// Find device tagged with the node that matches this RayOrigin node
								var deviceNode = deviceInputModule.GetDeviceNode(device);
								if (deviceNode.HasValue && deviceNode.Value == node)
								{
									var deviceData = new DeviceData();
									evrDeviceData.Add(deviceData);
									deviceData.proxy = proxy;
									deviceData.node = node;
									deviceData.rayOrigin = rayOriginPair.Value;
									deviceData.inputDevice = device;
									deviceData.uiInput = deviceInputModule.CreateActionMapInput(actionMap, device);
									deviceData.directSelectInput = deviceInputModule.CreateActionMapInput(deviceInputModule.directSelectActionMap, device);

									// Add RayOrigin transform, proxy and ActionMapInput references to input module list of sources
									inputModule.AddRaycastSource(proxy, node, deviceData.uiInput, rayOriginPair.Value);
								}
							}

							var rayOrigin = rayOriginPair.Value;
							rayOrigin.name = string.Format("{0} Ray Origin", node);
							var rayTransform = ObjectUtils.Instantiate(evr.m_ProxyRayPrefab.gameObject, rayOrigin).transform;
							rayTransform.position = rayOrigin.position;
							rayTransform.rotation = rayOrigin.rotation;
							var dpr = rayTransform.GetComponent<DefaultProxyRay>();
							dpr.SetColor(node == Node.LeftHand ? highlightModule.leftColor : highlightModule.rightColor);
							m_DefaultRays.Add(rayOrigin, dpr);

							keyboardModule.SpawnKeyboardMallet(rayOrigin);

							var proxyExtras = evr.m_ProxyExtras;
							if (proxyExtras)
							{
								var extraData = proxyExtras.data;
								List<GameObject> prefabs;
								if (extraData.TryGetValue(rayOriginPair.Key, out prefabs))
								{
									foreach (var prefab in prefabs)
									{
										var go = ui.InstantiateUI(prefab);
										go.transform.SetParent(rayOriginPair.Value, false);
									}
								}
							}

							var tester = rayOriginPair.Value.GetComponentInChildren<IntersectionTester>();
							tester.active = proxy.active;
							intersectionModule.AddTester(tester);

							highlightModule.AddRayOriginForNode(node, rayOrigin);

							switch (node)
							{
								case Node.LeftHand:
									workspaceModule.leftRayOrigin = rayOrigin;
									break;
								case Node.RightHand:
									workspaceModule.rightRayOrigin = rayOrigin;
									break;
							}
						}

						Tools.SpawnDefaultTools(proxy);
					}
				}
			}

			internal static void UpdateRaycasts()
			{
				var intersectionModule = evr.GetModule<IntersectionModule>();
				var distance = k_DefaultRayLength * Viewer.GetViewerScale();
				ForEachRayOrigin(rayOrigin => { intersectionModule.UpdateRaycast(rayOrigin, distance); });
			}

			internal void UpdateDefaultProxyRays()
			{
				var intersectionModule = evr.GetModule<IntersectionModule>();
				var inputModule = evr.GetModule<MultipleRayInputModule>();

				// Set ray lengths based on renderer bounds
				foreach (var proxy in m_Proxies)
				{
					if (!proxy.active)
						continue;

					foreach (var rayOrigin in proxy.rayOrigins.Values)
					{
						var distance = k_DefaultRayLength * Viewer.GetViewerScale();

						// Give UI priority over scene objects (e.g. For the TransformTool, handles are generally inside of the
						// object, so visually show the ray terminating there instead of the object; UI is already given
						// priority on the input side)
						var uiEventData = inputModule.GetPointerEventData(rayOrigin);
						if (uiEventData != null && uiEventData.pointerCurrentRaycast.isValid)
						{
							// Set ray length to distance to UI objects
							distance = uiEventData.pointerCurrentRaycast.distance;
						}
						else
						{
							float hitDistance;
							if (intersectionModule.GetFirstGameObject(rayOrigin, out hitDistance))
								distance = hitDistance;
						}

						m_DefaultRays[rayOrigin].SetLength(distance);
					}
				}
			}

			internal static void ForEachProxyDevice(ForEachProxyDeviceCallback callback, bool activeOnly = true)
			{
				var evrDeviceData = evr.m_DeviceData;
				for (var i = 0; i < evrDeviceData.Count; i++)
				{
					var deviceData = evrDeviceData[i];
					var proxy = deviceData.proxy;
					if (activeOnly && !proxy.active)
						continue;

					callback(deviceData);
				}
			}

			static void ForEachRayOrigin(ForEachRayOriginCallback callback)
			{
				ForEachProxyDevice(deviceData => callback(deviceData.rayOrigin));
			}

			static IProxy GetProxyForRayOrigin(Transform rayOrigin)
			{
				IProxy result = null;
				var deviceData = evr.m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
				if (deviceData != null)
					result = deviceData.proxy;

				return result;
			}

			static GameObject GetFirstGameObject(Transform rayOrigin)
			{
				var intersectionModule = evr.GetModule<IntersectionModule>();

				float distance;
				var go = intersectionModule.GetFirstGameObject(rayOrigin, out distance);
				if (go)
					return go;

				// If a raycast did not find an object use the spatial hash as a final test
				var tester = rayOrigin.GetComponentInChildren<IntersectionTester>();
				var renderer = intersectionModule.GetIntersectedObjectForTester(tester);
				if (renderer && !renderer.CompareTag(k_VRPlayerTag))
					return renderer.gameObject;

				var enumerator = evr.GetNestedModule<MiniWorlds>().rays.GetEnumerator();
				while(enumerator.MoveNext())
				{
					var miniWorldRay = enumerator.Current.Value;
					if (miniWorldRay.originalRayOrigin.Equals(rayOrigin))
					{
						tester = miniWorldRay.tester;
						if (!tester.active)
							continue;

						renderer = intersectionModule.GetIntersectedObjectForTester(tester);
						if (renderer)
							return renderer.gameObject;
					}
				}
				enumerator.Dispose();

				return null;
			}

			Transform GetPreviewOriginForRayOrigin(Transform rayOrigin)
			{
				foreach (var proxy in m_Proxies)
				{
					Transform previewOrigin;
					if (proxy.previewOrigins.TryGetValue(rayOrigin, out previewOrigin))
						return previewOrigin;
				}

				return null;
			}

			Transform GetFieldGrabOriginForRayOrigin(Transform rayOrigin)
			{
				foreach (var proxy in m_Proxies)
				{
					Transform fieldGrabOrigins;
					if (proxy.fieldGrabOrigins.TryGetValue(rayOrigin, out fieldGrabOrigins))
						return fieldGrabOrigins;
				}

				return null;
			}

			internal static bool IsRayActive(Transform rayOrigin)
			{
				var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
				return dpr == null || dpr.rayVisible;
			}

			internal static void ShowRay(Transform rayOrigin, bool rayOnly = false)
			{
				if (rayOrigin)
				{
					var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
					if (dpr)
						dpr.Show(rayOnly);
				}
			}

			internal static void HideRay(Transform rayOrigin, bool rayOnly = false)
			{
				if (rayOrigin)
				{
					var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
					if (dpr)
						dpr.Hide(rayOnly);
				}
			}

			internal static bool LockRay(Transform rayOrigin, object obj)
			{
				var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
				return dpr && dpr.LockRay(obj);
			}

			internal static bool UnlockRay(Transform rayOrigin, object obj)
			{
				var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
				return dpr && dpr.UnlockRay(obj);
			}

			internal void PreProcessRaycastSource(Transform rayOrigin)
			{
				var camera = CameraUtils.GetMainCamera();
				var cameraPosition = camera.transform.position;
				var matrix = camera.worldToCameraMatrix;

				if (!m_StandardManipulator)
					m_StandardManipulator = evr.GetComponentInChildren<StandardManipulator>();

				if (m_StandardManipulator)
					m_StandardManipulator.AdjustScale(cameraPosition, matrix);

				if (!m_ScaleManipulator)
					m_ScaleManipulator = evr.GetComponentInChildren<ScaleManipulator>();

				if (m_ScaleManipulator)
					m_ScaleManipulator.AdjustScale(cameraPosition, matrix);
			}
		}
	}
}
#endif
