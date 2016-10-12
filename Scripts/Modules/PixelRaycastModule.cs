using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VR.Modules
{
	public class PixelRaycastModule : MonoBehaviour
	{
		class PixelRaycastHit
		{
			public GameObject gameObject;
			public bool direct;
		}

		readonly Dictionary<Transform, PixelRaycastHit> m_RaycastGameObjects = new Dictionary<Transform, PixelRaycastHit>(); // Stores which gameobject the proxys' ray origins are pointing at

		GameObject[] m_IgnoreList;

		public Transform ignoreRoot { get; set; }

		/// <summary>
		/// Must be called from OnGUI. Does raycast from given rayOrigin if its gameObject is active.
		/// </summary>
		/// <param name="rayOrigin"></param> rayOrigin to raycast from
		/// <param name="camera"></param> Camera to use for pixel based raycast (will be moved to the proxies' ray origins
		/// <param name="pointerLength"></param> Length of pointer used to determine direct selection
		/// <param name="direct"></param> Whether the object is close enough for direct selection
		public GameObject UpdateRaycast(Transform rayOrigin, Camera camera, float pointerLength)
		{
			if (!rayOrigin.gameObject.activeSelf)
				return null;

			UpdateIgnoreList();

			float distance;
			var result = Raycast(new Ray(rayOrigin.position, rayOrigin.forward), camera, out distance);

			m_RaycastGameObjects[rayOrigin] = new PixelRaycastHit
			{
				gameObject = result,
				direct = distance <= pointerLength
			};
			return result;
		}

		public GameObject GetFirstGameObject(Transform rayOrigin)
		{
			PixelRaycastHit hit;
			if (m_RaycastGameObjects.TryGetValue(rayOrigin, out hit))
				return hit.gameObject;
			return null;
		}

		public GameObject GetFirstGameObject(Transform rayOrigin, out bool direct)
		{
			PixelRaycastHit hit;
			if (m_RaycastGameObjects.TryGetValue(rayOrigin, out hit))
			{
				direct = hit.direct;
				return hit.gameObject;
			}
			direct = false;
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
#if UNITY_EDITOR
			camera.transform.position = ray.origin;
			camera.transform.forward = ray.direction;

			var restoreCamera = Camera.current;
			// HACK: Match Screen.width/height for scene picking
			camera.targetTexture = RenderTexture.GetTemporary(Screen.width, Screen.height);
			Camera.SetupCurrent(camera);

			var go = HandleUtility.PickGameObject(camera.pixelRect.center, false, m_IgnoreList);
			// Find the distance to the closest renderer to check for direct selection
			distance = float.MaxValue;
			if (go)
			{
				foreach (var renderer in go.GetComponentsInChildren<Renderer>())
				{
					float newDist;
					if (renderer.bounds.IntersectRay(ray, out newDist) && newDist > 0)
						distance = Mathf.Min(distance, newDist);
				}
			}

			Camera.SetupCurrent(restoreCamera);
			RenderTexture.ReleaseTemporary(camera.targetTexture);
			camera.targetTexture = null;

			return go;
#else
			return null;
#endif
		}
	}
}