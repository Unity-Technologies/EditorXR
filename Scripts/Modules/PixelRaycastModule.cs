using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	public class PixelRaycastModule : MonoBehaviour
	{
		readonly Dictionary<Transform, GameObject> m_RaycastGameObjects = new Dictionary<Transform, GameObject>(); // Stores which gameobject the proxys' ray origins are pointing at

		GameObject[] m_IgnoreList;

		public Transform ignoreRoot { get; set; }

		/// <summary>
		/// Must be called from OnGUI. Updates pixel raycast result for given rayOrigin
		/// </summary>
		/// <param name="rayOrigin"></param> rayOrigin to raycast from
		/// <param name="camera"></param> Camera to use for pixel based raycast (will be moved to the proxies' ray origins
		public void UpdateRaycast(Transform rayOrigin, Camera camera)
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

		GameObject Raycast(Ray ray, Camera camera)
		{
#if UNITY_EDITOR
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
#else
			return null;
#endif
		}
	}
}