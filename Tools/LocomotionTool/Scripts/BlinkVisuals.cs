using System.Collections;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

public class BlinkVisuals : MonoBehaviour, IUsesViewerScale, IRaycast
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
	int m_SphereCount = 25;

	[SerializeField]
	float m_Spherespeed = 1f;

	[SerializeField]
	float m_InvalidThreshold = -0.8f;

	[SerializeField]
	float m_TransitionTime = 0.3f;

	[SerializeField]
	GameObject m_MotionIndicatorSphere;

	[SerializeField]
	GameObject m_ArcLocator;

	float m_SpherePosition;
	VRLineRenderer m_LineRenderer;
	Vector3[] m_Positions;
	Transform[] m_Spheres;
	Material m_VisualsMaterial;
	Vector3 m_SphereScale;
	bool m_Visible;
	float m_TransitionAmount;

	public Vector3? targetPosition { get; private set; }

	public float extraSpeed { private get; set; }

	public bool visible
	{
		set
		{
			if (value == m_Visible)
				return;

			m_Visible = value;

			if (m_Visible)
			{
				gameObject.SetActive(true);
			}
			else
			{
				StartCoroutine(VisibilityTransition(false));
			}
		}
	}

	void Awake()
	{
		m_SphereScale = m_MotionIndicatorSphere.transform.localScale;
		m_VisualsMaterial = MaterialUtils.GetMaterialClone(m_MotionIndicatorSphere.GetComponent<Renderer>());

		m_Positions = new Vector3[m_MaxProjectileSteps];

		m_LineRenderer = GetComponent<VRLineRenderer>();
		m_LineRenderer.SetPositions(m_Positions, true);

		m_Spheres = new Transform[m_SphereCount];
		for (var i = 0; i < m_SphereCount; i++)
		{
			m_Spheres[i] = Instantiate(m_MotionIndicatorSphere, transform, false).transform;
		}

		foreach (var renderer in m_ArcLocator.GetComponentsInChildren<Renderer>(true))
		{
			renderer.sharedMaterial = m_VisualsMaterial;
		}
	}

	void OnEnable()
	{
		for (var i = 0; i < m_MaxProjectileSteps; i++)
		{
			m_Positions[i] = transform.position;
		}

		m_LineRenderer.SetPositions(m_Positions);

		StartCoroutine(VisibilityTransition(true));
	}

	IEnumerator VisibilityTransition(bool visible)
	{
		var startValue = m_TransitionAmount;
		var targetValue = visible ? 1f : 0f;
		var startTime = Time.time;
		var timeDiff = Time.time - startTime;
		while (timeDiff < m_TransitionTime)
		{
			m_TransitionAmount = Mathf.Lerp(startValue, targetValue, timeDiff / m_TransitionTime);
			timeDiff = Time.time - startTime;
			yield return null;
		}

		m_TransitionAmount = targetValue;

		if (!visible)
			gameObject.SetActive(false);
	}

	void OnDisable()
	{
		StopAllCoroutines();
		m_TransitionAmount = 0;
	}

	void Update()
	{
		targetPosition = null;

		if (Mathf.Approximately(m_TransitionAmount, 0))
			return;

		var viewerScale = this.GetViewerScale();
		var lastPosition = transform.position;
		var timeStep = m_TimeStep * viewerScale;
		var projectileSpeed = m_ProjectileSpeed + extraSpeed * (m_MaxProjectileSpeed - m_ProjectileSpeed);
		projectileSpeed *= m_TransitionAmount;
		var startVelocity = transform.forward * projectileSpeed * timeStep;
		var gravity = Physics.gravity * timeStep;
		m_SpherePosition = (m_SpherePosition + Time.deltaTime * m_Spherespeed) % 1;
		for (var i = 0; i < m_MaxProjectileSteps; i++)
		{
			if (targetPosition.HasValue)
			{
				m_Positions[i] = targetPosition.Value;
				if (i < m_SphereCount)
					m_Spheres[i].gameObject.SetActive(false);
			}
			else
			{
				var nextPosition = lastPosition + startVelocity;
				startVelocity += gravity;

				var segment = nextPosition - lastPosition;

				if (i < m_SphereCount)
				{
					var sphere = m_Spheres[i];
					if (i == 0)
						sphere.localScale = m_SphereScale * m_SpherePosition;

					if (i == m_SphereCount - 1)
						sphere.localScale = m_SphereScale * (1 - m_SpherePosition);

					m_Spheres[i].position = lastPosition + segment * m_SpherePosition;
					m_Spheres[i].gameObject.SetActive(true);
				}

				var ray = new Ray(lastPosition, segment);
				RaycastHit hit;
				GameObject go;
				m_Positions[i] = lastPosition;
				if (this.Raycast(ray, out hit, out go, segment.magnitude))
					targetPosition = hit.point;

				lastPosition = nextPosition;
			}
		}

		if (targetPosition.HasValue)
		{
			m_ArcLocator.SetActive(true);
			m_ArcLocator.transform.rotation = Quaternion.identity;
			m_ArcLocator.transform.position = targetPosition.Value;
		}
		else
		{
			m_ArcLocator.SetActive(false);
		}

		if (Vector3.Dot(transform.forward, Physics.gravity.normalized) < m_InvalidThreshold)
			targetPosition = null;

		var lineWidth = m_LineWidth * viewerScale;
		m_LineRenderer.SetWidth(lineWidth, lineWidth);

		var color = targetPosition.HasValue ? m_ValidColor : m_InvalidColor;
		color.a *= m_TransitionAmount * m_TransitionAmount;
		m_VisualsMaterial.SetColor("_TintColor", color);
		m_LineRenderer.SetColors(color, color);

		m_LineRenderer.SetPositions(m_Positions);
	}

	void OnDestroy()
	{
		ObjectUtils.Destroy(m_VisualsMaterial);

		foreach (var sphere in m_Spheres)
		{
			ObjectUtils.Destroy(sphere.gameObject);
		}
	}
}
