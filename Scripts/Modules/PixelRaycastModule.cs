using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VR.Proxies;

namespace UnityEditor.VR.Modules
{
	public class PixelRaycastModule : MonoBehaviour
	{
		private readonly Dictionary<Transform, GameObject> m_RaycastGameObjects = new Dictionary<Transform, GameObject>(); // Stores which gameobject the proxys' ray origins are pointing at

		private GameObject[] m_IgnoreList;

		public Transform ignoreRoot { get; set; }

		/// <summary>
		/// Must be called from OnGUI. Does raycast from all ray origins in the given proxies that are active.
		/// </summary>
		/// <param name="proxies"></param> List of proxies to raycast from
		/// <param name="camera"></param> Camera to use for pixel based raycast (will be moved to the proxies' ray origins
		public void UpdateRaycast(Transform rayOrigin, Camera camera)
		{
			UpdateIgnoreList();
			m_RaycastGameObjects[rayOrigin] = Raycast(new Ray(rayOrigin.position, rayOrigin.forward), camera);
		}

		public GameObject GetFirstGameObject(Transform rayOrigin)
		{
			GameObject go;
			if (m_RaycastGameObjects.TryGetValue(rayOrigin, out go))
				return go;
			else
				Debug.LogError("Transform rayOrigin " + rayOrigin + " is not set to raycast from.");
			return null;
		}

		private void UpdateIgnoreList()
		{
			var children = ignoreRoot.GetComponentsInChildren<Transform>();
			m_IgnoreList = new GameObject[children.Length];
			for (int i = 0; i < children.Length; i++)
				m_IgnoreList[i] = children[i].gameObject;
		}

		private GameObject Raycast(Ray ray, Camera camera)
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