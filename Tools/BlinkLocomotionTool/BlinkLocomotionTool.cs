using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.UI;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;

[ExecuteInEditMode]
public class BlinkLocomotionTool : MonoBehaviour, ITool, ILocomotion, ICustomRay, ICustomActionMap
{
    public enum BlinkMode
	{
		Move = 0,
		Fade = 1
	}

	private enum State
	{
		Inactive = 0,
		TransitioningIn = 1,
		TransitioningOut = 2,
        Moving = 3
	}

    public ActionMap actionMap { get { return m_BlinkActionMap; } }
    
    public ActionMapInput actionMapInput
    {
        get { return m_BlinkLocomotionInput; }
        set { m_BlinkLocomotionInput = (BlinkLocomotion) value; }
    }

    public BlinkMode blinkMode { get { return m_BlinkMode; } set { value = m_BlinkMode; } }

    public List<VRLineRenderer> defaultProxyLineRenderers { get { return m_DefaultProxyLineRenderers; } set { m_DefaultProxyLineRenderers = value; } }

    public Transform rayOrigin { get { return m_RayOrigin; } set { m_RayOrigin = value; } }

	public Transform viewerPivot { set { m_ViewerPivot = value; } }

	public List<VRLineRenderer> m_DefaultProxyLineRenderers;
    
    [SerializeField]
    private ActionMap m_BlinkActionMap;
    [SerializeField]
    private GameObject m_BlinkVisualsPrefab;
    [SerializeField]
    private bool m_DoSmoothing = true;
	[SerializeField]
	private bool m_EnableFadeMode = false; // TODO: return fade support to the tool after locomotion is in
    [SerializeField] [Tooltip("UI plane that will be instantiated in front of the user's vision and provide the fade color.")]
	private GameObject m_FadeImagePrefab;
    [SerializeField] [Tooltip("Total fade time (fade in / fade out each take half this amount)")]
	private float m_FadeTime = 0.5f;
	[SerializeField] [Range(0f, 1f)] [Tooltip("Lerp amount. Closer to 0 = smoother, closer to 1 = faster.")]
	private float m_IndicatorSmoothing = 0.2f;
    [SerializeField]
    private VRLineRenderer m_CustomPointerRayPrefab;

	private GameObject m_BlinkArcGO;
	private BlinkVisuals m_BlinkVisuals;
	private BlinkLocomotion m_BlinkLocomotionInput;
    private BlinkMode m_BlinkMode = BlinkMode.Move;
    private RaycastHit m_BlinkTargetHit;
    private Plane m_DefaultGroundPlane;
    private Image m_FadeImage;
    private GameObject m_FadeImageGO;
    private readonly Color m_FadeInColor = Color.black;
	private readonly Color m_FadeOutColor = new Color(0f, 0f, 0f, 0f);
    private float m_InitialDefaultLineRendererWidth;
    private readonly Color m_InvalidTargetColor = new Color(1f, 0.25f, 0.25f, 0.125f);
    private float m_MovementSpeed = 8f;
    private Vector3 m_OriginalTipScale;
	private Transform[] m_PointerTips;
	private Transform m_RayOrigin;
	private Transform m_RoomScale;
    private float m_StartingElevation;
	private State m_State = State.Inactive;
    private Transform m_TrackingCenter;
	private Transform m_ViewerPivot;

	void Start()
    {
        Debug.LogWarning("<color=yellow>TODO: Remove all FADE functionality/references/etc, support only movement to destination point.</color>");
        Debug.LogWarning("<color=orange>TODO: Fix multiple BlinkVisuals bein created at root of hierarchy when spawning Blink!</color>");
        Debug.Log("TODO: Optimization pass on blink.");
        Debug.Log("TODO: Perform Unity-style code formatting/cleanups");
        // Creating a default plane to raycast against, so that the user can blink around without having to create a ground first. 
        m_DefaultGroundPlane = new Plane(Vector3.up, 0);
        m_FadeTime = m_FadeTime / 2f; // Fade in/out are each half of the total.
        m_ViewerPivot = transform; // TODO: remove, just to get compiling and testing
	    m_StartingElevation = m_ViewerPivot.position.y;
		m_TrackingCenter = U.Object.CreateEmptyGameObject("Blink Tracking Center Indicator", m_ViewerPivot).transform;
		
        m_BlinkArcGO = U.Object.InstantiateAndSetActive(m_BlinkVisualsPrefab);
		m_BlinkVisuals = m_BlinkArcGO.GetComponentInChildren<BlinkVisuals>();
		m_BlinkVisuals.ValidTargetFound += ValidTargetFound;
		m_BlinkArcGO.transform.parent = rayOrigin;
        m_BlinkArcGO.transform.localPosition = Vector3.zero;
		m_BlinkArcGO.transform.localRotation = Quaternion.identity;
        
        m_FadeImageGO = U.Object.InstantiateAndSetActive(m_FadeImagePrefab);
        m_FadeImageGO.transform.SetParent(m_ViewerPivot.transform);
        m_FadeImageGO.transform.position = Vector3.zero + m_ViewerPivot.forward * 0.1f;
        m_FadeImage = m_FadeImageGO.GetComponentInChildren<Image>();
	    m_FadeImage.color = m_FadeOutColor; // set initial color to fully transparent black
		m_FadeImage.gameObject.SetActive(false);
		m_PointerTips = transform.GetComponentsInChildren<Transform>().Where( x => x.gameObject.name == "Tip").ToArray();
		m_OriginalTipScale = m_PointerTips[0].localScale;

        m_InitialDefaultLineRendererWidth = m_CustomPointerRayPrefab.WidthStart;
    }

