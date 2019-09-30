using TMPro;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

public class Ring : MonoBehaviour
{
#pragma warning disable 649
    [SerializeField]
    Transform m_RingTransform;

    [SerializeField]
    TextMeshPro m_EleveationText;

    [SerializeField]
    TextMeshPro m_CoordinatesText;

    [SerializeField]
    Renderer m_CoordinatesRenderer;

    [SerializeField]
    Renderer m_RingRenderer;

    [SerializeField]
    Renderer m_ElevationTextRenderer;

    [SerializeField]
    float m_FadeAwaySpeed = 1f;

    [SerializeField]
    float m_FadeInSpeed = 5f;

    [SerializeField]
    float m_CoreFadeAwaySpeed = 1f;

    [SerializeField]
    float m_CoreFadeInSpeed = 5f;

    [SerializeField]
    LineRenderer m_LineA;

    [SerializeField]
    LineRenderer m_LineB;
#pragma warning restore 649

    readonly Gradient m_OrigGradient = new Gradient();
    readonly Gradient m_CurrentGradient = new Gradient();
    readonly int m_IntensityHash = Shader.PropertyToID("_Intensity");
    GradientAlphaKey[] m_AlphaKeys;

    Material m_RingMat;
    float m_InitAltitude;
    float m_Intensity;
    float m_IntensityCore;
    float m_IntensityCoreUp;
    float m_IntensityCoreDown;
    float m_LineLength;
    float m_LineOffset;

    bool m_MouseWasHeld;

    public bool coreVisible { get { return m_IntensityCore > 0; } }

    void Start()
    {
        m_InitAltitude = transform.position.y;

        m_RingMat = MaterialUtils.GetMaterialClone(m_RingTransform.GetComponent<MeshRenderer>());
        m_OrigGradient.SetKeys(m_LineA.colorGradient.colorKeys, m_LineA.colorGradient.alphaKeys);
        m_CurrentGradient.SetKeys(m_LineA.colorGradient.colorKeys, m_LineA.colorGradient.alphaKeys);

        m_LineLength = m_LineA.GetPosition(1).y;
        m_LineOffset = m_LineA.GetPosition(0).y;

        m_AlphaKeys = new GradientAlphaKey[m_OrigGradient.alphaKeys.Length];
    }

    void Update()
    {
        var deltaTime = Time.deltaTime;
        m_Intensity = Mathf.Max(0f, m_Intensity - m_FadeAwaySpeed * deltaTime);
        m_IntensityCore = Mathf.Max(0f, m_IntensityCore - m_CoreFadeAwaySpeed * deltaTime);
        m_IntensityCoreUp = Mathf.Max(0f, m_IntensityCoreUp - m_CoreFadeAwaySpeed * deltaTime);
        m_IntensityCoreDown = Mathf.Max(0f, m_IntensityCoreDown - m_CoreFadeAwaySpeed * deltaTime);

        var altitude = transform.position.y - m_InitAltitude;
        m_EleveationText.text = string.Format("{0:F2} m", altitude);
        var position = transform.position;
        position = new Vector3(position.x, position.y - m_InitAltitude, position.x);
        m_CoordinatesText.text = string.Format("x:{0:F2} y:{1:F2} z:{2:F3}", position.x, position.y, position.z);

        m_RingMat.SetFloat(m_IntensityHash, m_Intensity);

        var alphaKeys = m_OrigGradient.alphaKeys;
        var currentAlphaKeys = m_CurrentGradient.alphaKeys;
        var alphaKeysLength = alphaKeys.Length; 
        for (var i = 0; i < alphaKeysLength; i++)
        {
            m_AlphaKeys[i] = new GradientAlphaKey(alphaKeys[i].alpha * m_IntensityCore, currentAlphaKeys[i].time);
        }

        m_CurrentGradient.SetKeys(m_OrigGradient.colorKeys, m_AlphaKeys);

        m_LineA.colorGradient = m_CurrentGradient;
        m_LineB.colorGradient = m_CurrentGradient;

        var lineLength = m_LineLength * m_IntensityCore;
        m_LineA.SetPosition(1, new Vector3(0f, Mathf.Max(m_LineOffset, lineLength * m_IntensityCoreUp, 0f)));
        m_LineB.SetPosition(1, new Vector3(0f, -Mathf.Max(m_LineOffset, lineLength * m_IntensityCoreDown, 0f)));

        m_EleveationText.color = new Color(1f, 1f, 1f, m_IntensityCore);
        m_CoordinatesText.color = new Color(1f, 1f, 1f, Mathf.Max(m_IntensityCore, m_Intensity));

#if UNITY_2018_4_OR_NEWER
        if (VRView.MiddleMouseButtonHeld && !m_MouseWasHeld)
            m_CoordinatesText.enabled = !m_CoordinatesText.enabled;
#endif

        m_MouseWasHeld = VRView.MiddleMouseButtonHeld;

        var ringEnabled = !Mathf.Approximately(m_Intensity, 0f);
        m_RingRenderer.enabled = ringEnabled;
        m_CoordinatesRenderer.enabled = ringEnabled;

        var coreEnabled = !Mathf.Approximately(m_IntensityCore, 0f);
        m_LineA.enabled = coreEnabled;
        m_LineB.enabled = coreEnabled;
        m_ElevationTextRenderer.enabled = coreEnabled;
    }

    public void SetEffectWorldDirection(Vector3 movdir)
    {
        if (movdir.sqrMagnitude > 0f)
        {
            m_RingTransform.rotation = Quaternion.LookRotation(movdir);
            m_Intensity = Mathf.Min(1f, m_Intensity + m_FadeInSpeed * Time.deltaTime);
        }
    }

    public void SetEffectCore()
    {
        m_IntensityCore = Mathf.Min(1f, m_IntensityCore + m_CoreFadeInSpeed * Time.deltaTime);
    }

    public void SetEffectCoreUp()
    {
        m_IntensityCoreUp = Mathf.Min(1f, m_IntensityCoreUp + m_CoreFadeInSpeed * Time.deltaTime);
    }

    public void SetEffectCoreDown()
    {
        m_IntensityCoreDown = Mathf.Min(1f, m_IntensityCoreDown + m_CoreFadeInSpeed * Time.deltaTime);
    }

    void OnDestroy()
    {
        ObjectUtils.Destroy(m_RingMat);
    }
}
