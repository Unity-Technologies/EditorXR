#if UNITY_EDITOR
using System.Collections;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

public class SpatialMenuGhostVisuals : MonoBehaviour, ISpatialProxyRay, IUsesViewerScale
{
    public enum SpatialInteractionType
    {
        ray,
        bci,
        touch,
        vive
    }

    const float k_SpatialRayLength = 1f;

    [SerializeField]
    DefaultProxyRay m_SpatialProxyRayPrefab;

    [SerializeField]
    Transform m_GhostInputDeviceContainer;

    [SerializeField]
    GameObject m_RayContainer;

    [SerializeField]
    GameObject m_BCIContainer;

    [SerializeField]
    GameObject m_TouchContainer;

    [SerializeField]
    GameObject m_ViveContainer;

    [Header("Accompanying Visual Elements")]
    [SerializeField]
    Transform m_SpatialSecondaryVisuals;

    [SerializeField]
    Transform m_RaybasedSecondaryVisuals;

    SpatialInteractionType m_SpatialInteractionType;
    Vector3 m_GhostInputDeviceOriginalLocalPosition;
    Coroutine m_GhostInputDeviceRepositionCoroutine;

    public SpatialInteractionType spatialInteractionType
    {
        set
        {
            if (m_SpatialInteractionType == value)
                return;

            m_SpatialInteractionType = value;

            //m_GhostInputDeviceContainer.localPosition = m_GhostInputDeviceOriginalLocalPosition;

            var rayVisible = false;
            var bciVisible = false;
            var touchVisible = false;
            var viveVisible = false;
            switch (m_SpatialInteractionType)
            {
                case SpatialInteractionType.ray:
                    rayVisible = true;
                    SetPositionOffset(Vector3.zero);
                    break;
                case SpatialInteractionType.bci:
                    UpdateRotation(Quaternion.identity);
                    bciVisible = true;
                    break;
                case SpatialInteractionType.touch:
                    UpdateRotation(Quaternion.identity);
                    SetPositionOffset(Vector3.zero);
                    touchVisible = true;
                    break;
                case SpatialInteractionType.vive:
                    UpdateRotation(Quaternion.identity);
                    viveVisible = true;
                    break;
            }

            m_SpatialSecondaryVisuals.gameObject.SetActive(false); // disable for now, until touch again supports rotation(touchVisible);
            m_RaybasedSecondaryVisuals.gameObject.SetActive(rayVisible);

            m_RayContainer.SetActive(rayVisible);
            m_BCIContainer.SetActive(bciVisible);
            m_TouchContainer.SetActive(touchVisible);
            m_ViveContainer.SetActive(viveVisible);
        }
    }

    /// <summary>
    /// Ray origin used for ray-based interaction with Spatial UI elements
    /// </summary>
    public Transform spatialProxyRayOrigin { get; set; }

    public DefaultProxyRay spatialProxyRay { get; set; }

    //public Transform spatialProxyRayDriverTransform { get; set; }

    public bool transitioningModes { get; private set; }

    void Awake()
    {
        m_GhostInputDeviceOriginalLocalPosition = m_GhostInputDeviceContainer.localPosition;
        //spatialProxyRayDriverTransform = m_GhostInputDeviceContainer;

        //spatialProxyRayOrigin = ObjectUtils.Instantiate(m_SpatialProxyRayPrefab.gameObject, m_RayContainer.transform).transform;
        spatialProxyRayOrigin = this.InitializeSpatialProxyRay(m_RayContainer.transform, m_SpatialProxyRayPrefab.gameObject);
        spatialProxyRayOrigin.localPosition = Vector3.zero;
        spatialProxyRayOrigin.localRotation = Quaternion.identity;
        spatialProxyRayOrigin.localScale = Vector3.one;
        spatialProxyRay = spatialProxyRayOrigin.GetComponent<DefaultProxyRay>();

        //var tester = spatialProxyRayOrigin.GetComponentInChildren<IntersectionTester>();
        //tester.active = false;

        m_SpatialSecondaryVisuals.gameObject.SetActive(false);
        m_RaybasedSecondaryVisuals.gameObject.SetActive(false);
        //spatialProxyRay.SetColor(Color.white);
    }

    private void OnDestroy()
    {
        ObjectUtils.Destroy(spatialProxyRayOrigin.gameObject);
    }

    void Update()
    {
        return;
        spatialProxyRay.SetLength(k_SpatialRayLength * this.GetViewerScale());
        spatialProxyRay.SetColor(Random.ColorHSV());
    }

    public void SetPositionOffset(Vector3 newLocalPositionOffset)
    {
        var newGhostInputDevicePosition = m_GhostInputDeviceOriginalLocalPosition - newLocalPositionOffset;
        if (m_SpatialInteractionType == SpatialInteractionType.ray)
            newGhostInputDevicePosition = m_GhostInputDeviceOriginalLocalPosition - Vector3.forward * 0.325f;

        this.RestartCoroutine(ref m_GhostInputDeviceRepositionCoroutine, AnimateGhostInputDevicePosition(newGhostInputDevicePosition));
    }

    /*
    public void UpdateSpatialRay(IPerformSpatialRayInteraction caller)
    {
        this.UpdateSpatialProxyRayLength();
        spatialProxyRayOrigin.SetParent(caller.spatialProxyRayDriverTransform);
        spatialProxyRayOrigin.localPosition = Vector3.zero;
        spatialProxyRayOrigin.localRotation = caller.spatialProxyRayDriverTransform.localRotation;
    }
    */

    public void UpdateRotation(Quaternion rotation)
    {
        m_GhostInputDeviceContainer.localRotation = rotation;
    }

    IEnumerator AnimateGhostInputDevicePosition(Vector3 targetLocalPosition)
    {
        transitioningModes = true;
        var currentPosition = m_GhostInputDeviceContainer.localPosition;
        var transitionAmount = 0f;
        var transitionSubtractMultiplier = 5f;
        while (transitionAmount < 1f)
        {
            var smoothTransition = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
            m_GhostInputDeviceContainer.localPosition = Vector3.Lerp(currentPosition, targetLocalPosition, smoothTransition);
            transitionAmount += Time.deltaTime * transitionSubtractMultiplier;
            yield return null;
        }

        m_GhostInputDeviceContainer.localPosition = targetLocalPosition;

        var waitBeforeTransitionEnd = 1f;
        while (waitBeforeTransitionEnd > 0f)
        {
            waitBeforeTransitionEnd -= Time.unscaledDeltaTime;
            yield return null;
        }

        transitioningModes = false;
        m_GhostInputDeviceRepositionCoroutine = null;
    }

}
#endif
