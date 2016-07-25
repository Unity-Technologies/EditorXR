using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class BlinkVisuals : MonoBehaviour
{
    #region Enums
    private enum State
    {
        Inactive = 0,
        TransitioningIn = 1,
        TransitioningOut = 2
    }
    #endregion

    #region Delegates
    public Action<Vector3> ValidTargetFound;
    #endregion
    
    #region Properties
    public Vector3 locatorPosition { get { return locatorRoot.position; } }
	public Transform locatorRoot { get { return m_LocatorRoot; } }
	public bool validTarget { get { return m_ValidTarget; } }

    private float pointerStrength { get { return (m_ToolPoint.forward.y + 1.0f) * 0.5f; } }
    #endregion

    #region Public Fields
    public Color invalidLocationColor;
    public LayerMask layerMask = -1;
    public int lineSegmentCount = 10;
	public float maxArc = 0.85f;
    public float radius = 0.5f;
    public float range = 5f; // todo make this work for horizontal aiming
	public Color validLocationColor;
    #endregion

    #region Private Serialized Fields
    [SerializeField]
	private VRLineRenderer m_LineRenderer;
	[SerializeField]
	private Transform m_LocatorRoot;
    [SerializeField]
    public GameObject m_MotionIndicatorSphere;
    [SerializeField]
	private int m_MotionSphereCount = 10;
	[SerializeField]
	private MeshRenderer m_RingRenderer;
    [SerializeField] [Tooltip("Room-Scale mesh that will be instantiated to visually represent the surrounding room-scale bounds.")]
	private MeshRenderer m_RoomScaleRenderer;
	[SerializeField]
	private MeshRenderer m_TubeRenderer;
    #endregion

    #region Private Fields
    private float m_ArcEndHeight = -2f;
	private float m_CurveLengthEstimate;
    private Vector3? m_DetachedWorldArcPosition = null;
    private Vector3 m_FinalPosition;
    private Vector3 m_LastPosition;
    private Quaternion m_LastRotation;
    private MeshRenderer m_LineRendererMeshRenderer;
    private float m_MotionSphereOffset;
    private Transform[] m_MotionSpheres;
    private Vector3 m_MotionSphereOriginalScale;
	private float m_MotionSphereSpeed = 0.125f;
    private float m_MovementMagnitudeDelta;
    private Vector3 m_MovementVelocityDelta;
    private bool m_OutOfMaxRange;
	private Vector3 m_Point0, m_Point1, m_Point2, m_Point3;
	private Transform m_RingTransform;
    private Vector3 m_RingTransformOriginalScale;
    private Vector3 m_RoomscaleLazyPosition;
	private Transform m_RoomScaleTransform;
    private Vector3[] m_SegmentPositions;
    private State m_State = State.Inactive;
	private readonly string m_TintColor = "_TintColor";
	private Transform m_ToolPoint;
    private Transform m_Transform;
	private Transform m_TubeTransform;
    private Vector3 m_TubeTransformHiddenScale;
    private Vector3 m_TubeTransformOriginalScale;
	private bool m_ValidTarget = false;
    #endregion

    #region Initialization & LifeCycle
    void Awake()
    {
        m_LineRenderer = GetComponent<VRLineRenderer>();
        m_LineRendererMeshRenderer = m_LineRenderer.GetComponent<MeshRenderer>();
    }

    public void Start()
    {
        m_Transform = transform;

        if ( m_ToolPoint == false ) m_ToolPoint = transform;

        m_LineRenderer.SetVertexCount( lineSegmentCount );
        m_LineRenderer.useWorldSpace = true;

        m_MotionSpheres = new Transform[m_MotionSphereCount];
        for ( int i = 0; i < m_MotionSphereCount; i++ )
        {
            m_MotionSpheres[i] = ((GameObject) Instantiate(m_MotionIndicatorSphere, m_ToolPoint.position, m_ToolPoint.rotation)).transform;
            m_MotionSpheres[i].SetParent(m_Transform);
            m_MotionSpheres[i].name = "motion-sphere-" + i;
            m_MotionSpheres[i].gameObject.SetActive(false);
        }
	    m_MotionSphereOriginalScale = m_MotionSpheres[0].localScale;
		m_CurveLengthEstimate = 1.0f;
        m_MotionSphereOffset = 0.0f;

	    m_RoomScaleTransform = m_RoomScaleRenderer.transform;
		m_RoomScaleTransform.parent = locatorRoot;
		m_RoomScaleTransform.localPosition = Vector3.zero;
		m_RoomScaleTransform.localRotation = Quaternion.identity;
        
	    m_RingTransform = m_RingRenderer.transform;
	    m_RingTransformOriginalScale = m_RingTransform.localScale;
        m_TubeTransform = m_TubeRenderer.transform;
        m_TubeTransformOriginalScale = m_TubeTransform.localScale;
        m_TubeTransformHiddenScale = new Vector3(m_TubeTransform.localScale.x, 0.0001f, m_TubeTransform.localScale.z);

        ShowLine(false);
    }
    #endregion

    void Update ()
    {
		if (m_State != State.Inactive)
		{
			m_MotionSphereOffset = (m_MotionSphereOffset + (UnityEngine.Time.unscaledDeltaTime * m_MotionSphereSpeed)) % (1.0f / (float) m_MotionSphereCount);

			if (m_LastPosition != m_Transform.position || m_LastRotation != m_Transform.rotation)
			{
				DrawArc();
                m_LastPosition = m_Transform.position;
				m_LastRotation = m_Transform.rotation;
			}
			DrawMotionSpheres();
            
            m_RoomScaleTransform.position = Vector3.SmoothDamp(m_RoomscaleLazyPosition, m_LocatorRoot.position, ref m_MovementVelocityDelta, 0.35f, 100f, Time.unscaledDeltaTime * 4);
            m_RoomscaleLazyPosition = m_RoomScaleTransform.position;
            m_MovementMagnitudeDelta = (m_RoomScaleTransform.position - m_LocatorRoot.position).magnitude;
            m_TubeTransform.localScale = Vector3.Lerp(m_TubeTransformHiddenScale, m_TubeTransformOriginalScale, 1f / (m_MovementMagnitudeDelta * 6f));
		}
        else if (m_OutOfMaxRange && Mathf.Abs(pointerStrength) < maxArc)
        {
            m_OutOfMaxRange = false;
            ShowVisuals();
        }
    }

    private void ValidateTargetSelection()
    {
        if (validTarget)
        {
            Action<Vector3> validTargetFoundHandler = ValidTargetFound;
            if (validTargetFoundHandler != null)
                validTargetFoundHandler(locatorPosition);
        }
    }

	public void ShowVisuals()
	{
	    if (m_State == State.Inactive || m_State == State.TransitioningOut)
	    {
            m_RoomscaleLazyPosition = m_RoomScaleTransform.position;

            for (int i = 0; i < m_MotionSphereCount; ++i)
                m_MotionSpheres[i].gameObject.SetActive(true);

            StartCoroutine(AnimateShowVisuals());
	    }
	}

	public void HideVisuals(bool validateTarget = true)
	{
        if (m_State != State.Inactive)
            StartCoroutine(AnimateHideVisuals(validateTarget));

        if (validateTarget)
	        m_OutOfMaxRange = false;
	}

	private IEnumerator AnimateShowVisuals()
	{
        m_State = State.TransitioningIn;
	    m_RoomScaleTransform.position = m_FinalPosition + Vector3.up * 0.01f;
        ShowLine();
		float scale = 0f;
		float tubeScale = m_TubeTransform.localScale.x;

		for (int i = 0; i < m_MotionSphereCount; ++i)
		{
            m_MotionSpheres[i].localScale = m_MotionSphereOriginalScale;
		}

		while (m_State == State.TransitioningIn && scale < 1)
		{
			m_TubeTransform.localScale = new Vector3(tubeScale, scale, tubeScale);
			locatorRoot.localScale = Vector3.one * scale;
			m_LineRenderer.SetWidth(scale, scale);
			scale = UnityEngine.VR.Utilities.U.Math.Ease(scale, 1f, 8, 0.05f);
			yield return null;
		}

	    if (m_State == State.TransitioningIn)
	    {
            // HARD SET of values if still in this state
	        //foreach (var pointerRayRenderer in DefaultVrLineRenderers)
	            //pointerRayRenderer.SetWidth(0, 0);
	    }
	}

	private IEnumerator AnimateHideVisuals(bool validateTarget = true)
	{
        if (validateTarget)
	        ValidateTargetSelection();

        m_State = State.TransitioningOut;
        m_DetachedWorldArcPosition = m_LocatorRoot.position;
		float scale = 1f;
		float tubeScale = m_TubeTransform.localScale.x;
        
        while (m_State == State.TransitioningOut && scale > 0.0001f)
		{
			SetColors(Color.Lerp(validTarget == true ? validLocationColor : invalidLocationColor, Color.clear, 1-scale));
			m_TubeTransform.localScale = new Vector3(tubeScale, scale, tubeScale);
			m_LineRenderer.SetWidth(scale, scale);
			scale = UnityEngine.VR.Utilities.U.Math.Ease(scale, 0f, 8, 0.0005f);
			m_RingTransform.localScale = Vector3.Lerp(m_RingTransform.localScale, m_RingTransformOriginalScale, scale);
			yield return null;
		}

	    m_DetachedWorldArcPosition = null;

	    // set value if no additional transition hasn begun
	    if (m_State == State.TransitioningOut)
	    {
		    m_RingTransform.localScale = m_RingTransformOriginalScale;
	        m_State = State.Inactive;
            m_LineRenderer.SetWidth(0f, 0f);
		    ShowLine(false);

            for (int i = 0; i < m_MotionSphereCount; ++i)
	            m_MotionSpheres[i].gameObject.SetActive(false);
	    }
	}
    
    Vector3 CalculateBezierPoint( float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3 )
    {
        var u = 1f - t;
        var tt = t * t;
        var uu = u * u;
        var uuu = uu * u;
        var ttt = tt * t;

        //first term
        var p = uuu * p0;
        //second term
        p += 3f * uu * t * p1;
        //third term
        p += 3f * u * tt * p2;
        //fourth term
        p += ttt * p3;

        return p;
    }

    public void DrawArc()
    {
        locatorRoot.rotation = Quaternion.identity;

        // prevent rendering line when pointing to high or low
        if (Mathf.Abs(pointerStrength) > maxArc)
        {
            m_ValidTarget = false;
            m_OutOfMaxRange = true;
			StartCoroutine(AnimateHideVisuals(false));
			return;
        }

        // start point
        m_Point0 = m_ToolPoint.position;
        // first handle -- determines how steep the first part will be
        var handle0 = m_ToolPoint.position + m_ToolPoint.forward * pointerStrength * range;
        m_Point1 = handle0;

        m_FinalPosition = new Vector3(handle0.x, m_ArcEndHeight, handle0.z);
        // end point
        m_Point3 = m_FinalPosition;
        // second handle -- determines how steep the intersection with the ground will be
        m_Point2 = m_FinalPosition;

		// set the position of the locator
		locatorRoot.position = m_DetachedWorldArcPosition == null ? m_FinalPosition + Vector3.up * 0.01f : (Vector3)m_DetachedWorldArcPosition;

        m_ValidTarget = false;

        // TODO: Switch to support for the new EVR collider-free system
        var colliders = Physics.OverlapSphere(m_FinalPosition, radius, layerMask.value);
        m_ValidTarget = colliders != null && colliders.Length > 0;

        SetColors(validTarget ? validLocationColor : invalidLocationColor);

        // calculate and send points to the line renderer
        m_SegmentPositions = new Vector3[lineSegmentCount];

        for ( int i = 0; i < lineSegmentCount; i++ )
        {
            var t = i / (float) Mathf.Max((lineSegmentCount - 1), 1);
            var q = CalculateBezierPoint( t, m_Point0, m_Point1, m_Point2, m_Point3 );
            m_SegmentPositions[i] = q;
        }
        m_LineRenderer.SetPositions(m_SegmentPositions);

		// The curve length will be somewhere between a straight line between the points 
		// and a path that directly follows the control points.  So we estimate this by just averaging the two.
		m_CurveLengthEstimate = ((m_Point3 - m_Point0).magnitude + ((m_Point1 - m_Point0).magnitude + (m_Point1 - m_Point2).magnitude))*0.5f;
    }

	public void DrawMotionSpheres()
	{
		// We estimate how much we should correct our curve time by with a guess step
        for ( int i = 0; i < m_MotionSphereCount; ++i )
        {
			var t = (i / ( float ) m_MotionSphereCount) + m_MotionSphereOffset;
			m_MotionSpheres[i].position = CalculateBezierPoint( t, m_Point0, m_Point1, m_Point2, m_Point3 );
            float validTargetEase = m_State == State.TransitioningIn ? (m_ValidTarget == true ? m_MotionSphereOriginalScale.x : 0.05f) : 0f;
            validTargetEase = U.Math.Ease(m_MotionSpheres[i].localScale.x, validTargetEase, 16, 0.0005f) * Mathf.Min((m_Transform.position - m_MotionSpheres[i].position).magnitude * 4, 1f);
            m_MotionSpheres[i].localScale = Vector3.one * validTargetEase;
            m_MotionSpheres[i].localRotation = Quaternion.identity;

            // If we're not at the starting point, we apply a correction factor
            if (t > 0.0f)
			{
				// We have how long we *think* the curve should be
				var lengthEstimate = (m_CurveLengthEstimate * t);

				// We compare that to how long our distance actually is
				var correctionFactor = lengthEstimate/(m_MotionSpheres[i].position - m_Point0).magnitude;

				// We then scale our time value by this correction factor
				var correctedTime =  Mathf.Clamp01(t * correctionFactor);
				m_MotionSpheres[i].position = CalculateBezierPoint( correctedTime, m_Point0, m_Point1, m_Point2, m_Point3 );				
			}
        }
	}

    public void ShowLine(bool show = true)
    {
        locatorRoot.gameObject.SetActive(show);
        m_LineRendererMeshRenderer.enabled = show;

        if (!show)
        {
            m_RoomScaleRenderer.sharedMaterial.SetColor(m_TintColor, Color.clear);

            for (int i = 0; i < m_MotionSphereCount; ++i)
                m_MotionSpheres[i].localScale = Vector3.zero;
        }
    }

    void SetColors(Color color)
    {
        m_LineRenderer.SetColors(color, color);
        m_MotionSpheres[0].GetComponent<MeshRenderer>().sharedMaterial.color = color;
        // Set the color for all object sharind the blink material
	    m_RoomScaleRenderer.sharedMaterial.SetColor(m_TintColor, color);
	}
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor( typeof( BlinkVisuals ) )]
public class BlinkVisualsEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

		BlinkVisuals blink = (BlinkVisuals) target;

        GUILayout.BeginHorizontal();

        if (GUILayout.Button(""))
            blink.DrawArc();

        GUILayout.EndHorizontal();
    }
}
#endif