using UnityEngine;

/// <summary>
/// A VR-Focused drop-in replacement for the Line Renderer
/// This renderer draws fixed-width lines with simulated volume and glow.
/// This has many of the advantages of the traditional Line Renderer, old-school system-level line rendering functions, and volumetric (a linked series of capsules or cubes) rendering
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class VRTrailRenderer : MonoBehaviour
{
	// TrailRenderer Interface
	public float time
	{
		get 
		{ 
			return m_Time; 
		}
		set 
		{ 
			m_Time = value; 
		}
	}
	[SerializeField]
	protected float m_Time = 5.0f;

	public float startWidth
	{
		get
		{
			return m_StartWidth;
		}
		set
		{
			m_StartWidth = value;
		}
	}
	[SerializeField]
	protected float m_StartWidth = 1.0f;

	public float endWidth
	{
		get
		{
			return m_EndWidth;
		}
		set
		{
			m_EndWidth = value;
		}
	}
	[SerializeField]
	protected float m_EndWidth = 1.0f;

	public float minVertexDistance
	{
		get
		{
			return m_MinVertexDistance;
		}
		set
		{
			m_MinVertexDistance = value;
		}
	}
	[SerializeField]
	protected float m_MinVertexDistance = 0.1f;
	
	public bool autodestruct
	{
		get
		{
			return m_Autodestruct;
		}
		set
		{
			m_Autodestruct = value;
		}
	}
	[SerializeField]
	protected bool m_Autodestruct = false;
	
	public bool smoothInterpolation
	{
		get
		{
			return m_SmoothInterpolation;
		}
		set
		{
			m_SmoothInterpolation = value;
		}
	}
	[SerializeField]
	public bool m_SmoothInterpolation = false;	// With this enabled, the last point will smooth lerp between the last recorded anchor point and the one after it

	// Stored Trail Data
	[SerializeField]
	protected int m_MaxTrailPoints = 20;	// How many points to store for tracing.  

	[SerializeField]
	protected bool m_StealLastPointWhenEmpty = true;	// Whether to use the last point or the first point of the trail when more are needed and none are available

	[SerializeField]
	protected Color[] m_Colors;

	// Circular array support for trail point recording
	protected Vector3[] m_Points;
	protected float[] m_PointTimes;
	protected int m_PointIndexStart = 0;
	protected int m_PointIndexEnd = 0;


	// Cached Data
	protected VRLineRendererInternals.MeshChain m_MeshData;
	protected bool m_MeshNeedsRefreshing = false;
	protected Renderer m_MeshRenderer;
	protected Vector3 m_LastRecordedPoint = Vector3.zero;

	protected int m_UsedPoints = 0;			// How many points we are currently rendering - for size/color blending
	protected float m_LastPointTime = 0.0f;	

	protected float m_EditorDeltaHelper = 0.0f;	// This lets us have access to a time data while not in play mode


	/// <summary>
	/// Ensures the trails have all their data precached upon loading
	/// </summary>
	public void Awake() 
	{
		m_MeshRenderer = GetComponent<Renderer>();
		Initialize();
	}

	/// <summary>
	/// Updates the built-in mesh data for each control point of the trail
	/// </summary>
	public void LateUpdate()
	{
		// We do  the actual internal mesh updating as late as possible so nothing ends up a frame behind
		var deltaTime = Time.deltaTime;

		// We give the editor a little help with handling delta time in edit mode
		if (Application.isPlaying == false)
		{
			deltaTime = Time.realtimeSinceStartup - m_EditorDeltaHelper;
			m_EditorDeltaHelper = Time.realtimeSinceStartup;
		}
		
		// Get the current position of the renderer
		var currentPoint = transform.position;
		var pointDistance = (currentPoint - m_LastRecordedPoint).sqrMagnitude;

		// Is it more than minVertexDistance from the last position?
		if (pointDistance > (m_MinVertexDistance*m_MinVertexDistance))
		{
			// In the situation we have no points, we need to record the start point as well
			if (m_PointIndexStart == m_PointIndexEnd)
			{
				m_Points[m_PointIndexStart] = m_LastRecordedPoint;
				m_PointTimes[m_PointIndexStart] = m_Time;
			}

			// Make space for a new point
			var newEndIndex = (m_PointIndexEnd + 1) % m_MaxTrailPoints;
			
			// In the situation that we are rendering all available vertices
			// We can either keep using the current point, or take the last point, depending on the user's preference
			if (newEndIndex != m_PointIndexStart)
			{
				m_PointIndexEnd = newEndIndex;
				m_PointTimes[m_PointIndexEnd] = 0;
				m_UsedPoints++;
			}
			else
			{
				if (m_StealLastPointWhenEmpty)
				{
					m_MeshData.SetElementSize(m_PointIndexStart * 2, 0);
					m_MeshData.SetElementSize((m_PointIndexStart * 2) + 1, 0);
					m_PointIndexStart = (m_PointIndexStart + 1) % m_MaxTrailPoints;
					m_PointIndexEnd = newEndIndex;
					m_PointTimes[m_PointIndexEnd] = 0;
					m_LastPointTime = m_PointTimes[m_PointIndexStart];
				}
			}

			m_Points[m_PointIndexEnd] = currentPoint;

			// Update the last recorded point
			m_LastRecordedPoint = currentPoint;
		}
		// Do time processing
		// The end point counts up to a maximum of 'time'
		m_PointTimes[m_PointIndexEnd] = Mathf.Min(m_PointTimes[m_PointIndexEnd] + deltaTime, m_Time);

		if (m_PointIndexStart != m_PointIndexEnd)
		{
			// Run down the counter on the start point
			m_PointTimes[m_PointIndexStart] -= deltaTime;
			
			// If we've hit 0, this point is done for
			if (m_PointTimes[m_PointIndexStart] <= 0.0f)
			{
				m_MeshData.SetElementSize(m_PointIndexStart * 2, 0);
				m_MeshData.SetElementSize((m_PointIndexStart * 2) + 1, 0);
				m_PointIndexStart = (m_PointIndexStart + 1) % m_MaxTrailPoints;
				m_LastPointTime = m_PointTimes[m_PointIndexStart];
				m_UsedPoints--;
			}
		}
		
		if (m_PointIndexStart != m_PointIndexEnd)
		{
			m_MeshNeedsRefreshing = true;
			m_MeshRenderer.enabled = true;
		}
		else
		{
			m_MeshNeedsRefreshing = false;
			m_MeshRenderer.enabled = false;
		}
		if (m_MeshNeedsRefreshing == true)
		{
			m_MeshRenderer.enabled = true;

			// Update first and last points position-wise
			var nextIndex = (m_PointIndexStart + 1) % m_MaxTrailPoints;
			if (m_SmoothInterpolation)
			{
				var toNextPoint = 1.0f - (m_PointTimes[m_PointIndexStart] / m_LastPointTime);
				var lerpPoint = Vector3.Lerp(m_Points[m_PointIndexStart], m_Points[nextIndex], toNextPoint);
				m_MeshData.SetElementPosition((m_PointIndexStart * 2), ref lerpPoint);
				m_MeshData.SetElementPipe((m_PointIndexStart * 2) + 1, ref lerpPoint, ref m_Points[nextIndex]);
			}
			else
			{
				m_MeshData.SetElementPosition((m_PointIndexStart * 2), ref m_Points[m_PointIndexStart]);
				m_MeshData.SetElementPipe((m_PointIndexStart * 2) + 1, ref m_Points[m_PointIndexStart], ref m_Points[nextIndex]);
			}
			
			var prevIndex = m_PointIndexEnd - 1;
			if (prevIndex < 0)
			{
				prevIndex = m_MaxTrailPoints - 1;
			}

			m_MeshData.SetElementPipe((prevIndex * 2) + 1, ref m_Points[prevIndex], ref m_Points[m_PointIndexEnd]);
			m_MeshData.SetElementPosition((m_PointIndexEnd * 2), ref m_Points[m_PointIndexEnd]);
			

			// Go through all points and update size and color
			var pointUpdateCounter = m_PointIndexStart;
			float pointCount = 0;
			var blendStep = 1.0f / m_UsedPoints;
			var colorValue = 1.0f;

			while (pointUpdateCounter != m_PointIndexEnd)
			{
				var currentBlend = blendStep * pointCount;
				var nextBlend = blendStep * (pointCount + 1.0f);

				var currentWidth = Mathf.Lerp(m_EndWidth, m_StartWidth, currentBlend);
				var nextWidth = Mathf.Lerp(m_EndWidth, m_StartWidth, nextBlend);

				m_MeshData.SetElementSize(pointUpdateCounter * 2, currentWidth);
				m_MeshData.SetElementSize((pointUpdateCounter * 2) + 1, currentWidth, nextWidth);

				var currentColor = GetLerpedColor(colorValue);
				var nextColor = GetLerpedColor(colorValue - blendStep);
				
				m_MeshData.SetElementColor(pointUpdateCounter * 2, ref currentColor);
				m_MeshData.SetElementColor((pointUpdateCounter * 2) + 1, ref currentColor, ref nextColor);

				pointUpdateCounter = (pointUpdateCounter + 1) % m_MaxTrailPoints;
				pointCount++;
				colorValue -= blendStep;
			}

			m_MeshData.SetElementSize((m_PointIndexEnd * 2), m_StartWidth);
			m_MeshData.SetElementColor((m_PointIndexEnd * 2), ref m_Colors[0]);

			m_MeshData.SetMeshDataDirty(VRLineRendererInternals.MeshChain.MeshRefreshFlag.All);

			m_MeshData.RefreshMesh();
		}
	}

	/// <summary>
	/// Editor helper function to ensure changes are reflected in edit-mode
	/// </summary>
	public void EditorCheckForUpdate()
	{
		// If we did not initialize, refresh all the properties instead
		Initialize();
	}

	// TrailRenderer Functions
	/// <summary>
	/// Removes all points from the TrailRenderer. Useful for restarting a trail from a new position.
	/// </summary>
	public void Clear()
	{
		m_PointIndexStart = 0;
		m_PointIndexEnd = 0;
		m_LastRecordedPoint = transform.position;
	}

	/// <summary>
	/// Retrieves the blended color through any point on the trail's color chain
	/// </summary>
	/// <param name="percent">How far along the trail's color chain to get the color</param>
	/// <returns>The blended color</returns>
	protected Color GetLerpedColor(float percent)
	{
		var stretchedColorValue = percent * (m_Colors.Length);
		var curColorIndex = Mathf.Clamp(Mathf.FloorToInt(stretchedColorValue), 0, m_Colors.Length - 1);
		var nextColorIndex = Mathf.Clamp(Mathf.FloorToInt(stretchedColorValue + 1), 0, m_Colors.Length - 1);
		var blendValue = stretchedColorValue % 1.0f;

		return Color.Lerp(m_Colors[curColorIndex], m_Colors[nextColorIndex], blendValue);
	}

	/// <summary>
	/// Ensures the mesh data for the renderer is created, and updates it if neccessary
	/// </summary>
	/// <param name="force">Whether or not to force a full rebuild of the mesh data</param>
	/// <returns>True if an initialization occurred, false if it was skipped</returns>
	protected bool Initialize()
	{
		m_MaxTrailPoints = Mathf.Max(m_MaxTrailPoints, 3);
		// If we have a point mismatch, we force this operation
		if (m_Points != null && m_MaxTrailPoints == m_Points.Length)
		{
			return false;
		}
		
		m_Points = new Vector3[m_MaxTrailPoints];
		m_PointTimes = new float[m_MaxTrailPoints];
		Clear();

		if (m_Colors == null || m_Colors.Length == 0)
		{
			m_Colors = new Color[1];
			m_Colors[0] = Color.white;
		}

		// For a trail renderer we assume one big chain
		// We need a control point for each billboard and a control point for each pipe connecting them together
		// We make this a circular trail so the update logic is easier.  This gives us (position * 2)
		var neededPoints = Mathf.Max((m_MaxTrailPoints * 2), 0);

		if (m_MeshData == null)
		{
			m_MeshData = new VRLineRendererInternals.MeshChain();
		}

		if (m_MeshData.reservedElements != neededPoints)
		{
			m_MeshData.worldSpaceData = true;
			m_MeshData.GenerateMesh(gameObject, true, neededPoints);

			if (neededPoints == 0)
			{
				return true;
			}

			var pointCounter = 0;
			var elementCounter = 0;
			var zeroVec = Vector3.zero;
			var zeroColor = new Color(0,0,0,0);

			// Initialize everything to 0 so we don't render any trails at first
			m_MeshData.SetElementColor(0, ref zeroColor);
			while (pointCounter < m_Points.Length)
			{
				// Start point
				m_MeshData.SetElementSize(elementCounter, 0);
				m_MeshData.SetElementPosition(elementCounter, ref zeroVec);
				elementCounter++;

				// Pipe to the next point
				m_MeshData.SetElementSize(elementCounter, 0);
				m_MeshData.SetElementPipe(elementCounter, ref zeroVec, ref zeroVec);

				// Go onto the next point while retaining previous values we might need to lerp between
				elementCounter++;
				pointCounter++;
			}

			// Dirty all the MeshChain flags so everything gets refreshed
			m_MeshRenderer.enabled = false;
			m_MeshData.SetMeshDataDirty(VRLineRendererInternals.MeshChain.MeshRefreshFlag.All);
			m_MeshNeedsRefreshing = true;
		}
		return true;
	}
}