	private void OnDisable()
	{
		if (m_BlinkVisuals)
			m_BlinkVisuals.ValidTargetFound -= ValidTargetFound;

		m_State = State.Inactive;
	}
    
    void OnDestroy()
    {
        if (m_FadeImageGO != null)
            GameObject.DestroyImmediate(m_FadeImageGO);

        // Re-enable the default VRLineRenderers from the proxy
        if (defaultProxyLineRenderers != null && defaultProxyLineRenderers.Count > 0)
            foreach (var vrLineRenderer in defaultProxyLineRenderers)
                vrLineRenderer.SetWidth(m_InitialDefaultLineRendererWidth, m_InitialDefaultLineRendererWidth);
    }

    public void ShowDefaultProxyRays()
    {
        if (defaultProxyLineRenderers != null && defaultProxyLineRenderers.Count > 0)
            foreach (var vrLineRenderer in defaultProxyLineRenderers)
                vrLineRenderer.gameObject.SetActive(true);
    }

    public void HideDefaultProxyRays()
    {
        Debug.LogWarning("<color=yellow>FINISH IMPLEMENTING HIDING OF DEFAULT PROXY RAYS, and ENABLING(for anim) THE TOOL's Custom proxy rays!</color>");
        //if (defaultProxyLineRenderers != null && defaultProxyLineRenderers.Count > 0)
        //    foreach (var vrLineRenderer in defaultProxyLineRenderers)
        //        vrLineRenderer.gameObject.SetActive(false);
    }

