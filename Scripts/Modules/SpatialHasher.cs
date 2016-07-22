using System.Collections.Generic;          
using UnityEngine;
using Mono.Simd;
using IntVector3 = Mono.Simd.Vector4i;
using System.Collections;
using UnityEngine.VR.Utilities;
using Debug = UnityEngine.Debug;
					 
public class SpatialHasher : MonoBehaviour
{
	[SerializeField]
	float m_CellSize = 1f;
	[SerializeField]
	private Color m_TesterColor = Color.yellow;
	[SerializeField]
	int m_ConeSegments = 4;
	[SerializeField]
	float m_ConeRadius = 0.03f;
	[SerializeField]
	float m_ConeHeight = 0.05f;
            
    const float kMinCellSize = 0.1f;                                
    
    //Vector3 bucket represents center of cube with side-length m_CellSize
    readonly Dictionary<IntVector3, List<SpatialObject>>	m_SpatialDictionary = new Dictionary<IntVector3, List<SpatialObject>>();
    readonly List<SpatialObject>							m_SpatialObjects = new List<SpatialObject>();
    readonly Dictionary<IntersectionTester, SpatialObject>	m_IntersectedObjects = new Dictionary<IntersectionTester, SpatialObject>();
    IntersectionTester[]									m_Testers = new IntersectionTester[0];
    float m_LastCellSize;
	bool m_Changes;

	static int s_ProcessedObjectCount;
	static float s_FrameStartTime;
	static float s_MaxDeltaTime = 0.015f;
	Mesh m_ConeMesh;
	Ray[] m_ConeRays;

	public static float maxDeltaTime
	{
		get { return s_MaxDeltaTime; }
	}
	public static float frameStartTime {
		get { return s_FrameStartTime; }
	}						 
	
	public bool hasObjects {
		get { return m_IntersectedObjects.Count > 0; }
	}	   
	public IntersectionTester[] testers {				//This exists for the purpose of the inspector--could be removed
		get { return m_Testers; }
	}
#if UNITY_EDITOR						
	public List<SpatialObject> spatialObjects {
		get { return m_SpatialObjects; }
	}										   
	public int spatialCellCount
	{
		get { return m_SpatialDictionary.Count; }
	}

	public int intersectedObjectCount
	{
		get { return m_IntersectedObjects.Count; }
	}

#endif

	void Awake()
	{
		Setup();
		StopAllCoroutines();
		StartCoroutine(SetupObjects());
	}
    void Setup() {
		m_ConeMesh = IntersectionTester.GenerateConeMesh(m_ConeSegments, m_ConeRadius, m_ConeHeight, out m_ConeRays);
		ResetWorld();
    } 

    //TODO: don't use procedural meshes for the m_Testers? As it stands, they are created/destroyed every time you run --MTS
    public void AddTester(Transform trans)
    {								 
        m_IntersectedObjects.Clear();               		
        GameObject g = new GameObject("pointer");
        MeshRenderer renderer = g.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Standard"));
	    renderer.sharedMaterial.color = m_TesterColor;
		MeshFilter filter = g.AddComponent<MeshFilter>();
        g.transform.SetParent(trans, false);
        filter.sharedMesh = m_ConeMesh;
		m_Testers = new List<IntersectionTester>(m_Testers) { new IntersectionTester (renderer, m_ConeRays) }.ToArray();
    }

    public void ResetWorld()
    {                                          
        m_SpatialObjects.Clear();
        m_SpatialDictionary.Clear();
        m_LastCellSize = m_CellSize;
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
        m_SpatialObjects.Sort((a, b) => a.sceneObject.bounds.size.magnitude.CompareTo(b.sceneObject.bounds.size.magnitude) );             
    }                               

    void OnIntersection(IntersectionTester IntersectionTester, SpatialObject obj)
    {   
		//TODO: Bring back intersection logic--need to figure out how we want to store and consume the current state
        //SpatialObject old;
        //if (m_IntersectedObjects.TryGetValue(IntersectionTester, out old))
        //{
        //    old.sceneObject.sharedMaterials = IntersectionTester.oldMaterials;
        //}
        //IntersectionTester.oldMaterials = obj.sceneObject.sharedMaterials;
        ////If we've already intersected, use the stored material
        //foreach (var intersectedObject in m_IntersectedObjects)
        //{
        //    if (intersectedObject.Value == obj)
        //    {
        //        IntersectionTester.oldMaterials = intersectedObject.Key.oldMaterials;
        //        break;
        //    }
        //}
        //m_IntersectedObjects[IntersectionTester] = obj;
        //Material[] selectedMaterials = new Material[obj.sceneObject.sharedMaterials.Length];
        //for (int i = 0; i < selectedMaterials.Length; i++)
        //    selectedMaterials[i] = selectedMaterial;
        //obj.sceneObject.sharedMaterials = selectedMaterials;
		Debug.Log("intersect");
    }
    void OnIntersectionExit(IntersectionTester IntersectionTester, SpatialObject obj) {
		//TODO: Bring back intersection logic--this will currently not be called
        //obj.sceneObject.sharedMaterials = IntersectionTester.oldMaterials;
        //m_IntersectedObjects.Remove(IntersectionTester);
    }
   
