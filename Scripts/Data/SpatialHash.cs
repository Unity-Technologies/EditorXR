using System.Collections;
using System.Collections.Generic;
using Mono.Simd;

namespace UnityEngine.VR.Data
{
	public class SpatialHash
	{
		public bool cellSizeChanged { get; private set; }
		float m_CellSize = 1f;
		private float m_LastCellSize;
		private bool m_Changes;
		private const float kMinCellSize = 0.1f;

		//Vector3 bucket represents center of cube with side-length m_CellSize
		readonly Dictionary<Vector4i, List<SpatialObject>> m_SpatialDictionary = new Dictionary<Vector4i, List<SpatialObject>>();
		readonly List<SpatialObject> m_AllObjects = new List<SpatialObject>();

		public bool changes
		{
			get { return false; }
		}

		public float cellSize
		{
			get { return m_CellSize; }
		}

		public List<SpatialObject> allObjects
		{
			get { return m_AllObjects; }
		}

#if UNITY_EDITOR
		public int spatialCellCount
		{
			get { return m_SpatialDictionary.Count; }
		}
#endif

		public void SetCellSize(float cellSize)
		{
			m_CellSize = cellSize;
			if (m_CellSize != m_LastCellSize)
			{
				if (m_CellSize < kMinCellSize)
					m_CellSize = kMinCellSize;
				if (m_CellSize != m_LastCellSize)
				{
					Clear();
					cellSizeChanged = true;
				}
			}
		}

		public void Clear()
		{
			m_SpatialDictionary.Clear();
			m_AllObjects.Clear();
			m_LastCellSize = m_CellSize;
		}

		public Vector4i SnapToGrid(Vector3 vec)
		{
			return SnapToGrid(vec, m_CellSize);
		}

		public static Vector4i SnapToGrid(Vector3 vec, float cellSize)
		{
			Vector4i iVec = new Vector4i
			{
				X = Mathf.RoundToInt(vec.x / cellSize),
				Y = Mathf.RoundToInt(vec.y / cellSize),
				Z = Mathf.RoundToInt(vec.z / cellSize)
			};
			return iVec;
		}

		public bool GetIntersections(Vector4i globalBucket, out List<SpatialObject> intersections)
		{
			return m_SpatialDictionary.TryGetValue(globalBucket, out intersections);
		}

		internal void AddObjectToBucket(Vector4i worldBucket, SpatialObject spatialObject)
		{
			List<SpatialObject> contents;
			if (!m_SpatialDictionary.TryGetValue(worldBucket, out contents))
			{
				contents = new List<SpatialObject>();
				m_SpatialDictionary[worldBucket] = contents;
			}
			contents.Add(spatialObject);
			m_Changes = true;
		}

		public IEnumerable AddObject(SpatialObject spatialObject)
		{
			return spatialObject.AddToHash(this);
		}

		public void RemoveObject(Renderer obj) {
			SpatialObject spatial = null;
			foreach (var spatialObject in m_AllObjects) {
				spatial = spatialObject;
			}
			if (spatial != null)
				RemoveObject(spatial);
		}

		public void RemoveObject(SpatialObject obj) {
			m_AllObjects.Remove(obj);
			List<Vector4i> removeBuckets = obj.GetRemoveBuckets();
			obj.ClearBuckets();
			RemoveObjectFromBuckets(removeBuckets, obj);
		}

		internal void RemoveObjectFromBuckets(ICollection<Vector4i> buckets, SpatialObject spatialObject) {
			foreach (var bucket in buckets)
			{
				RemoveObjectFromBucket(bucket, spatialObject);
			}
		}
		internal void RemoveObjectFromBucket(Vector4i bucket, SpatialObject spatialObject) {
			List<SpatialObject> contents;
			if (m_SpatialDictionary.TryGetValue(bucket, out contents)) {
				contents.Remove(spatialObject);
				if (contents.Count == 0)
					m_SpatialDictionary.Remove(bucket);
			}
		}

	}
}