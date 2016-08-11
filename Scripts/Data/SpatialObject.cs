using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Data
{
	public class SpatialObject
	{
		public bool tooBig { get; private set; }
		public Renderer sceneObject { get; private set; } //The object we are tracking

		private const int k_MinProcess = 400;
		private const int k_MaxBuckets = 10000;
		private static int s_ProcessCount;

		private readonly List<IntVector3> m_Buckets = new List<IntVector3>(); //Buckets that the object currently occupies.

		private IntVector3 m_LastLowerLeft;
		private IntVector3 m_LastUpperRight;

		private MeshData m_MeshData;
		private IntVector3 m_PositionOffset;

		public static int processCount
		{
			set { s_ProcessCount = value; }
		}

		public Vector3[] vertices
		{
			get { return m_MeshData.vertices; }
		}

		public float meshCellSize
		{
			get { return m_MeshData.cellSize; }
		}

		public Dictionary<IntVector3, List<IntVector3>> triBuckets
		{
			get { return m_MeshData.triBuckets; }
		}

		public bool processed
		{
			get { return m_MeshData.processed; }
		}

		public string name
		{
			get
			{
				string name = m_MeshData.name;
				if (m_MeshData.processed)
					name += " - m_Buckets: " + triBuckets.Count;
				else
					name += " - processing...";
				return name;
			}

		}

		public SpatialObject(Renderer sceneObject)
		{
			this.sceneObject = sceneObject;
			SetupMesh();
		}

		void SetupMesh()
		{
			MeshFilter filter = sceneObject.GetComponent<MeshFilter>();
			if (filter)
			{
				m_MeshData = MeshData.GetMeshData(filter.sharedMesh);
			} else
			{
				throw new ArgumentException("SpatialObject renderers require an attached MeshFilter on " + filter);
			}
		}

		public IEnumerable UpdatePosition(SpatialHash hash)
		{
			IntVector3 lowerLeft = hash.SnapToGrid(sceneObject.bounds.center - (sceneObject.bounds.extents - Vector3.one * hash.cellSize * 0.5f)) - m_PositionOffset;
			IntVector3 upperRight = hash.SnapToGrid(sceneObject.bounds.center + (sceneObject.bounds.extents + Vector3.one * hash.cellSize * 0.5f)) - m_PositionOffset;

			if (m_LastLowerLeft == lowerLeft && m_LastUpperRight == upperRight)
				yield break;
			//Optimization to only add/remove to m_Buckets that changed. Replaces hashset
			List<IntVector3> removeBuckets = GetRemoveBuckets();
			m_Buckets.Clear();
			m_PositionOffset = hash.SnapToGrid(sceneObject.transform.position + Vector3.one * hash.cellSize * 0.5f);

			m_LastLowerLeft = lowerLeft;
			m_LastUpperRight = upperRight;
			m_Buckets.Capacity = (upperRight.x - lowerLeft.x) * (upperRight.y - lowerLeft.y) * (upperRight.z - lowerLeft.z);
			for (int x = lowerLeft.x; x <= upperRight.x; x++)
			{
				for (int y = lowerLeft.y; y <= upperRight.y; y++)
				{
					for (int z = lowerLeft.z; z <= upperRight.z; z++)
					{
						IntVector3 bucket = new IntVector3(x, y, z);
						m_Buckets.Add(bucket);
						IntVector3 worldBucket = bucket + m_PositionOffset;
						if (!removeBuckets.Remove(worldBucket))
							hash.AddObjectToBucket(worldBucket, this);
						if (s_ProcessCount++ > k_MinProcess && Time.realtimeSinceStartup - SpatialHashUpdateModule.frameStartTime > SpatialHashUpdateModule.maxDeltaTime)
							yield return null;
					}
				}
			}
			foreach (var bucket in removeBuckets)
			{
				hash.RemoveObjectFromBucket(bucket, this);
				if (s_ProcessCount++ > k_MinProcess && Time.realtimeSinceStartup - SpatialHashUpdateModule.frameStartTime > SpatialHashUpdateModule.maxDeltaTime)
				{
					yield return null;
				}
			}
			sceneObject.transform.hasChanged = false;
			yield return null;
		}

		public IEnumerable AddToHash(SpatialHash hash)
		{
			//Optimization to only add/remove to m_Buckets that changed. Replaces hashset                           
			m_Buckets.Clear();
			m_PositionOffset = hash.SnapToGrid(sceneObject.transform.position + Vector3.one * hash.cellSize * 0.5f);
			IntVector3 lowerLeft = hash.SnapToGrid(sceneObject.bounds.center - (sceneObject.bounds.extents - Vector3.one * hash.cellSize * 0.5f)) - m_PositionOffset;
			IntVector3 upperRight = hash.SnapToGrid(sceneObject.bounds.center + (sceneObject.bounds.extents + Vector3.one * hash.cellSize * 0.5f)) - m_PositionOffset;
			m_Buckets.Capacity = (upperRight.x - lowerLeft.x) * (upperRight.y - lowerLeft.y) * (upperRight.z - lowerLeft.z);
			if (m_Buckets.Capacity > k_MaxBuckets)
			{
				tooBig = true;
				yield break;
			}
			for (int x = lowerLeft.x; x <= upperRight.x; x++)
			{
				for (int y = lowerLeft.y; y <= upperRight.y; y++)
				{
					for (int z = lowerLeft.z; z <= upperRight.z; z++)
					{
						IntVector3 bucket = new IntVector3(x, y, z);
						m_Buckets.Add(bucket);
						IntVector3 worldBucket = bucket + m_PositionOffset;
						hash.AddObjectToBucket(worldBucket, this);
						if (s_ProcessCount++ > k_MinProcess && Time.realtimeSinceStartup - SpatialHashUpdateModule.frameStartTime > SpatialHashUpdateModule.maxDeltaTime)
						{
							yield return null;
						}
					}
				}
			}
			sceneObject.transform.hasChanged = false;
		}

		public List<IntVector3> GetRemoveBuckets()
		{
			return new List<IntVector3>(m_Buckets.Select(bucket => bucket + m_PositionOffset));
		}

		public void ClearBuckets()
		{
			m_Buckets.Clear();
		}
	}
}