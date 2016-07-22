using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.VR;
using UnityEngine.VR.Proxies;

public class PixelRaycastModule : MonoBehaviour
{
	private Dictionary<Transform, GameObject> m_RaycastGameObjects = new Dictionary<Transform, GameObject>(); // Stores which gameobject the proxys' ray origins are pointing at

	private GameObject[] m_IgnoreList;

	public Transform ignoreRoot { get; set; }

	public void UpdateRaycasts(List<IProxy> proxies, Camera camera)
	{
		UpdateIgnoreList();
		foreach (var proxy in proxies)
		{
			if (proxy.active)
			{
				foreach (var rayOrigin in proxy.rayOrigins.Values)
					m_RaycastGameObjects[rayOrigin] = Raycast(new Ray(rayOrigin.position, rayOrigin.forward), camera);
			}
		}
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
		
		// TODO populate ignore list and use it to prevent raycasts from returning editor vr's gameobjects
		var go = HandleUtility.PickGameObject(camera.pixelRect.center, false, m_IgnoreList);

		Camera.SetupCurrent(restoreCamera);
		RenderTexture.ReleaseTemporary(camera.targetTexture);
		camera.targetTexture = null;

		return go;
	}
}
