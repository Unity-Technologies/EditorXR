#if UNITY_EDITOR && UNITY_EDITORVR
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Manipulators;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	partial class EditorVR
	{
		[SerializeField]
		DefaultProxyRay m_ProxyRayPrefab;

		class Rays : Nested
		{
			internal delegate void ForEachProxyDeviceCallback(DeviceData deviceData);

			const float k_DefaultRayLength = 100f;

			internal Dictionary<Transform, DefaultProxyRay> defaultRays { get { return m_DefaultRays; } }
			readonly Dictionary<Transform, DefaultProxyRay> m_DefaultRays = new Dictionary<Transform, DefaultProxyRay>();

			readonly List<IProxy> m_Proxies = new List<IProxy>();

			internal Transform lastSelectionRayOrigin { get; private set; }

			StandardManipulator m_StandardManipulator;
			ScaleManipulator m_ScaleManipulator;

			internal void UpdateRayForDevice(DeviceData deviceData, Transform rayOrigin)
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

			internal void SetLastSelectionRayOrigin(Transform rayOrigin)
			{
				lastSelectionRayOrigin = rayOrigin;
			}

			internal void CreateAllProxies()
			{
				foreach (var proxyType in ObjectUtils.GetImplementationsOfInterface(typeof(IProxy)))
				{
					var proxy = (IProxy)ObjectUtils.CreateGameObjectWithComponent(proxyType, VRView.cameraRig);
					proxy.trackedObjectInput = evr.m_DeviceInputModule.trackedObjectInput;
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
						var deviceInputModule = evr.m_DeviceInputModule;

						foreach (var rayOriginPair in proxy.rayOrigins)
						{
							var node = rayOriginPair.Key;

							var systemDevices = deviceInputModule.GetSystemDevices();
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
									deviceData.uiInput = deviceInputModule.CreateActionMapInput(evr.m_InputModule.actionMap, device);
									deviceData.directSelectInput = deviceInputModule.CreateActionMapInput(deviceInputModule.directSelectActionMap, device);

									// Add RayOrigin transform, proxy and ActionMapInput references to input module list of sources
									evr.m_InputModule.AddRaycastSource(proxy, node, deviceData.uiInput, rayOriginPair.Value, source =>
									{
										foreach (var miniWorld in evr.m_MiniWorlds.worlds)
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
								}
							}

							var rayOrigin = rayOriginPair.Value;
							var rayTransform = ObjectUtils.Instantiate(evr.m_ProxyRayPrefab.gameObject, rayOrigin).transform;
							rayTransform.position = rayOrigin.position;
							rayTransform.rotation = rayOrigin.rotation;
							var dpr = rayTransform.GetComponent<DefaultProxyRay>();
							dpr.getViewerScale = Viewer.GetViewerScale;
							dpr.SetColor(node == Node.LeftHand ? evr.m_HighlightModule.leftColor : evr.m_HighlightModule.rightColor);
							m_DefaultRays.Add(rayOrigin, dpr);

							evr.m_KeyboardModule.SpawnKeyboardMallet(rayOrigin);

							var proxyExtras = evr.m_ProxyExtras;
							if (proxyExtras)
							{
								var extraData = proxyExtras.data;
								List<GameObject> prefabs;
								if (extraData.TryGetValue(rayOriginPair.Key, out prefabs))
								{
									foreach (var prefab in prefabs)
									{
										var go = evr.m_UI.InstantiateUI(prefab);
										go.transform.SetParent(rayOriginPair.Value, false);
									}
								}
							}

							var tester = rayOriginPair.Value.GetComponentInChildren<IntersectionTester>();
							tester.active = proxy.active;
							evr.m_IntersectionModule.AddTester(tester);

							evr.m_HighlightModule.AddRayOriginForNode(node, rayOrigin);
						}

						evr.m_Tools.SpawnDefaultTools(proxy);
					}
				}
			}

			internal void UpdateDefaultProxyRays()
			{
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
						var uiEventData = evr.m_InputModule.GetPointerEventData(rayOrigin);
						if (uiEventData != null && uiEventData.pointerCurrentRaycast.isValid)
						{
							// Set ray length to distance to UI objects
							distance = uiEventData.pointerCurrentRaycast.distance;
						}
						else
						{
							// If not hitting UI, then check standard raycast and approximate bounds to set distance
							var go = GetFirstGameObject(rayOrigin);
							if (go != null)
							{
								var ray = new Ray(rayOrigin.position, rayOrigin.forward);
								var newDist = distance;
								foreach (var renderer in go.GetComponentsInChildren<Renderer>())
								{
									if (renderer.bounds.IntersectRay(ray, out newDist) && newDist > 0)
										distance = Mathf.Min(distance, newDist);
								}
							}
						}
						m_DefaultRays[rayOrigin].SetLength(distance);
					}
				}
			}

			internal void ForEachProxyDevice(ForEachProxyDeviceCallback callback, bool activeOnly = true)
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

			internal void ForEachRayOrigin(ForEachRayOriginCallback callback)
			{
				ForEachProxyDevice(deviceData => callback(deviceData.rayOrigin));
			}

			internal IProxy GetProxyForRayOrigin(Transform rayOrigin)
			{
				IProxy result = null;
				var deviceData = evr.m_DeviceData.FirstOrDefault(dd => dd.rayOrigin == rayOrigin);
				if (deviceData != null)
					result = deviceData.proxy;

				return result;
			}

			internal GameObject GetFirstGameObject(Transform rayOrigin)
			{
				var go = evr.m_PixelRaycastModule.GetFirstGameObject(rayOrigin);
				if (go)
					return go;

				var intersectionModule = evr.m_IntersectionModule;

				// If a raycast did not find an object use the spatial hash as a final test
				if (intersectionModule)
				{
					var tester = rayOrigin.GetComponentInChildren<IntersectionTester>();
					var renderer = intersectionModule.GetIntersectedObjectForTester(tester);
					if (renderer && !renderer.CompareTag(k_VRPlayerTag))
						return renderer.gameObject;
				}

				foreach (var ray in evr.m_MiniWorlds.rays)
				{
					var miniWorldRay = ray.Value;
					if (miniWorldRay.originalRayOrigin.Equals(rayOrigin))
					{
						var tester = miniWorldRay.tester;
						if (!tester.active)
							continue;

						var renderer = intersectionModule.GetIntersectedObjectForTester(tester);
						if (renderer)
							return renderer.gameObject;
					}
				}

				return null;
			}

			internal Transform GetPreviewOriginForRayOrigin(Transform rayOrigin)
			{
				foreach (var proxy in m_Proxies)
				{
					Transform previewOrigin;
					if (proxy.previewOrigins.TryGetValue(rayOrigin, out previewOrigin))
						return previewOrigin;
				}

				return null;
			}

			internal Transform GetFieldGrabOriginForRayOrigin(Transform rayOrigin)
			{
				foreach (var proxy in m_Proxies)
				{
					Transform fieldGrabOrigins;
					if (proxy.fieldGrabOrigins.TryGetValue(rayOrigin, out fieldGrabOrigins))
						return fieldGrabOrigins;
				}

				return null;
			}

			internal bool IsRayActive(Transform rayOrigin)
			{
				var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
				return dpr == null || dpr.rayVisible;
			}

			internal static void ShowRay(Transform rayOrigin, bool rayOnly = false)
			{
				var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
				if (dpr)
					dpr.Show(rayOnly);
			}

			internal static void HideRay(Transform rayOrigin, bool rayOnly = false)
			{
				var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
				if (dpr)
					dpr.Hide(rayOnly);
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

				if (m_StandardManipulator && m_StandardManipulator.adjustScaleForCamera)
					m_StandardManipulator.AdjustScale(cameraPosition, matrix);

				if (!m_ScaleManipulator)
					m_ScaleManipulator = evr.GetComponentInChildren<ScaleManipulator>();

				if (m_ScaleManipulator && m_ScaleManipulator.adjustScaleForCamera)
					m_ScaleManipulator.AdjustScale(cameraPosition, matrix);
			}
		}
	}
}

#endif
