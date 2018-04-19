#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR;
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
    GameObject m_RayContainer;

    [SerializeField]
    GameObject m_BCIContainer;

    [SerializeField]
    GameObject m_TouchContainer;

    [SerializeField]
    GameObject m_ViveContainer;

    SpatialInteractionType m_SpatialInteractionType;

    public SpatialInteractionType spatialInteractionType
    {
        set
        {
            if (m_SpatialInteractionType == value)
                return;

            m_SpatialInteractionType = value;

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

    public void UpdateSpatialRay(IPerformSpatialRayInteraction caller)
    {
        this.UpdateSpatialProxyRayLength();
        spatialProxyRayOrigin.SetParent(caller.spatialProxyRayDriverTransform);
        spatialProxyRayOrigin.localPosition = Vector3.zero;
        spatialProxyRayOrigin.localRotation = caller.spatialProxyRayDriverTransform.localRotation;
    }

    public void UpdateRotation(Quaternion rotation)
    {

    }
}
#endif
