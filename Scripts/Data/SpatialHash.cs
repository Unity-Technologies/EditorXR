using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.VR.Data
{
	public class SpatialHash
	{
		public bool cellSizeChanged { get; private set; }
		private float m_CellSize = 1f;
		private float m_LastCellSize;
		private bool m_Changes;
		private const float kMinCellSize = 0.1f;

		//Vector3 bucket represents center of cube with side-length m_CellSize
		private readonly Dictionary<IntVector3, List<SpatialObject>> m_SpatialDictionary = new Dictionary<IntVector3, List<SpatialObject>>();
		private readonly List<SpatialObject> m_AllObjects = new List<SpatialObject>();

		public bool changes
		{
			get { return m_Changes; }
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

		public IntVector3 SnapToGrid(Vector3 vec)
		{
			return SnapToGrid(vec, m_CellSize);
		}

		public static IntVector3 SnapToGrid(Vector3 vec, float cellSize)
		{
			IntVector3 iVec = new IntVector3()
			{
				x = Mathf.RoundToInt(vec.x / cellSize),
				y = Mathf.RoundToInt(vec.y / cellSize),
				z = Mathf.RoundToInt(vec.z / cellSize)
			};
			return iVec;
		}

		public bool GetIntersections(IntVector3 globalBucket, out List<SpatialObject> intersections)
		{
			return m_SpatialDictionary.TryGetValue(globalBucket, out intersections);
		}

		//Note: I want this to be private, but SpatialObject needs access to it
		internal void AddObjectToBucket(IntVector3 worldBucket, SpatialObject spatialObject)
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
			//Debug.Log("Adding object " + spatialObject.name);
			m_AllObjects.Add(spatialObject);
			return spatialObject.AddToHash(this);
		}

		public void RemoveObject(Renderer obj)
		{
			SpatialObject spatial = null;
			foreach (var spatialObject in m_AllObjects)
			{
				spatial = spatialObject;
			}
			if (spatial != null)
				RemoveObject(spatial);
		}

		public void RemoveObject(SpatialObject obj)
		{
			m_AllObjects.Remove(obj);
			List<IntVector3> removeBuckets = obj.GetRemoveBuckets();
			obj.ClearBuckets();
			RemoveObjectFromBuckets(removeBuckets, obj);
		}

		private void RemoveObjectFromBuckets(ICollection<IntVector3> buckets, SpatialObject spatialObject)
		{
			foreach (var bucket in buckets)
			{
				RemoveObjectFromBucket(bucket, spatialObject);
			}
		}

		internal void RemoveObjectFromBucket(IntVector3 bucket, SpatialObject spatialObject)
		{
			List<SpatialObject> contents;
			if (m_SpatialDictionary.TryGetValue(bucket, out contents))
			{
				contents.Remove(spatialObject);
				if (contents.Count == 0)
					m_SpatialDictionary.Remove(bucket);
			}
		}
	}
}