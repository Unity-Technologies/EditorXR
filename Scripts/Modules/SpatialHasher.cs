using System.Collections.Generic;          
using UnityEngine;
using Mono.Simd;
using IntVector3 = Mono.Simd.Vector4i;
using System.Collections;
using UnityEngine.VR.Utilities;
using Debug = UnityEngine.Debug;
					 
public class SpatialHasher : MonoBehaviour
{
    public float cellSize = 1f;

    //public IntersectionTester[] testers;
    //public Transform[] testTransforms;                 
    //public Material selectedMaterial;
    public Material coneMaterial;

    public int coneSegments = 4;
    public float coneRadius = 0.03f;
    public float coneHeight = 0.05f;

    public bool hasObjects
    {
        get { return intersectedObjects.Count > 0; }
    }
            
    const float k_MinCellSize = 0.1f;                                
    
    //Vector3 bucket represents center of cube with side-length cellSize
    readonly Dictionary<IntVector3, List<SpatialObject>> spatialDictionary = new Dictionary<IntVector3, List<SpatialObject>>();
    readonly List<SpatialObject> spatialObjects = new List<SpatialObject>();
    readonly Dictionary<IntersectionTester, SpatialObject> intersectedObjects = new Dictionary<IntersectionTester, SpatialObject>();
    public IntersectionTester[] testers = new IntersectionTester[0];
    float lastCellSize;

    public static float maxDeltaTime = 0.015f;
              
    static bool started;            //Because onEnable is called twice when entering play mode
    static bool updateStarted;
	private Mesh cone;
	private Ray[] coneRays;			   

	void Awake()
	{
		FullReset();
		StopAllCoroutines();
		StartCoroutine(SetupObjects());
	}
    public void FullReset() {
		cone = IntersectionTester.GenerateConeMesh(coneSegments, coneRadius, coneHeight, out coneRays);
		ResetWorld();
    }

#if UNITY_EDITOR
    public List<SpatialObject> GetSpatialObjects()
    {
        return spatialObjects;
    }                                                    
    public void OnInspectorGUI()
    {
        GUILayout.Label("Spatial Objects: " + spatialObjects.Count);
        GUILayout.Label("Spatial Cells: " + spatialDictionary.Count);
        GUILayout.Label("Intersected Objects: " + intersectedObjects.Count);
        if (GUILayout.Button("Reset Objects"))
            ResetWorld();
        if (GUILayout.Button("Reset Object Cache"))
        {
            MeshData.ClearCache();
            ResetWorld();
        }                           
    }
#endif	
    //TODO: don't use procedural meshes for the testers? As it stands, they are created/destroyed every time you run --MTS
    public void AddTester(Transform trans)
    {								 
        intersectedObjects.Clear();               		
        GameObject g = new GameObject("pointer");
        MeshRenderer renderer = g.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = coneMaterial;
        MeshFilter filter = g.AddComponent<MeshFilter>();
        g.transform.SetParent(trans, false);
        filter.sharedMesh = cone;
		testers = new List<IntersectionTester>(testers) { new IntersectionTester (renderer, coneRays) }.ToArray();
    }

    void ResetWorld()
    {                                          
        spatialObjects.Clear();
        spatialDictionary.Clear();
        lastCellSize = cellSize;
        MeshFilter[] meshes = FindObjectsOfType<MeshFilter>();
        foreach (var meshFilter in meshes)
        {                
            //TODO: Exclude certain objects?
	        if (meshFilter.GetComponentInParent<EditorVR>())
		        continue;
            if (meshFilter.sharedMesh && MeshData.ValidMesh(meshFilter.sharedMesh))
            { 
                Renderer render = meshFilter.GetComponent<Renderer>();
                if (render)
                    AddObject(render);
            }
        }   
        spatialObjects.Sort((a, b) => a.sceneObject.bounds.size.magnitude.CompareTo(b.sceneObject.bounds.size.magnitude) );             
    }                               

