using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.UI;
using UnityEngine.VR.Tools;

public class BlinkLocomotionTool : MonoBehaviour, ITool, ILocomotion, IRay, IStandardActionMap
{
    public Transform RayOrigin { get; set; }

    // TODO: 
    // TrackedObjectInput should probably be coming from an interface / setter in EditorVR. 
    // My intention is to use TrackedObjectInput to be able to preview where the user's hands will be when the blink is executed. 
    // Normally we'd use a ray transform to get the hand position, but in this case it's a one-handed tool (so only gets the one ray), 
    // but we want to have the option to visualize both hands. 
    public TrackedObject TrackedObjectInput { get; set; }

    public Standard StandardInput { get; set; }

    public Transform ViewerPivot
    {
        set
        {
            // TODO:
            // We need a separate transform reference for body, with its origin at the ground / feet,
            // so we can move the avatar to a spot on the ground (without moving their *head* into the ground!). 
            // For the time being, I'm just setting BodyOrigin to the ViewerPivot.
            m_ViewerPivot = value;
            m_BodyOrigin = value;
        }
    }
    
    private Transform m_ViewerPivot;

    private Plane m_DefaultGroundPlane;

    private Transform m_BodyOrigin;

    [Header("Settings")]
    [Tooltip("Total fade time (fade in / fade out each take half this amount)")]
    [SerializeField]
    private float m_FadeTime = 0.5f;
    [SerializeField]
    private bool m_ShowHands = true;
    [SerializeField]
    private bool m_DoSmoothing = true;
    [Tooltip("Lerp amount. Closer to 0 = smoother, closer to 1 = faster.")]
    [Range(0f, 1f)]
    [SerializeField]
    private float m_IndicatorSmoothing = 0.2f;
    [Header("References")]
    [SerializeField]
    private ActionMap m_BlinkActionMap;
    [SerializeField]
    private GameObject m_BlinkArcRendererPrefab;
    [Tooltip("UI plane that will be instantiated in front of the user's vision and provide the fade color.")]
    [SerializeField]
    private GameObject m_FadeImagePrefab;
    [Header("Prefabs")]
    [SerializeField]
    private GameObject m_AvatarBaseIndicatorPrefab;
    [SerializeField]
    private GameObject m_AvatarHeadIndicatorPrefab;
    [SerializeField]
    private GameObject m_AvatarLeftControllerIndicatorPrefab; // TODO: We probably want this to just be fetched from the proxy / avatar rig.
    [SerializeField]
    private GameObject m_AvatarRightControllerIndicatorPrefab;

    private Transform m_TrackingCenter;
    private Transform m_FootIndicator;
    private Transform m_HeadIndicator;
    private Transform m_LeftControllerIndicator;
    private Transform m_RightControllerIndicator;
    private RaycastHit m_BlinkTargetHit;
    private RaycastHit m_HitBelowHead;
    private bool m_WasOnGround;
    private float m_StartingElevation;
    private Image m_FadeImage;
    private GameObject m_FadeImageGO;
    private GameObject m_BlinkArcGO;
    
    void Start()
    {
        // Creating a default plane to raycast against, so that the user can blink around without having to create a ground first. 
        m_DefaultGroundPlane = new Plane(Vector3.up, Vector3.zero);
        
        m_FadeTime = m_FadeTime / 2f; // Fade in/out are each half of the total.
        m_StartingElevation = m_ViewerPivot.position.y;
        m_TrackingCenter = new GameObject("Tracking Center Indicator").transform;
        m_FootIndicator = Instantiate(m_AvatarBaseIndicatorPrefab).transform;
        m_HeadIndicator = Instantiate(m_AvatarHeadIndicatorPrefab).transform;
        // Parent other indicators to the trackingCenter xform so that we don't have to position (and smooth) each of them;
        // we can just adjust local position for head height etc.
        m_FootIndicator.parent = m_TrackingCenter; 
        m_HeadIndicator.parent = m_TrackingCenter;
        if (m_ShowHands)
        {
            m_LeftControllerIndicator = Instantiate(m_AvatarLeftControllerIndicatorPrefab).transform;
            m_RightControllerIndicator = Instantiate(m_AvatarRightControllerIndicatorPrefab).transform;
            m_LeftControllerIndicator.parent = m_TrackingCenter;
            m_RightControllerIndicator.parent = m_TrackingCenter;
            m_LeftControllerIndicator.localPosition = m_RightControllerIndicator.localPosition = Vector3.zero;
        }
        m_HeadIndicator.localPosition = Vector3.zero;
        HideIndicator();

        m_BlinkArcGO = Instantiate(m_BlinkArcRendererPrefab);
        m_BlinkArcGO.transform.parent = RayOrigin;
        m_BlinkArcGO.transform.localPosition = Vector3.zero;
        m_BlinkArcGO.SetActive(false);

        // Create the UI element that does the fading. 
        // TODO: 
        // This should probably be a static piece of the avatar rig rather than created by this tool.
        // We should also look into alternate methods for doing fades - maybe using the SDK-specific 
        // fades for platforms that support it, and fall back to a method like this for those that don't.
        m_FadeImageGO = Instantiate(m_FadeImagePrefab);
        m_FadeImageGO.transform.position = m_ViewerPivot.position + m_ViewerPivot.forward * 0.1f;
        m_FadeImageGO.transform.parent = m_ViewerPivot;
        m_FadeImage = m_FadeImagePrefab.GetComponent<Image>();
    }
    
