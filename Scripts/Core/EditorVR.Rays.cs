#if UNITY_EDITORVR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Manipulators;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.Proxies;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR
{
	partial class EditorVR
	{
		delegate void ForEachRayOriginCallback(IProxy proxy, KeyValuePair<Node, Transform> rayOriginPair, InputDevice device, DeviceData deviceData);

		const float kDefaultRayLength = 100f;

		[SerializeField]
		DefaultProxyRay m_ProxyRayPrefab;

		readonly Dictionary<Transform, DefaultProxyRay> m_DefaultRays = new Dictionary<Transform, DefaultProxyRay>();
        public List<IProxy> Proxies
        {
            get { return m_Proxies; }
        }
        readonly List<IProxy> m_Proxies = new List<IProxy>();

		MultipleRayInputModule m_InputModule;

		PixelRaycastModule m_PixelRaycastModule;
		bool m_UpdatePixelRaycastModule = true;
		bool m_PixelRaycastIgnoreListDirty = true;

		Transform m_LastSelectionRayOrigin;

		StandardManipulator m_StandardManipulator;
		ScaleManipulator m_ScaleManipulator;

		void UpdateRayForDevice(DeviceData deviceData, Transform rayOrigin)
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
			m_LastSelectionRayOrigin = rayOrigin;
		}

		private void CreateAllProxies()
		{
			foreach (Type proxyType in U.Object.GetImplementationsOfInterface(typeof(IProxy)))
			{
				IProxy proxy = U.Object.CreateGameObjectWithComponent(proxyType, VRView.viewerPivot) as IProxy;
				proxy.trackedObjectInput = m_TrackedObjectInput;
				foreach (var rayOriginPair in proxy.rayOrigins)
				{
					var rayOriginPairValue = rayOriginPair.Value;
					var rayTransform = U.Object.Instantiate(m_ProxyRayPrefab.gameObject, rayOriginPairValue).transform;
					rayTransform.position = rayOriginPairValue.position;
					rayTransform.rotation = rayOriginPairValue.rotation;
					m_DefaultRays.Add(rayOriginPairValue, rayTransform.GetComponent<DefaultProxyRay>());

					var malletTransform = U.Object.Instantiate(m_KeyboardMalletPrefab.gameObject, rayOriginPairValue).transform;
					malletTransform.position = rayOriginPairValue.position;
					malletTransform.rotation = rayOriginPairValue.rotation;
					var mallet = malletTransform.GetComponent<KeyboardMallet>();
					mallet.gameObject.SetActive(false);
					m_KeyboardMallets.Add(rayOriginPairValue, mallet);
				}

				m_Proxies.Add(proxy);
			}
		}

		void UpdateDefaultProxyRays()
		{
			// Set ray lengths based on renderer bounds
			foreach (var proxy in m_Proxies)
			{
				if (!proxy.active)
					continue;

				foreach (var rayOrigin in proxy.rayOrigins.Values)
				{
					var distance = kDefaultRayLength;

					// Give UI priority over scene objects (e.g. For the TransformTool, handles are generally inside of the
					// object, so visually show the ray terminating there instead of the object; UI is already given
					// priority on the input side)
					var uiEventData = m_InputModule.GetPointerEventData(rayOrigin);
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

		void ForEachRayOrigin(ForEachRayOriginCallback callback, bool activeOnly = true)
		{
			for (var i = 0; i < m_Proxies.Count; i++)
			{
				var proxy = m_Proxies[i];
				if (activeOnly && !proxy.active)
					continue;

				foreach (var rayOriginPair in proxy.rayOrigins)
				{
					var systemDevices = GetSystemDevices();
					for (int j = 0; j < systemDevices.Count; j++)
					{
						var device = systemDevices[j];
						// Find device tagged with the node that matches this RayOrigin node
						var node = GetDeviceNode(device);
						if (node.HasValue && node.Value == rayOriginPair.Key)
						{
							DeviceData deviceData;
							if (m_DeviceData.TryGetValue(device, out deviceData))
								callback(proxy, rayOriginPair, device, deviceData);

							break;
						}
					}
				}
			}
		}

		IProxy GetProxyForRayOrigin(Transform rayOrigin)
		{
			IProxy result = null;
			ForEachRayOrigin((proxy, rayOriginPair, device, deviceData) =>
			{
				if (rayOriginPair.Value == rayOrigin)
					result = proxy;
			});

			return result;
		}

		private GameObject GetFirstGameObject(Transform rayOrigin)
		{
			var go = m_PixelRaycastModule.GetFirstGameObject(rayOrigin);
			if (go)
				return go;

			// If a raycast did not find an object use the spatial hash as a final test
			if (m_IntersectionModule)
			{
				var tester = rayOrigin.GetComponentInChildren<IntersectionTester>();
				var renderer = m_IntersectionModule.GetIntersectedObjectForTester(tester);
				if (renderer && !renderer.CompareTag(kVRPlayerTag))
					return renderer.gameObject;
			}

			foreach (var ray in m_MiniWorldRays)
			{
				var miniWorldRay = ray.Value;
				if (miniWorldRay.originalRayOrigin.Equals(rayOrigin))
				{
					var tester = miniWorldRay.tester;
					if (!tester.active)
						continue;

#if ENABLE_MINIWORLD_RAY_SELECTION
					var miniWorldRayOrigin = ray.Key;
					go = m_PixelRaycastModule.GetFirstGameObject(miniWorldRayOrigin);
					if (go)
						return go;
#endif

					var renderer = m_IntersectionModule.GetIntersectedObjectForTester(tester);
					if (renderer)
						return renderer.gameObject;
				}
			}

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

		bool IsRayActive(Transform rayOrigin)
		{
			var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
			return dpr == null || dpr.rayVisible;
		}

		static void ShowRay(Transform rayOrigin, bool rayOnly = false)
		{
			var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
			if (dpr)
				dpr.Show(rayOnly);
		}

		static void HideRay(Transform rayOrigin, bool rayOnly = false)
		{
			var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
			if (dpr)
				dpr.Hide(rayOnly);
		}

		static bool LockRay(Transform rayOrigin, object obj)
		{
			var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
			return dpr && dpr.LockRay(obj);
		}

		static bool UnlockRay(Transform rayOrigin, object obj)
		{
			var dpr = rayOrigin.GetComponentInChildren<DefaultProxyRay>();
			return dpr && dpr.UnlockRay(obj);
		}

		void PreProcessRaycastSource(Transform rayOrigin)
		{
			var camera = U.Camera.GetMainCamera();
			var cameraPosition = camera.transform.position;
			var matrix = camera.worldToCameraMatrix;

#if ENABLE_MINIWORLD_RAY_SELECTION
			MiniWorldRay ray;
			if (m_MiniWorldRays.TryGetValue(rayOrigin, out ray))
				matrix = ray.miniWorld.getWorldToCameraMatrix(camera);
#endif

			if (!m_StandardManipulator)
				m_StandardManipulator = GetComponentInChildren<StandardManipulator>();

			if (m_StandardManipulator)
				m_StandardManipulator.AdjustScale(cameraPosition, matrix);

			if (!m_ScaleManipulator)
				m_ScaleManipulator = GetComponentInChildren<ScaleManipulator>();

			if (m_ScaleManipulator)
				m_ScaleManipulator.AdjustScale(cameraPosition, matrix);
		}
	}
}
#endif
