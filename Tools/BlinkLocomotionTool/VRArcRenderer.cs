// todo test for arcing to different heights
// todo 

using UnityEngine;
using UnityEngineInternal;

public class VRArcRenderer : MonoBehaviour
{
    public int lineSegmentCount = 10;

    public float range = 5f; // todo make this work for horizontal aiming
    public float radius = 0.5f;
    public LayerMask layerMask = -1;

    public Transform locatorRoot;
    private SpriteRenderer locatorSprite;
    public MeshRenderer locatorTubeRenderer;

    public Color validLocationColor;
    public Color invalidLocationColor;

	public float maxArc = 0.85f;
    private VRLineRenderer lineRenderer;
    private MeshRenderer lineRendererMeshRenderer;

    private Vector3[] segmentPositions;
	private float curveLengthEstimate;

	private Vector3 p0, p1, p2, p3;

    // moving spheres to move along the points
	public int motionSphereCount = 10;
	public float motionSphereSpeed = 1.0f;
    public GameObject motionIndicatorSphere;
    private Transform[] motionSpheres;
    private float motionSphereOffset;
	private Transform toolPoint;
	
    Vector3 lastPosition;
    Quaternion lastRotation;
	private bool m_validTarget = false;

	public Vector3 locatorPosition { get { return locatorRoot.position; } }
	public bool validTarget { get { return m_validTarget; } }

    void Awake()
    {
        lineRenderer = GetComponent<VRLineRenderer>();
        lineRendererMeshRenderer = lineRenderer.GetComponent<MeshRenderer>();

        locatorSprite = locatorRoot.GetComponentInChildren<SpriteRenderer>();
    }

    public void Start()
    {
        if ( toolPoint == false ) toolPoint = transform;

        lineRenderer.SetVertexCount( lineSegmentCount );
        lineRenderer.useWorldSpace = true;

        motionSpheres = new Transform[motionSphereCount];
        for ( int i = 0; i < motionSphereCount; i++ )
        {
            motionSpheres[i] = ((GameObject) Instantiate(motionIndicatorSphere, toolPoint.position, toolPoint.rotation)).transform;
            motionSpheres[i].SetParent(locatorRoot);
            motionSpheres[i].name = "motion-sphere-" + i;
        }
		motionIndicatorSphere.SetActive(false);
		curveLengthEstimate = 1.0f;
        motionSphereOffset = 0.0f;
    }

    void OnEnable()
    {
        ShowLine(false);
    }

    //http://devmag.org.za/2011/04/05/bzier-curves-a-tutorial/
    Vector3 CalculateBezierPoint( float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3 )
    {
        var u = 1f - t;
        var tt = t * t;
        var uu = u * u;
        var uuu = uu * u;
        var ttt = tt * t;

        var p = uuu * p0; //first term
        p += 3f * uu * t * p1; //second term
        p += 3f * u * tt * p2; //third term
        p += ttt * p3; //fourth term

        return p;
    }

    public void DrawArc()
    {
        locatorRoot.rotation = Quaternion.identity;

        // prevent rendering line when pointing to high or low
		var pointerStrength = (toolPoint.forward.y + 1.0f)*.5f;

        if ( Mathf.Abs(pointerStrength) > maxArc)
        {
            ShowLine(false);
            return;
        }

        ShowLine(true);
		
        p0 = toolPoint.position; // start point
        // first handle -- determines how steep the first part will be
        var handle0 = toolPoint.position + toolPoint.forward * pointerStrength * range;
        p1 = handle0;

        var final = new Vector3(handle0.x, 0, handle0.z);
        p3 = final; // end point
        p2 = final; // second handle -- determines how steep the intersection with the ground will be

        // set the position of the locator
        locatorRoot.position = final + Vector3.up * 0.01f;

        m_validTarget = false;

        var colliders = Physics.OverlapSphere( final, radius, layerMask.value );
        foreach ( var collider in colliders )
        {
			m_validTarget = true;
            // todo check for invalid colliders
        }

        SetColors(validTarget ? validLocationColor : invalidLocationColor);

        // calculate and send points to the line renderer
        segmentPositions = new Vector3[lineSegmentCount];

        for ( int i = 0; i < lineSegmentCount; i++ )
        {
            var t = i / ( float ) Mathf.Max((lineSegmentCount - 1), 1);
            var q = CalculateBezierPoint( t, p0, p1, p2, p3 );
            segmentPositions[i] = q;
        }
        lineRenderer.SetPositions( segmentPositions );

		// The curve length will be somewhere between a straight line between the points 
		// and a path that directly follows the control points.  So we estimate this by just averaging the two.
		curveLengthEstimate = ((p3 - p0).magnitude + ((p1 - p0).magnitude + (p1 - p2).magnitude))*0.5f;
    }

	public void DrawMotionSpheres()
	{
		// We estimate how much we should correct our curve time by with a guess step
		// It's not perfect (a lookup would be more efficient), but it does a decent job
        for ( int i = 0; i < motionSphereCount; i++ )
        {
			var t = (i / ( float ) motionSphereCount) + motionSphereOffset;
			// We guess at our position
			motionSpheres[i].position = CalculateBezierPoint( t, p0, p1, p2, p3 );

			// If we're not at the starting point, we apply a correction factor
			if (t > 0.0f)
			{
				// We have how long we *think* the curve should be
				var lengthEstimate = (curveLengthEstimate * t);

				// We compare that to how long our distance actually is
				var correctionFactor = lengthEstimate/(motionSpheres[i].position - p0).magnitude;

				// We then scale our time value by this correction factor
				var correctedTime =  Mathf.Clamp01(t * correctionFactor);
				motionSpheres[i].position = CalculateBezierPoint( correctedTime, p0, p1, p2, p3 );				
			}	
        }
	}

    public void ShowLine(bool show = true)
    {
        locatorRoot.gameObject.SetActive(show);
        lineRendererMeshRenderer.enabled = show;
    }

    void SetColors(Color color)
    {
        locatorSprite.color = color;
        locatorTubeRenderer.material.color = color;

        lineRenderer.SetColors(color, color);

        motionSpheres[0].GetComponent<MeshRenderer>().sharedMaterial.color = color;
    }

	// Update is called once per frame
	void Update ()
    {
		motionSphereOffset = (motionSphereOffset + (Time.deltaTime * motionSphereSpeed)) % (1.0f/(float)motionSphereCount);

	    if ( lastPosition != transform.position || lastRotation != transform.rotation )
	    {
	        DrawArc();
	        lastPosition = transform.position;
	        lastRotation = transform.rotation;
	    }
		DrawMotionSpheres();
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor( typeof( VRArcRenderer ) )]
public class VRArcRendererEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VRArcRenderer myScript = ( VRArcRenderer ) target;

        GUILayout.BeginHorizontal();

        if ( GUILayout.Button( "" ) )
        {
            myScript.DrawArc();
        }
        GUILayout.EndHorizontal();
    }
}
#endif