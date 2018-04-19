#if UNITY_EDITOR
using System.Collections;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

public class SpatialUIGhostVisuals : MonoBehaviour, ISpatialProxyRay
{
    public enum SpatialInteractionType
    {
        ray,
        bci,
        touch,
        vive
    }

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

            m_GhostInputDeviceContainer.localPosition = m_GhostInputDeviceOriginalLocalPosition;

            var rayVisible = false;
            var bciVisible = false;
            var touchVisible = false;
            var viveVisible = false;
            switch (m_SpatialInteractionType)
            {
                case SpatialInteractionType.ray:
                    rayVisible = true;
                    break;
                case SpatialInteractionType.bci:
                    bciVisible = true;
                    break;
                case SpatialInteractionType.touch:
                    touchVisible = true;
                    break;
                case SpatialInteractionType.vive:
                    viveVisible = true;
                    break;
            }

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

    public Transform spatialProxyRayDriverTransform { get; set; }

    void Start()
    {
        m_GhostInputDeviceOriginalLocalPosition = m_GhostInputDeviceContainer.localPosition;
        spatialProxyRayDriverTransform = m_GhostInputDeviceContainer;

        spatialProxyRayOrigin = ObjectUtils.Instantiate(m_SpatialProxyRayPrefab.gameObject, m_RayContainer.transform).transform;
        spatialProxyRayOrigin.position = Vector3.zero;
        spatialProxyRayOrigin.rotation = Quaternion.identity;
        spatialProxyRay = spatialProxyRayOrigin.GetComponent<DefaultProxyRay>();
        spatialProxyRay.SetColor(Color.white);
    }

    private void OnDestroy()
    {
        ObjectUtils.Destroy(spatialProxyRayOrigin.gameObject);
    }

    public void SetPositionOffset(Vector3 newLocalPositionOffset)
    {
        var newGhostInputDevicePosition = m_GhostInputDeviceOriginalLocalPosition + newLocalPositionOffset;
        this.RestartCoroutine(ref m_GhostInputDeviceRepositionCoroutine, AnimateGhostInputDevicePosition(newLocalPositionOffset));
    }

    public void UpdateSpatialRay(IPerformSpatialRayInteraction caller)
    {
        this.UpdateSpatialProxyRayLength();
        spatialProxyRayOrigin.SetParent(caller.spatialProxyRayDriverTransform);
        spatialProxyRayOrigin.localPosition = Vector3.zero;
        spatialProxyRayOrigin.localRotation = caller.spatialProxyRayDriverTransform.localRotation;
    }

    public void UpdateRotation(Quaternion rotation)
    {
        m_GhostInputDeviceContainer.localRotation = rotation;
    }

    IEnumerator AnimateGhostInputDevicePosition(Vector3 targetLocalPosition)
    {
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
        m_GhostInputDeviceRepositionCoroutine = null;
    }

}
#endif
