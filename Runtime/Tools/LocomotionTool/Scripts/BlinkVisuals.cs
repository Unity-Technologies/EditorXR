using System.Collections;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using Unity.Labs.XRLineRenderer;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEngine;

public class BlinkVisuals : MonoBehaviour, IUsesViewerScale, IUsesSceneRaycast
{
    const float k_Epsilon = 0.001f;

#pragma warning disable 649
    [SerializeField]
    float m_LineWidth = 1f;

    [SerializeField]
    Color m_ValidColor;

    [SerializeField]
    Color m_InvalidColor;

    [SerializeField]
    float m_BlinkDistance = 15f;

    [SerializeField]
    float m_MaxBlinkDistance = 50f;

    [SerializeField]
    float m_TimeStep = 0.75f;

    [SerializeField]
    int m_MaxProjectileSteps = 150;

    [SerializeField]
    int m_SphereCount = 25;

    [SerializeField]
    float m_Spherespeed = 2f;

    [SerializeField]
    float m_InvalidThreshold = -0.95f;

    [SerializeField]
    float m_TransitionTime = 0.15f;

    [SerializeField]
    GameObject m_MotionIndicatorSphere;

    [SerializeField]
    GameObject m_ArcLocator;
#pragma warning restore 649

    float m_SpherePosition;
    XRLineRenderer m_LineRenderer;
    Vector3[] m_Positions;
    Transform[] m_Spheres;
    Material m_VisualsMaterial;
    Vector3 m_SphereScale;
    bool m_Visible;
    float m_TransitionAmount;
    Coroutine m_VisibilityCoroutine;

    public Vector3? targetPosition { get; private set; }

    public float extraSpeed { private get; set; }

    public List<GameObject> ignoreList { private get; set; }

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
                this.RestartCoroutine(ref m_VisibilityCoroutine, VisibilityTransition(true));
            }
            else if (gameObject.activeInHierarchy)
            {
                this.RestartCoroutine(ref m_VisibilityCoroutine, VisibilityTransition(false));
            }
        }
    }

#if !FI_AUTOFILL
    IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
    IProvidesSceneRaycast IFunctionalitySubscriber<IProvidesSceneRaycast>.provider { get; set; }
#endif

    void Awake()
    {
        m_SphereScale = m_MotionIndicatorSphere.transform.localScale;
        m_VisualsMaterial = MaterialUtils.GetMaterialClone(m_MotionIndicatorSphere.GetComponent<Renderer>());

        m_Positions = new Vector3[m_MaxProjectileSteps];

        m_LineRenderer = GetComponent<XRLineRenderer>();
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
    }

    void OnDisable()
    {
        targetPosition = null;
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

    void Update()
    {
        targetPosition = null;
        var viewerScale = this.GetViewerScale();
        var blinkDistance = m_BlinkDistance + extraSpeed * extraSpeed * (m_MaxBlinkDistance - m_BlinkDistance);
        blinkDistance *= viewerScale;
        var gravity = Physics.gravity;
        if (gravity == Vector3.zero)
            gravity = Vector3.down; // Assume (0,-1,0) if gravity is zero

        var timeStep = m_TimeStep * viewerScale / Mathf.Sqrt(blinkDistance * gravity.magnitude);
        blinkDistance *= m_TransitionAmount;
        var speed = Mathf.Sqrt(blinkDistance * gravity.magnitude);
        var velocity = transform.forward * speed * timeStep;
        gravity *= timeStep * timeStep;

        var lastPosition = transform.position;
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
                var nextPosition = lastPosition + velocity;
                velocity += gravity;

                var segment = nextPosition - lastPosition;

                if (segment == Vector3.zero)
                    continue;

                var scaledEpsilon = k_Epsilon * viewerScale;
                var ray = new Ray(lastPosition - segment.normalized * scaledEpsilon, segment);
                RaycastHit hit;
                GameObject go;
                m_Positions[i] = lastPosition;
                if (this.Raycast(ray, out hit, out go, segment.magnitude + scaledEpsilon, ignoreList))
                    targetPosition = hit.point;

                if (i < m_SphereCount)
                {
                    var sphere = m_Spheres[i];
                    if (targetPosition.HasValue)
                    {
                        sphere.gameObject.SetActive(false);
                    }
                    else
                    {
                        if (i == 0)
                            sphere.localScale = m_SphereScale * m_SpherePosition;

                        if (i == m_SphereCount - 1)
                            sphere.localScale = m_SphereScale * (1 - m_SpherePosition);

                        m_Spheres[i].position = lastPosition + segment * m_SpherePosition;
                        m_Spheres[i].gameObject.SetActive(true);
                    }
                }

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

        if (!m_Visible || Vector3.Dot(transform.forward, gravity.normalized) < m_InvalidThreshold)
            targetPosition = null;

        var lineWidth = m_LineWidth * viewerScale;
        m_LineRenderer.widthStart = lineWidth;
        m_LineRenderer.widthEnd = lineWidth;

        var color = targetPosition.HasValue ? m_ValidColor : m_InvalidColor;
        color.a *= m_TransitionAmount * m_TransitionAmount;
        m_VisualsMaterial.SetColor("_TintColor", color);
        m_LineRenderer.colorStart = color;
        m_LineRenderer.colorEnd = color;

        m_LineRenderer.SetPositions(m_Positions);
    }

    void OnDestroy()
    {
        UnityObjectUtils.Destroy(m_VisualsMaterial);

        foreach (var sphere in m_Spheres)
        {
            UnityObjectUtils.Destroy(sphere.gameObject);
        }
    }
}