    void OnIntersection(IntersectionTester IntersectionTester, SpatialObject obj)
    {   
        //SpatialObject old;
        //if (intersectedObjects.TryGetValue(IntersectionTester, out old))
        //{
        //    old.sceneObject.sharedMaterials = IntersectionTester.oldMaterials;
        //}
        //IntersectionTester.oldMaterials = obj.sceneObject.sharedMaterials;
        ////If we've already intersected, use the stored material
        //foreach (var intersectedObject in intersectedObjects)
        //{
        //    if (intersectedObject.Value == obj)
        //    {
        //        IntersectionTester.oldMaterials = intersectedObject.Key.oldMaterials;
        //        break;
        //    }
        //}
        //intersectedObjects[IntersectionTester] = obj;
        //Material[] selectedMaterials = new Material[obj.sceneObject.sharedMaterials.Length];
        //for (int i = 0; i < selectedMaterials.Length; i++)
        //    selectedMaterials[i] = selectedMaterial;
        //obj.sceneObject.sharedMaterials = selectedMaterials;
		Debug.Log("intersect");
    }
    void OnIntersectionExit(IntersectionTester IntersectionTester, SpatialObject obj) {
        obj.sceneObject.sharedMaterials = IntersectionTester.oldMaterials;
        intersectedObjects.Remove(IntersectionTester);
    }

    bool changes;
    public static float frameStartTime;
    public static int minProcess = 400;
    public static int processCount;
   
    void Update()
    {
        frameStartTime = Time.realtimeSinceStartup;
        processCount = 0;
        if (cellSize < k_MinCellSize)
            cellSize = k_MinCellSize;
        if (cellSize != lastCellSize)                                        
        {
            ResetWorld();
        }                   
        lastCellSize = cellSize;
       
        if (testers == null)
            return;         
        foreach (var IntersectionTester in testers)
        {                         
            if (!IntersectionTester.active)
                continue;           
            if (changes || IntersectionTester.renderer.transform.hasChanged)
            {
                bool detected = false;
                var globalBucket = IntersectionTester.GetCell(cellSize);
                List<SpatialObject> intersections = null;
                if (spatialDictionary.TryGetValue(globalBucket, out intersections))
                {                                                             
                    //Sort list to try and hit closer object first
                    intersections.Sort((a, b) => (a.sceneObject.bounds.center - IntersectionTester.renderer.bounds.center).magnitude.CompareTo((b.sceneObject.bounds.center - IntersectionTester.renderer.bounds.center).magnitude));
                    foreach (var obj in intersections)
                    {                         
                        //Early-outs:
                        // No mesh data
                        // Not updated yet
                        if (!obj.processed || obj.sceneObject.transform.hasChanged)
                            continue;    
                        //Bounds check                                                                     
                        if (!obj.sceneObject.bounds.Intersects(IntersectionTester.renderer.bounds))
                            continue;
	                    if (U.Intersection.TestObject(obj, IntersectionTester))
	                    {
		                    detected = true;
							OnIntersection(IntersectionTester, obj);
						}
                        if(detected)
                            break;
                    }
                }                                                                              
                if (!detected)
                {                                                                              
                    SpatialObject intersectedObject;                                                
                    if (intersectedObjects.TryGetValue(IntersectionTester, out intersectedObject))
                    {                 
                        OnIntersectionExit(IntersectionTester, intersectedObject);
                    }
                }
            }
            IntersectionTester.renderer.transform.hasChanged = false;
        }
        changes = false;
    }

    public static int objCount = 0;
    int bucketCount = 0;
    int bucketTotal = 0;
    void OnGUI()
    {
        GUILayout.Label(objCount + " / " + spatialObjects.Count);
        GUILayout.Label(bucketCount + " / " + bucketTotal);
    }

    IEnumerator SetupObjects() {
        foreach (var obj in spatialObjects) {                                  
            foreach (var e in obj.SpatializeNew(cellSize, spatialDictionary)) {
                yield return null;
                changes = true;
            }                                                                                  
            objCount++;                                                                        
        }													
        StartCoroutine(UpdateDynamicObjects());
    }

