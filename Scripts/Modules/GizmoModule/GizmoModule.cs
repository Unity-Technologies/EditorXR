using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	public class GizmoModule : MonoBehaviour, IUsesViewerScale
	{
		public static GizmoModule instance;

		public const float rayLength = 100f;
		const float k_RayWidth = .001f;

		readonly List<Renderer> m_Rays = new List<Renderer>();
		int m_RayCount;

		readonly List<Renderer> m_Spheres = new List<Renderer>();
		int m_SphereCount;

		public Material gizmoMaterial
		{
			get { return m_GizmoMaterial; }
		}
		[SerializeField]
		Material m_GizmoMaterial;

		public Func<float> getViewerScale { private get; set; }

		void Awake()
		{
			instance = this;
		}

		void LateUpdate()
		{
			for (int i = m_RayCount; i < m_Rays.Count; i++)
			{
				m_Rays[i].gameObject.SetActive(false);
			}

			for (int i = m_SphereCount; i < m_Spheres.Count; i++)
			{
				m_Spheres[i].gameObject.SetActive(false);
			}

			m_SphereCount = 0;
			m_RayCount = 0;
		}

		public void DrawRay(Vector3 origin, Vector3 direction, Color color, float rayLength = rayLength)
		{
			Renderer ray;
			if (m_Rays.Count > m_RayCount)
			{
				ray = m_Rays[m_RayCount];
			}
			else
			{
				ray = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<Renderer>();
				ObjectUtils.Destroy(ray.GetComponent<Collider>());
				ray.transform.parent = transform;
				ray.sharedMaterial = Instantiate(m_GizmoMaterial);
				m_Rays.Add(ray);
			}

			ray.gameObject.SetActive(true);
			ray.sharedMaterial.color = color;
			var rayTransform = ray.transform;
			var rayWidth = k_RayWidth * getViewerScale();
			rayTransform.localScale = new Vector3(rayWidth, rayWidth, rayLength);
			rayTransform.position = origin + direction * rayLength * 0.5f;
			rayTransform.rotation = Quaternion.LookRotation(direction);

			m_RayCount++;
		}

		public void DrawSphere(Vector3 center, float radius, Color color)
		{
			Renderer sphere;
			if (m_Spheres.Count > m_SphereCount)
			{
				sphere = m_Spheres[m_SphereCount];
			}
			else
			{
				sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<Renderer>();
				sphere.transform.parent = transform;
				sphere.sharedMaterial = Instantiate(m_GizmoMaterial);
				m_Spheres.Add(sphere);
			}

			sphere.gameObject.SetActive(true);
			sphere.sharedMaterial.color = color;
			var sphereTransform = sphere.transform;
			sphereTransform.localScale = Vector3.one * radius;
			sphereTransform.position = center;

			m_SphereCount++;
		}

		void OnDestroy()
		{
			foreach (var ray in m_Rays)
			{
				ObjectUtils.Destroy(ray.GetComponent<Renderer>().sharedMaterial);
			}

			foreach (var sphere in m_Spheres)
			{
				ObjectUtils.Destroy(sphere.GetComponent<Renderer>().sharedMaterial);
			}
		}
	}
}