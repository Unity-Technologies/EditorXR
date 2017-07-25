using UnityEditor.Experimental.EditorVR;
using UnityEngine;

public class BlinkVisuals1 : MonoBehaviour, IUsesViewerScale, IRaycast
{
	[SerializeField]
	float m_ProjectileSpeed = 1f;

	[SerializeField]
	float m_TimeStep = 0.01f;

	[SerializeField]
	int m_MaxProjectileSteps = 100;

	VRLineRenderer m_LineRenderer;

	public Vector3 targetPosition { get; set; }

	public bool visible
	{
		set { enabled = value; }
	}

	public bool outOfMaxRange { get; set; }

	void Start()
	{
		m_LineRenderer = GetComponent<VRLineRenderer>();
	}

	void Update()
	{
		GizmoModule.instance.DrawSphere(transform.position, 0.1f, Color.black);
		var lastPosition = transform.position;
		var timeStep = m_TimeStep * this.GetViewerScale();
		var startVelocity = transform.forward * m_ProjectileSpeed * timeStep;
		var gravity = Physics.gravity * timeStep;
		for (var i = 0; i < m_MaxProjectileSteps; i++)
		{
			var nextPosition = lastPosition + startVelocity;
			startVelocity += gravity;

			var segment = nextPosition - lastPosition;
			var ray = new Ray(lastPosition, segment);
			RaycastHit hit;
			GameObject go;
			GizmoModule.instance.DrawRay(ray.origin, ray.direction, Color.white, segment.magnitude);
			//if (this.Raycast(ray, out hit, out go, segment.magnitude))
			//{
			//	break;
			//}

			lastPosition = nextPosition;
		}
	}
}