    void Update()
    {
        s_FrameStartTime = Time.realtimeSinceStartup;
        SpatialObject.processCount = 0;
        if (m_CellSize < kMinCellSize)
            m_CellSize = kMinCellSize;
        if (m_CellSize != m_LastCellSize)                                        
        {
            ResetWorld();
        }                   
        m_LastCellSize = m_CellSize;
       
        if (m_Testers == null)
            return;         
        foreach (var tester in m_Testers)
        {                         
            if (!tester.active)
                continue;           
            if (m_Changes || tester.renderer.transform.hasChanged)
            {
                bool detected = false;
                var globalBucket = tester.GetCell(m_CellSize);
                List<SpatialObject> intersections = null;
                if (m_SpatialDictionary.TryGetValue(globalBucket, out intersections))
                {                                                             
                    //Sort list to try and hit closer object first
                    intersections.Sort((a, b) => (a.sceneObject.bounds.center - tester.renderer.bounds.center).magnitude.CompareTo((b.sceneObject.bounds.center - tester.renderer.bounds.center).magnitude));
                    foreach (var obj in intersections)
                    {                         
                        //Early-outs:
                        // No mesh data
                        // Not updated yet
                        if (!obj.processed || obj.sceneObject.transform.hasChanged)
                            continue;    
                        //Bounds check                                                                     
                        if (!obj.sceneObject.bounds.Intersects(tester.renderer.bounds))
                            continue;
	                    if (U.Intersection.TestObject(obj, tester))
	                    {
		                    detected = true;
							OnIntersection(tester, obj);
						}
                        if(detected)
                            break;
                    }
                }                                                                              
                if (!detected)
                {                                                                              
                    SpatialObject intersectedObject;                                                
                    if (m_IntersectedObjects.TryGetValue(tester, out intersectedObject))
                    {                 
                        OnIntersectionExit(tester, intersectedObject);
                    }
                }
            }
            tester.renderer.transform.hasChanged = false;
        }
        m_Changes = false;
    }
    void OnGUI()
    {
        GUILayout.Label(s_ProcessedObjectCount + " / " + m_SpatialObjects.Count);
    }

    IEnumerator SetupObjects() {
        foreach (var obj in m_SpatialObjects)
        {
	        var enumerator = obj.SpatializeNew(m_CellSize, m_SpatialDictionary).GetEnumerator();
			while(enumerator.MoveNext()) {
                yield return null;
                m_Changes = true;
            }                                                                                  
            s_ProcessedObjectCount++;                                                                        
        }													
        StartCoroutine(UpdateDynamicObjects());
    }

    IEnumerator UpdateDynamicObjects()
    {
        while (true)
        {
            bool newFrame = false;
            List<SpatialObject> tmp = new List<SpatialObject>(m_SpatialObjects);
            s_ProcessedObjectCount = 0;
            foreach (var obj in tmp)
            {
                s_ProcessedObjectCount++;
                if(obj.tooBig)
                    continue;
                if (obj.sceneObject.transform.hasChanged)
                {
	                var enumerator = obj.Spatialize(m_CellSize, m_SpatialDictionary).GetEnumerator();
					while(enumerator.MoveNext())
                    {
                        yield return null;
                        newFrame = true;
                    }             
                    m_Changes = true;
                }
            }
			if (!newFrame)
                yield return null;
        }
    }

    public void AddObject(Renderer obj)
    {                             
        obj.transform.hasChanged = true;
	    StartCoroutine(AddNewObject(new SpatialObject(obj)));
    }

	IEnumerator AddNewObject(SpatialObject obj)
	{				  
		m_SpatialObjects.Add(obj);
		var enumerator = obj.SpatializeNew(m_CellSize, m_SpatialDictionary).GetEnumerator();
		while (enumerator.MoveNext()) {
			yield return null;
			m_Changes = true;
		}
	}

    public void RemoveObject(Renderer obj)
    {
        SpatialObject spatial = null;
        foreach (var spatialObject in m_SpatialObjects)
        {
            spatial = spatialObject;
        }
        if(spatial != null)
            RemoveObject(spatial);
    }

    public void RemoveObject(SpatialObject obj) {         
        m_SpatialObjects.Remove(obj);
        List<IntVector3> removeBuckets = obj.GetRemoveBuckets();
        obj.ClearBuckets();
        RemoveFromDictionary(obj, removeBuckets);
    }
    void RemoveFromDictionary(SpatialObject obj, List<IntVector3> removeBuckets)
    {   
        foreach (var bucket in removeBuckets) {
            List<SpatialObject> contents;
            if (m_SpatialDictionary.TryGetValue(bucket, out contents)) {
                contents.Remove(obj);
                if (contents.Count == 0)
                    m_SpatialDictionary.Remove(bucket);
            }
        }
    }
    public IntersectionTester GetLeftTester() {
        if (m_Testers.Length > 0)
            return m_Testers[0];
        return null;
    }
    public IntersectionTester GetRightTester() {
        if (m_Testers.Length > 1)
            return m_Testers[1];
        return null;
    }
    public SpatialObject GetIntersectedObjectForTester(IntersectionTester IntersectionTester) {
        SpatialObject obj;
        m_IntersectedObjects.TryGetValue(IntersectionTester, out obj);
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
        if (!m_SpatialObjects.Contains(IntersectionTester.grabbed))
        {
            m_SpatialObjects.Add(IntersectionTester.grabbed);
            //IntersectionTester.grabbed.sceneObject.transform.hasChanged = true;     
            StartCoroutine(UnGrabReAdd(IntersectionTester.grabbed));
        }
        IntersectionTester.grabbed = null;
        IntersectionTester.active = true;
    }

    IEnumerator UnGrabReAdd(SpatialObject obj)
    {
	    var enumerator = obj.SpatializeNew(m_CellSize, m_SpatialDictionary).GetEnumerator();
		while(enumerator.MoveNext())
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