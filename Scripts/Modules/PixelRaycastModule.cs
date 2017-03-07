#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Core;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	class PixelRaycastModule : MonoBehaviour, IForEachRayOrigin
	{
		readonly Dictionary<Transform, GameObject> m_RaycastGameObjects = new Dictionary<Transform, GameObject>(); // Stores which gameobject the proxys' ray origins are pointing at

		GameObject[] m_IgnoreList;

		bool m_IgnoreListDirty = true;
		bool m_UpdateRaycasts = true;

		public Transform ignoreRoot { get; set; }
		public Camera raycastCamera { private get; set; }

		public Action<ForEachRayOriginCallback> forEachRayOrigin { get; set; }

		void OnEnable()
		{
			EditorApplication.hierarchyWindowChanged += OnHierarchyChanged;
			VRView.onGUIDelegate += OnSceneGUI;
		}

		void OnDisable()
		{
			EditorApplication.hierarchyWindowChanged -= OnHierarchyChanged;
			VRView.onGUIDelegate -= OnSceneGUI;
		}

		void Update()
		{
			// HACK: Send a custom event, so that OnSceneGUI gets called, which is requirement for scene picking to occur
			//		Additionally, on some machines it's required to do a delay call otherwise none of this works
			//		I noticed that delay calls were queuing up, so it was necessary to protect against that, so only one is processed
			if (m_UpdateRaycasts)
			{
				EditorApplication.delayCall += () =>
				{
					if (this != null) // Because this is a delay call, the component will be null when EditorVR closes
					{
						Event e = new Event();
						e.type = EventType.ExecuteCommand;
						VRView.activeView.SendEvent(e);
					}
				};

				m_UpdateRaycasts = false; // Don't allow another one to queue until the current one is processed
			}
		}

		/// <summary>
		/// Must be called from OnGUI. Updates pixel raycast result for given rayOrigin
		/// </summary>
		/// <param name="rayOrigin"></param> rayOrigin to raycast from
		/// <param name="camera"></param> Camera to use for pixel based raycast (will be moved to the proxies' ray origins
		void UpdateRaycast(Transform rayOrigin, Camera camera)
		{
			m_RaycastGameObjects[rayOrigin] = Raycast(new Ray(rayOrigin.position, rayOrigin.forward), camera);
		}

		/// <summary>
		/// Get the GameObject over which a particular ray is hovering
		/// </summary>
		/// <param name="rayOrigin">rayOrigin to check against</param>
		/// <returns></returns>
		public GameObject GetFirstGameObject(Transform rayOrigin)
		{
			GameObject go;
			if (m_RaycastGameObjects.TryGetValue(rayOrigin, out go))
				return go;

			return null;
		}

		/// <summary>
		/// Update the list of objects that ignore raycasts. This list will include EditorVR and all of its children
		/// </summary>
		void UpdateIgnoreList()
		{
			var children = ignoreRoot.GetComponentsInChildren<Transform>(true);
			m_IgnoreList = new GameObject[children.Length];
			for (int i = 0; i < children.Length; i++)
				m_IgnoreList[i] = children[i].gameObject;
		}

		void OnHierarchyChanged()
		{
			m_IgnoreListDirty = true;
		}

		public void OnSceneGUI(EditorWindow obj)
		{
			if (Event.current.type == EventType.ExecuteCommand)
			{
				if (m_IgnoreListDirty)
				{
					UpdateIgnoreList();
					m_IgnoreListDirty = false;
				}

				forEachRayOrigin(rayOrigin => UpdateRaycast(rayOrigin, raycastCamera));

				// Queue up the next round
				m_UpdateRaycasts = true;

				Event.current.Use();
			}
		}

		GameObject Raycast(Ray ray, Camera camera)
		{
			camera.transform.position = ray.origin;
			camera.transform.forward = ray.direction;

			var restoreCamera = Camera.current;
			// HACK: Match Screen.width/height for scene picking
			camera.targetTexture = RenderTexture.GetTemporary(Screen.width, Screen.height);
			Camera.SetupCurrent(camera);

			var go = HandleUtility.PickGameObject(camera.pixelRect.center, false, m_IgnoreList);

			Camera.SetupCurrent(restoreCamera);
			RenderTexture.ReleaseTemporary(camera.targetTexture);
			camera.targetTexture = null;

			return go;
		}
	}
}
#endif
