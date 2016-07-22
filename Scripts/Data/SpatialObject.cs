using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.VR.Modules;
using IntVector3 = Mono.Simd.Vector4i;

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

		public float cellSize
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

		public IEnumerable Spatialize(float cellSize, Dictionary<IntVector3, List<SpatialObject>> spatialDictionary)
		{
			IntVector3 lowerLeft = SpatialHasher.SnapToGrid(sceneObject.bounds.center - (sceneObject.bounds.extents - Vector3.one * cellSize * 0.5f), cellSize) - m_PositionOffset;
			IntVector3 upperRight = SpatialHasher.SnapToGrid(sceneObject.bounds.center + (sceneObject.bounds.extents + Vector3.one * cellSize * 0.5f), cellSize) - m_PositionOffset;

			if (m_LastLowerLeft == lowerLeft && m_LastUpperRight == upperRight)
				yield break;
			//Optimization to only add/remove to m_Buckets that changed. Replaces hashset                           
			List<IntVector3> removeBuckets = GetRemoveBuckets();
			m_Buckets.Clear();
			m_PositionOffset = SpatialHasher.SnapToGrid(sceneObject.transform.position + Vector3.one * cellSize * 0.5f, cellSize);

			m_LastLowerLeft = lowerLeft;
			m_LastUpperRight = upperRight;
			m_Buckets.Capacity = (upperRight.X - lowerLeft.X) * (upperRight.Y - lowerLeft.Y) * (upperRight.Z - lowerLeft.Z);
			for (int x = lowerLeft.X; x <= upperRight.X; x++)
			{
				for (int y = lowerLeft.Y; y <= upperRight.Y; y++)
				{
					for (int z = lowerLeft.Z; z <= upperRight.Z; z++)
					{
						IntVector3 bucket = new IntVector3(x, y, z, 0);
						m_Buckets.Add(bucket);
						IntVector3 worldBucket = bucket + m_PositionOffset;
						if (!removeBuckets.Remove(worldBucket))
						{
							List<SpatialObject> contents;
							if (!spatialDictionary.TryGetValue(worldBucket, out contents))
							{
								contents = new List<SpatialObject>();
								spatialDictionary[worldBucket] = contents;
							}
							contents.Add(this);
						}
						if (s_ProcessCount++ > k_MinProcess && Time.realtimeSinceStartup - SpatialHasher.frameStartTime > SpatialHasher.maxDeltaTime)
						{
							yield return null;
						}
					}
				}
			}
			foreach (var bucket in removeBuckets)
			{
				List<SpatialObject> contents;
				if (spatialDictionary.TryGetValue(bucket, out contents))
				{
					contents.Remove(this);
					if (contents.Count == 0)
						spatialDictionary.Remove(bucket);
				}
				if (s_ProcessCount++ > k_MinProcess && Time.realtimeSinceStartup - SpatialHasher.frameStartTime > SpatialHasher.maxDeltaTime)
				{
					yield return null;
				}
			}
			sceneObject.transform.hasChanged = false;
			yield return null;
		}

		public IEnumerable SpatializeNew(float cellSize, Dictionary<IntVector3, List<SpatialObject>> spatialDictionary)
		{
			//Optimization to only add/remove to m_Buckets that changed. Replaces hashset                           
			m_Buckets.Clear();
			m_PositionOffset = SpatialHasher.SnapToGrid(sceneObject.transform.position + Vector3.one * cellSize * 0.5f, cellSize);
			IntVector3 lowerLeft = SpatialHasher.SnapToGrid(sceneObject.bounds.center - (sceneObject.bounds.extents - Vector3.one * cellSize * 0.5f), cellSize) - m_PositionOffset;
			IntVector3 upperRight = SpatialHasher.SnapToGrid(sceneObject.bounds.center + (sceneObject.bounds.extents + Vector3.one * cellSize * 0.5f), cellSize) - m_PositionOffset;
			m_Buckets.Capacity = (upperRight.X - lowerLeft.X) * (upperRight.Y - lowerLeft.Y) * (upperRight.Z - lowerLeft.Z);
			if (m_Buckets.Capacity > k_MaxBuckets)
			{
				tooBig = true;
				yield break;
			}
			for (int x = lowerLeft.X; x <= upperRight.X; x++)
			{
				for (int y = lowerLeft.Y; y <= upperRight.Y; y++)
				{
					for (int z = lowerLeft.Z; z <= upperRight.Z; z++)
					{
						IntVector3 bucket = new IntVector3(x, y, z, 0);
						m_Buckets.Add(bucket);
						IntVector3 worldBucket = bucket + m_PositionOffset;
						List<SpatialObject> contents;
						if (!spatialDictionary.TryGetValue(worldBucket, out contents))
						{
							contents = new List<SpatialObject>();
							spatialDictionary[worldBucket] = contents;
						}
						contents.Add(this);
						if (s_ProcessCount++ > k_MinProcess && Time.realtimeSinceStartup - SpatialHasher.frameStartTime > SpatialHasher.maxDeltaTime)
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