    IEnumerator UpdateDynamicObjects()
    {
        while (true)
        {
            bool newFrame = false;
            List<SpatialObject> tmp = new List<SpatialObject>(spatialObjects);
            objCount = 0;
            foreach (var obj in tmp)
            {
                objCount++;
                if(obj.tooBig)
                    continue;
                if (obj.sceneObject.transform.hasChanged)
                {                                                               
                    foreach (var e in obj.Spatialize(cellSize, spatialDictionary))
                    {
                        yield return null;
                        newFrame = true;
                    }             
                    changes = true;
                }
            }
			if (!newFrame)
                yield return null;
        }
    }

    public void AddObject(Renderer obj)
    {                             
        obj.transform.hasChanged = true;
        spatialObjects.Add(new SpatialObject(obj));
    }   

    public void RemoveObject(Renderer obj)
    {
        SpatialObject spatial = null;
        foreach (var spatialObject in spatialObjects)
        {
            spatial = spatialObject;
        }
        if(spatial != null)
            RemoveObject(spatial);
    }

    public void RemoveObject(SpatialObject obj) {         
        spatialObjects.Remove(obj);
        List<IntVector3> removeBuckets = obj.GetRemoveBuckets();
        obj.ClearBuckets();
        RemoveFromDictionary(obj, removeBuckets);
    }
    void RemoveFromDictionary(SpatialObject obj, List<IntVector3> removeBuckets)
    {   
        foreach (var bucket in removeBuckets) {
            List<SpatialObject> contents;
            if (spatialDictionary.TryGetValue(bucket, out contents)) {
                contents.Remove(obj);
                if (contents.Count == 0)
                    spatialDictionary.Remove(bucket);
            }
        }
    }
    public IntersectionTester GetLeftTester() {
        if (testers.Length > 0)
            return testers[0];
        return null;
    }
    public IntersectionTester GetRightTester() {
        if (testers.Length > 1)
            return testers[1];
        return null;
    }
    public SpatialObject GetIntersectedObjectForTester(IntersectionTester IntersectionTester) {
        SpatialObject obj;
        intersectedObjects.TryGetValue(IntersectionTester, out obj);
        return obj;
    }

    public SpatialObject GrabObjectAndRemove(IntersectionTester IntersectionTester)
    {
        SpatialObject obj = GetIntersectedObjectForTester(IntersectionTester);
        IntersectionTester.grabbed = obj;
        RemoveObject(obj);        
        return obj;
    }
    public SpatialObject GrabObjectAndDisableTester(IntersectionTester IntersectionTester)
    {
        SpatialObject obj = GetIntersectedObjectForTester(IntersectionTester);
        IntersectionTester.grabbed = obj;     
        IntersectionTester.active = false;
        return obj;
    }
    public SpatialObject GrabObjectAndRemoveAndDisableTester(IntersectionTester IntersectionTester) {
        SpatialObject obj = GetIntersectedObjectForTester(IntersectionTester);
        if (obj != null)
        {
            IntersectionTester.grabbed = obj;
            RemoveObject(obj);
            IntersectionTester.active = false;
        }
        return obj;
    }
    public void UnGrabObject(IntersectionTester IntersectionTester)
    {
        if (!spatialObjects.Contains(IntersectionTester.grabbed))
        {
            spatialObjects.Add(IntersectionTester.grabbed);
            //IntersectionTester.grabbed.sceneObject.transform.hasChanged = true;     
            StartCoroutine(UnGrabReAdd(IntersectionTester.grabbed));
        }
        IntersectionTester.grabbed = null;
        IntersectionTester.active = true;
    }

    IEnumerator UnGrabReAdd(SpatialObject obj)
    {
        foreach (var e in obj.SpatializeNew(cellSize, spatialDictionary))
        {
            yield return null;
        }
    }

    public static IntVector3 SnapToGrid(Vector3 vec, float cellSize)
    {                                               
        IntVector3 iVec = new IntVector3
        {
            X = Mathf.RoundToInt(vec.x / cellSize),
            Y = Mathf.RoundToInt(vec.y / cellSize),
            Z = Mathf.RoundToInt(vec.z / cellSize)
        };
        return iVec;
    }
}

public static class Vector4iEx
{
    public static Vector3 mul(this Vector4i vec, float val) {
        return new Vector3(vec.X * val, vec.Y * val, vec.Z * val);
    }

    public static Vector3 ToVector3(this Vector4i vec)
    {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }
}           