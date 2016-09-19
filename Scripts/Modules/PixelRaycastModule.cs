using UnityEngine;
using System.Collections.Generic;

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
		public GameObject UpdateRaycast(Transform rayOrigin, Camera camera, float pointerLength = 0f)
		{
			UpdateIgnoreList();
			float distance = 0;
			GameObject result = null;
			result = rayOrigin.gameObject.activeSelf ? Raycast(new Ray(rayOrigin.position, rayOrigin.forward), camera, out distance) : null;

			// If a positive pointerLength is specified, use direct selection
			if (pointerLength > 0 && rayOrigin.gameObject.activeSelf)
			{
				if (pointerLength > 0 && distance > pointerLength)
					result = null;
			}
			m_RaycastGameObjects[rayOrigin] = result;
			return result;
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

		private GameObject Raycast(Ray ray, Camera camera, out float distance)
		{
			camera.transform.position = ray.origin;
			camera.transform.forward = ray.direction;

			var restoreCamera = Camera.current;
			// HACK: Match Screen.width/height for scene picking
			camera.targetTexture = RenderTexture.GetTemporary(Screen.width, Screen.height);
			Camera.SetupCurrent(camera);

			var go = HandleUtility.PickGameObject(camera.pixelRect.center, false, m_IgnoreList);
			distance = float.MaxValue;
			if (go)
			{
				foreach (var renderer in go.GetComponentsInChildren<Renderer>())
				{
					var newDist = 0f;
					if (renderer.bounds.IntersectRay(ray, out newDist) && newDist > 0)
						distance = Mathf.Min(distance, newDist);
				}
			}

			Camera.SetupCurrent(restoreCamera);
			RenderTexture.ReleaseTemporary(camera.targetTexture);
			camera.targetTexture = null;

			return go;
		}
	}
}