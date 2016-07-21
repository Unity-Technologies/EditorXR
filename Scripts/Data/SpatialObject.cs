using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using IntVector3 = Mono.Simd.Vector4i;

public class SpatialObject {
    public readonly Renderer sceneObject;               //The object we are tracking

#if UNITY_EDITOR
    public bool expanded, bucketsExpanded;              //Expand states for inspector foldouts
#endif

    public Vector3[] vertices {
        get { return meshData.vertices; }
    }
    public float cellSize {
        get { return meshData.cellSize; }
    }
    public Dictionary<IntVector3, List<IntVector3>> triBuckets {
        get { return meshData.triBuckets; }
    }

    public bool processed {
        get { return meshData.processed; }
    }

    public string name
    {
        get
        {
            string name = meshData.name;
            if (meshData.processed)                                                                                
                name += " - buckets: " + triBuckets.Count;
            else                           
                name += " - processing...";
            return name;
        }

    }

    const int maxBuckets = 10000;
    public bool tooBig;

    MeshData meshData;
    IntVector3 positionOffset;
    readonly List<IntVector3> buckets = new List<IntVector3>();       //Buckets that the object currently occupies.

    public SpatialObject(Renderer sceneObject) {
        this.sceneObject = sceneObject;         
        SetupMesh();
    }

    void SetupMesh() {
        MeshFilter filter = sceneObject.GetComponent<MeshFilter>();
        if (filter) {
            meshData = MeshData.GetMeshData(filter.sharedMesh);
        } else {
            throw new ArgumentException("SpatialObject renderers require an attached MeshFilter on " + filter);
        }
    }

    IntVector3 lastLowerLeft, lastUpperRight;
    public IEnumerable Spatialize(float cellSize, Dictionary<IntVector3, List<SpatialObject>> spatialDictionary) {
        IntVector3 lowerLeft = SpatialHasher.SnapToGrid(sceneObject.bounds.center - (sceneObject.bounds.extents - Vector3.one * cellSize * 0.5f), cellSize) - positionOffset;
        IntVector3 upperRight = SpatialHasher.SnapToGrid(sceneObject.bounds.center + (sceneObject.bounds.extents + Vector3.one * cellSize * 0.5f), cellSize) - positionOffset;

        if(lastLowerLeft == lowerLeft && lastUpperRight == upperRight)
            yield break;
        //Optimization to only add/remove to buckets that changed. Replaces hashset                           
        List<IntVector3> removeBuckets = GetRemoveBuckets();
        buckets.Clear();
        positionOffset = SpatialHasher.SnapToGrid(sceneObject.transform.position + Vector3.one * cellSize * 0.5f, cellSize);

        lastLowerLeft = lowerLeft;
        lastUpperRight = upperRight;
        buckets.Capacity = (upperRight.X - lowerLeft.X) * (upperRight.Y - lowerLeft.Y) * (upperRight.Z - lowerLeft.Z);
        for (int x = lowerLeft.X; x <= upperRight.X; x++) {
            for (int y = lowerLeft.Y; y <= upperRight.Y; y++) {
                for (int z = lowerLeft.Z; z <= upperRight.Z; z++) {
                    IntVector3 bucket = new IntVector3(x, y, z, 0);
                    buckets.Add(bucket);   
                    IntVector3 worldBucket = bucket + positionOffset;
                    if (!removeBuckets.Remove(worldBucket))
                    {
                        List<SpatialObject> contents;
                        if (!spatialDictionary.TryGetValue(worldBucket, out contents)) {
                            contents = new List<SpatialObject>();
                            spatialDictionary[worldBucket] = contents;
                        }
                        contents.Add(this);
                    }                               
                    if (SpatialHasher.processCount++ > SpatialHasher.minProcess && Time.realtimeSinceStartup - SpatialHasher.frameStartTime > SpatialHasher.maxDeltaTime) {
                        yield return null;
                    }
                }
            }
        }                                  
        foreach (var bucket in removeBuckets) {
            List<SpatialObject> contents;
            if (spatialDictionary.TryGetValue(bucket, out contents)) {
                contents.Remove(this);
                if (contents.Count == 0)
                    spatialDictionary.Remove(bucket);
            }
            if (SpatialHasher.processCount++ > SpatialHasher.minProcess && Time.realtimeSinceStartup - SpatialHasher.frameStartTime > SpatialHasher.maxDeltaTime) {
                yield return null;              
            }
        }
        sceneObject.transform.hasChanged = false;
        yield return null;
    }
    public IEnumerable SpatializeNew(float cellSize, Dictionary<IntVector3, List<SpatialObject>> spatialDictionary) {
        //Optimization to only add/remove to buckets that changed. Replaces hashset                           
        buckets.Clear();
        positionOffset = SpatialHasher.SnapToGrid(sceneObject.transform.position + Vector3.one * cellSize * 0.5f, cellSize);
        IntVector3 lowerLeft = SpatialHasher.SnapToGrid(sceneObject.bounds.center - (sceneObject.bounds.extents - Vector3.one * cellSize * 0.5f), cellSize) - positionOffset;
        IntVector3 upperRight = SpatialHasher.SnapToGrid(sceneObject.bounds.center + (sceneObject.bounds.extents + Vector3.one * cellSize * 0.5f), cellSize) - positionOffset;
        buckets.Capacity = (upperRight.X - lowerLeft.X) * (upperRight.Y - lowerLeft.Y) * (upperRight.Z - lowerLeft.Z);
        if (buckets.Capacity > maxBuckets)
        {
            tooBig = true;
            yield break;
        }                  
        for (int x = lowerLeft.X; x <= upperRight.X; x++) {
            for (int y = lowerLeft.Y; y <= upperRight.Y; y++) {
                for (int z = lowerLeft.Z; z <= upperRight.Z; z++)
                {
                    IntVector3 bucket = new IntVector3(x, y, z, 0);
                    buckets.Add(bucket);
                    IntVector3 worldBucket = bucket + positionOffset;
                    List<SpatialObject> contents;
                    if (!spatialDictionary.TryGetValue(worldBucket, out contents))
                    {
                        contents = new List<SpatialObject>();
                        spatialDictionary[worldBucket] = contents;
                    }                                                                     
                    contents.Add(this);             
                    if (SpatialHasher.processCount++ > SpatialHasher.minProcess && Time.realtimeSinceStartup - SpatialHasher.frameStartTime > SpatialHasher.maxDeltaTime) {
                        yield return null;
                    }
                }
            }                                         
        }
        sceneObject.transform.hasChanged = false;
    }
    public List<IntVector3> GetRemoveBuckets() {
        return new List<IntVector3>(buckets.Select(bucket => bucket + positionOffset));
    }

    public void ClearBuckets()
    {
        buckets.Clear();
    }
}

