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
		/// Must be called from OnGUI. Does raycast from given rayOrigin if its gameObject is active.
		/// </summary>
		/// <param name="rayOrigin"></param> rayOrigin to raycast from
		/// <param name="camera"></param> Camera to use for pixel based raycast (will be moved to the proxies' ray origins
		/// <param name="pointerLength"></param> Length of pointer used for direct selection. If zero any raycast result is returned
		public GameObject UpdateRaycast(Transform rayOrigin, Camera camera, float pointerLength = 0f)
		{
			if (!rayOrigin.gameObject.activeSelf)
			{
				m_RaycastGameObjects[rayOrigin] = null;
				return null;
			}

			float distance;
			var result = Raycast(new Ray(rayOrigin.position, rayOrigin.forward), camera, out distance);

			// If a positive pointerLength is specified, use direct selection
			if (pointerLength > 0 && rayOrigin.gameObject.activeSelf)
			{
				if (pointerLength > 0 && distance > pointerLength)
					result = null;
			}
			m_RaycastGameObjects[rayOrigin] = result;
			return result;
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
			else
				Debug.LogError("Transform rayOrigin " + rayOrigin + " is not set to raycast from.");
			return null;
		}

		/// <summary>
		/// Update the list of objects that ignore raycasts. This list will include EditorVR and all of its children
		/// </summary>
		public void UpdateIgnoreList()
		{
			var children = ignoreRoot.GetComponentsInChildren<Transform>(true);
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
		}
	}
}