using System.Collections.Generic;
using Mono.Simd;
using IntVector3 = Mono.Simd.Vector4i;
using System.Collections;
using UnityEngine;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Data;

namespace UnityEngine.VR.Modules
{
	public class SpatialHashUpdateModule : MonoBehaviour
	{
		private SpatialHash m_SpatialHash;
		
		static int s_ProcessedObjectCount;
		static float s_FrameStartTime;
		static float s_MaxDeltaTime = 0.015f;

		public static float maxDeltaTime
		{
			get { return s_MaxDeltaTime; }
		}

		public static float frameStartTime
		{
			get { return s_FrameStartTime; }
		}

		public int objectCount
		{
			get
			{
				if (m_SpatialHash == null)
					return 0;
				return m_SpatialHash.allObjects.Count;
			}
		}

		internal void Setup(SpatialHash hash)
		{
			m_SpatialHash = hash;
			ResetWorld();
			StartCoroutine(SetupObjects());
		}

		public void ResetWorld()
		{
			m_SpatialHash.Clear();
		}

		void OnGUI()
		{
			GUILayout.Label(s_ProcessedObjectCount + " / " + m_SpatialHash.allObjects.Count);
		}

		IEnumerator SetupObjects()
		{
			MeshFilter[] meshes = FindObjectsOfType<MeshFilter>();
			foreach (var meshFilter in meshes) {
				//TODO: Exclude certain objects?
				if (meshFilter.GetComponentInParent<EditorVR>())
					continue;
				if (meshFilter.sharedMesh && MeshData.ValidMesh(meshFilter.sharedMesh)) {
					Renderer render = meshFilter.GetComponent<Renderer>();
					if (render)
					{
						var enumerator = AddNewObject(new SpatialObject(render)).GetEnumerator();
						while (enumerator.MoveNext()) {
							yield return null;
						}
						s_ProcessedObjectCount++;
					}
				}
			}
			StartCoroutine(UpdateDynamicObjects());
		}

		void Update()
		{
			s_FrameStartTime = Time.realtimeSinceStartup;
			SpatialObject.processCount = 0;
			if(m_SpatialHash.cellSizeChanged)
				ResetWorld();
		}

		IEnumerator UpdateDynamicObjects()
		{
			while (true)
			{
				bool newFrame = false;
				List<SpatialObject> tmp = new List<SpatialObject>(m_SpatialHash.allObjects);
				s_ProcessedObjectCount = 0;
				foreach (var obj in tmp)
				{
					s_ProcessedObjectCount++;
					if (obj.tooBig)
						continue;
					if (obj.sceneObject.transform.hasChanged)
					{
						var enumerator = obj.UpdatePosition(m_SpatialHash).GetEnumerator();
						while (enumerator.MoveNext())
						{
							yield return null;
							newFrame = true;
						}
					}
				}
				if (!newFrame)
					yield return null;
			}
		}

		public void AddObject(Renderer obj)
		{
			StartCoroutine(AddObjectCoroutine(obj));
		}
		private IEnumerator AddObjectCoroutine(Renderer obj) {
			var enumerator = AddNewObject(new SpatialObject(obj)).GetEnumerator();
			while (enumerator.MoveNext()) {
				yield return null;
			}
		}

		IEnumerable AddNewObject(SpatialObject obj) {
			//var enumerator = obj.AddToHash(m_SpatialHash).GetEnumerator();
			var enumerator = m_SpatialHash.AddObject(obj).GetEnumerator();
			while (enumerator.MoveNext()) {
				yield return null;
			}
		}

		//public void RemoveObject(Renderer obj)
		//{
		//	SpatialObject spatial = null;
		//	foreach (var spatialObject in m_SpatialObjects)
		//	{
		//		spatial = spatialObject;
		//	}
		//	if (spatial != null)
		//		RemoveObject(spatial);
		//}

		//public void RemoveObject(SpatialObject obj)
		//{
		//	m_SpatialObjects.Remove(obj);
		//	List<IntVector3> removeBuckets = obj.GetRemoveBuckets();
		//	obj.ClearBuckets();
		//	RemoveFromDictionary(obj, removeBuckets);
		//}

		//void RemoveFromDictionary(SpatialObject obj, List<IntVector3> removeBuckets)
		//{
		//	foreach (var bucket in removeBuckets)
		//	{
		//		List<SpatialObject> contents;
		//		if (m_SpatialDictionary.TryGetValue(bucket, out contents))
		//		{
		//			contents.Remove(obj);
		//			if (contents.Count == 0)
		//				m_SpatialDictionary.Remove(bucket);
		//		}
		//	}
		//}
	}
}
//TODO: Put in Extensions class
public static class Vector4iEx {
	public static Vector3 mul(this Vector4i vec, float val) {
		return new Vector3(vec.X * val, vec.Y * val, vec.Z * val);
	}

	public static Vector3 ToVector3(this Vector4i vec) {
		return new Vector3(vec.X, vec.Y, vec.Z);
	}
}