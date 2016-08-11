using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.VR.Data;

namespace UnityEngine.VR.Modules
{
	public class SpatialHashUpdateModule : MonoBehaviour
	{
		private SpatialHash m_SpatialHash;

		private static int s_ProcessedObjectCount;
		private static float s_FrameStartTime;
		private static float s_MaxDeltaTime = 0.015f;

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

		//TODO: In-VR progress bar
		void OnGUI()
		{
			GUILayout.Label(s_ProcessedObjectCount + " / " + m_SpatialHash.allObjects.Count);
		}

		IEnumerator SetupObjects()
		{
			MeshFilter[] meshes = FindObjectsOfType<MeshFilter>();
			foreach (var meshFilter in meshes)
			{
				//TODO: Exclude certain objects?
				if (meshFilter.GetComponentInParent<EditorVR>())
					continue;
				if (meshFilter.sharedMesh)
				{
					Renderer render = meshFilter.GetComponent<Renderer>();
					if (render)
					{
						var enumerator = AddNewObject(new SpatialObject(render)).GetEnumerator();
						while (enumerator.MoveNext())
						{
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
			if (m_SpatialHash.cellSizeChanged)
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

		private IEnumerator AddObjectCoroutine(Renderer obj)
		{
			var enumerator = AddNewObject(new SpatialObject(obj)).GetEnumerator();
			while (enumerator.MoveNext())
			{
				yield return null;
			}
		}

		IEnumerable AddNewObject(SpatialObject obj)
		{
			var enumerator = m_SpatialHash.AddObject(obj).GetEnumerator();
			while (enumerator.MoveNext())
			{
				yield return null;
			}
		}
	}
}