    void Update()
    {
        if (m_State == State.Moving)
            return;

        if (m_EnableFadeMode)
            return; // TODO: refactor fade mode after locomotion is in and working

        if (m_BlinkLocomotionInput.blink.wasJustPressed)
        {
            HideCustomRay();
            m_BlinkVisuals.ShowVisuals();
		}
        else if (m_BlinkLocomotionInput.blink.isHeld)
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            float rayDistance;
            if (m_DefaultGroundPlane.Raycast(ray, out rayDistance))
            {
                Vector3 offset = m_ViewerPivot.position;
                offset.y = m_StartingElevation - m_ViewerPivot.position.y;
                m_TrackingCenter.position = m_BlinkTargetHit.point;
                if (m_DoSmoothing)
                    m_TrackingCenter.position = Vector3.Lerp(m_TrackingCenter.position, m_BlinkTargetHit.point, m_IndicatorSmoothing);
                else
                    m_TrackingCenter.position = m_BlinkTargetHit.point;
            }
        }
        else if (m_BlinkLocomotionInput.blink.wasJustReleased && m_State != State.Moving)
        {
			m_BlinkVisuals.HideVisuals();

            if (m_State == State.TransitioningIn)
                ShowCustomRay();
        }
    }
	
	/// <summary>
	/// Method called when a valid target location is found
	/// </summary>
	/// <param name="targetPosition">Target location at which to position the player</param>
	private void ValidTargetFound(Vector3 targetPosition)
	{
	    if (m_State != State.Moving)
	    {
	        switch (m_BlinkMode)
	        {
	            case BlinkMode.Move:
	                StartCoroutine(MoveTowardTarget(targetPosition));
	                break;
	            case BlinkMode.Fade:
	                StartCoroutine(FadeAndTeleport());
	                break;
	        }
	    }
	}

    private IEnumerator MoveTowardTarget(Vector3 targetPosition)
    {
        m_State = State.Moving;
        m_BlinkVisuals.HideVisuals(false);
        ShowCustomRay();

        targetPosition = new Vector3(targetPosition.x, m_ViewerPivot.position.y, targetPosition.z);
        while ((m_ViewerPivot.position - targetPosition).magnitude > 0.1f)
        {
            m_ViewerPivot.position = Vector3.Lerp(m_ViewerPivot.position, targetPosition, Time.unscaledDeltaTime * m_MovementSpeed);
            yield return null;
        }
        
        //yield return new WaitForSeconds(0.5f);

        m_State = State.Inactive;
    }
	
	private IEnumerator FadeAndTeleport()
    {
	    bool validTarget = m_BlinkVisuals.validTarget;
	    Color fadeInColor = validTarget ? m_FadeInColor : m_InvalidTargetColor;
	    float easeDivider = validTarget ? 3f : 2f;

		Debug.LogWarning("Fading Blink overlay!");
        m_FadeImage.gameObject.SetActive(true);
	    
		float fadeAmount = 0f;
	    while (fadeAmount < 1)
	    {
		    fadeAmount = U.Math.Ease(fadeAmount, 1f, easeDivider, 0.05f);
		    m_FadeImage.color = Color.Lerp(m_FadeOutColor, fadeInColor, fadeAmount);
		    yield return null;
	    }

	    fadeAmount = 1f;
		m_FadeImage.color = Color.Lerp(m_FadeOutColor, fadeInColor, fadeAmount);
		
	    if (validTarget)
	    {
			Vector3 pos = m_TrackingCenter.position + m_ViewerPivot.position;
		    pos = m_BlinkVisuals.locatorPosition;
		    pos.y = 1.7f; // m_StartingElevation;  TODO: set back to starting elevation later
            m_ViewerPivot.position = pos;
	    }

	    easeDivider *= 2;

        ShowCustomRay();

        while (fadeAmount > 0)
		{
			fadeAmount = U.Math.Ease(fadeAmount, 0f, easeDivider, 0.05f);
			m_FadeImage.color = Color.Lerp(m_FadeOutColor, fadeInColor, fadeAmount);
			yield return null;
		}

        m_FadeImage.gameObject.SetActive(false);
    }

    public void ShowCustomRay()
    {
        StartCoroutine(ShowCustomPointerRays());
    }

    public void HideCustomRay()
    {
        StartCoroutine(HideCustomPointerRays());
    }

	private IEnumerator HideCustomPointerRays()
	{
        if (m_State != State.Moving)
		    m_State = State.TransitioningIn;

		foreach (var tip in m_PointerTips)
			tip.localScale = Vector3.zero;

        // cache current width for smooth animation to target value without snapping
        float currentWidth = defaultProxyLineRenderers[0].WidthStart;
		while ((m_State == State.TransitioningIn || m_State == State.Moving) && currentWidth > 0)
		{
			foreach (var pointerRayRenderer in defaultProxyLineRenderers)
			{
				currentWidth = U.Math.Ease(currentWidth, 0f, 3, 0.0005f);
				pointerRayRenderer.SetWidth(currentWidth, currentWidth);
			}
			yield return null;
		}

        if (m_State == State.TransitioningIn || m_State == State.Moving)
            foreach (var pointerRayRenderer in defaultProxyLineRenderers)
                pointerRayRenderer.SetWidth(0, 0);
    }

	private IEnumerator ShowCustomPointerRays()
	{
        if (m_State != State.Moving)
            m_State = State.TransitioningOut;

        float currentWidth = defaultProxyLineRenderers[0].WidthStart;
		
		foreach (var tip in m_PointerTips)
			tip.localScale = m_OriginalTipScale;

		while ((m_State == State.TransitioningOut || m_State == State.Moving) && currentWidth < m_InitialDefaultLineRendererWidth)
		{
			currentWidth = U.Math.Ease(currentWidth, m_InitialDefaultLineRendererWidth, 5, 0.0005f);
			foreach (var pointerRayRenderer in defaultProxyLineRenderers)
				pointerRayRenderer.SetWidth(currentWidth, currentWidth);

			yield return null;
		}

        // only set the value if another transition hasn't begun
	    if (m_State == State.TransitioningOut)
	    {
	        m_State = State.Inactive;
            foreach (var pointerRayRenderer in defaultProxyLineRenderers)
                pointerRayRenderer.SetWidth(m_InitialDefaultLineRendererWidth, m_InitialDefaultLineRendererWidth);
        }
        else if (m_State == State.Inactive)
            foreach (var pointerRayRenderer in defaultProxyLineRenderers)
                pointerRayRenderer.SetWidth(m_InitialDefaultLineRendererWidth, m_InitialDefaultLineRendererWidth);
    }
}