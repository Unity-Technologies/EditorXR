using UnityEditor.Experimental.EditorVR;
using UnityEngine;

public class BlinkVisuals1 : MonoBehaviour, IUsesViewerScale, IRaycast
{
	[SerializeField]
	float m_LineWidth = 1f;

	[SerializeField]
	Color m_ValidColor;

	[SerializeField]
	Color m_InvalidColor;

	[SerializeField]
	float m_ProjectileSpeed = 250f;

	[SerializeField]
	float m_MaxProjectileSpeed = 500f;

	[SerializeField]
	float m_TimeStep = 0.0075f;

	[SerializeField]
	int m_MaxProjectileSteps = 200;

	[SerializeField]
	float m_InvalidThreshold = -0.8f;

	VRLineRenderer m_LineRenderer;
	Vector3[] m_Positions;

	public Vector3? targetPosition { get; private set; }

	public bool tooSteep
	{
		get { return Vector3.Dot(transform.forward, Physics.gravity.normalized) < m_InvalidThreshold; }
	}

	public float extraSpeed { private get; set; }

	void Start()
	{
		m_Positions = new Vector3[m_MaxProjectileSteps];

		m_LineRenderer = GetComponent<VRLineRenderer>();
		m_LineRenderer.SetPositions(m_Positions);
	}

	void Update()
	{
		targetPosition = null;

		if (tooSteep)
			gameObject.SetActive(false);

		var viewerScale = this.GetViewerScale();
		var lastPosition = transform.position;
		var timeStep = m_TimeStep * viewerScale;
		var projectileSpeed = m_ProjectileSpeed + extraSpeed * (m_MaxProjectileSpeed - m_ProjectileSpeed);
		var startVelocity = transform.forward * projectileSpeed * timeStep;
		var gravity = Physics.gravity * timeStep;
		for (var i = 0; i < m_MaxProjectileSteps; i++)
		{
			if (targetPosition.HasValue)
			{
				m_Positions[i] = targetPosition.Value;
			}
			else
			{
				var nextPosition = lastPosition + startVelocity;
				startVelocity += gravity;

				var segment = nextPosition - lastPosition;
				var ray = new Ray(lastPosition, segment);
				RaycastHit hit;
				GameObject go;
				m_Positions[i] = lastPosition;
				if (this.Raycast(ray, out hit, out go, segment.magnitude))
					targetPosition = hit.point;

				lastPosition = nextPosition;
			}
		}

		var lineWidth = m_LineWidth * viewerScale;
		m_LineRenderer.SetWidth(lineWidth, lineWidth);

		var color = targetPosition.HasValue ? m_ValidColor : m_InvalidColor;
		m_LineRenderer.SetColors(color, color);

		m_LineRenderer.SetPositions(m_Positions);
	}
}