    // Clean up objects when the tool is deactivated -- assuming that removing a tool from its stack destroys the component.
    // TODO: Pool these objects, rather than creating and destroying them with the tool.
    void OnDestroy()
    {
        Destroy(m_TrackingCenter.gameObject);
        Destroy(m_BlinkArcGO);
        Destroy(m_FadeImageGO);
    }

    void Update()
    {
        if (StandardInput.action.wasJustPressed)
        {
            m_BlinkArcGO.SetActive(true);
        }
        else if (StandardInput.action.isHeld)
        {
            Ray ray = new Ray(RayOrigin.position, RayOrigin.forward);
            float rayDistance;
            if (m_DefaultGroundPlane.Raycast(ray, out rayDistance))
            {
                Vector3 offset = m_BodyOrigin.position - m_HitBelowHead.point;
                offset.y = m_StartingElevation - (m_ViewerPivot.position.y - m_HitBelowHead.point.y);
                m_TrackingCenter.position = m_BlinkTargetHit.point;
                m_FootIndicator.localPosition = m_HitBelowHead.point;
                m_HeadIndicator.localPosition = m_ViewerPivot.position - m_HitBelowHead.point;
                m_HeadIndicator.rotation = m_ViewerPivot.rotation;
                if (m_ShowHands)
                {
                    m_LeftControllerIndicator.localPosition = TrackedObjectInput.leftPosition.vector3 - m_HitBelowHead.point;
                    m_LeftControllerIndicator.rotation = TrackedObjectInput.leftRotation.quaternion;
                    m_RightControllerIndicator.localPosition = TrackedObjectInput.rightPosition.vector3 - m_HitBelowHead.point;
                    m_RightControllerIndicator.rotation = TrackedObjectInput.rightRotation.quaternion;
                }
                if (m_DoSmoothing)
                {
                    m_TrackingCenter.position = !m_WasOnGround
                        ? m_BlinkTargetHit.point
                        : Vector3.Lerp(m_TrackingCenter.position, m_BlinkTargetHit.point, m_IndicatorSmoothing);
                }
                else
                {
                    m_TrackingCenter.position = m_BlinkTargetHit.point;
                }
                m_WasOnGround = true;

            }
            else
            {
                m_WasOnGround = false;
                HideIndicator();
            }
        }
        else if (StandardInput.action.wasJustReleased)
        {
            m_BlinkArcGO.SetActive(false);
            if (m_WasOnGround)
            {
                StartCoroutine(FadeAndTeleport());
            }
        }
    }
    
    // TODO: Fade should be a common/U method, perhaps with a delegate to execute during the blackout.
    private IEnumerator FadeAndTeleport()
    {
        m_FadeImage.gameObject.SetActive(true);
        m_FadeImage.CrossFadeAlpha(0, 0, false);
        m_FadeImage.CrossFadeAlpha(1, m_FadeTime, false);
        yield return new WaitForSeconds(m_FadeTime);
        Vector3 pos = m_TrackingCenter.position + (m_BodyOrigin.position - m_ViewerPivot.position);
        pos.y = m_StartingElevation;
        m_BodyOrigin.position = pos;
        m_WasOnGround = false;
        HideIndicator();
        m_FadeImage.CrossFadeAlpha(0, m_FadeTime, false);
        yield return new WaitForSeconds(m_FadeTime);
        m_FadeImage.gameObject.SetActive(false);
    }

    private void HideIndicator()
    {
        m_TrackingCenter.position = Vector3.down * 10000;
    }
}
