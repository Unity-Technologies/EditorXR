using UnityEngine;
using TMPro;
using UnityEditor.Experimental.EditorVR.Utilities;

public class Ring : MonoBehaviour
{
    [SerializeField]
    Transform ringTr;

    [SerializeField]
    TextMeshProUGUI text;

    [SerializeField]
    TextMeshProUGUI coords;

    [SerializeField]
    float fadeAwaySpeed = 1f;

    [SerializeField]
    float fadeInSpeed = 5f;

    [SerializeField]
    LineRenderer lineA, lineB;

    Material m_RingMat;
    float m_InitAltitude;
    float m_Intensity;
    float m_IntensityCore;
    float m_IntensityCoreUp;
    float m_IntensityCoreDown;
    int m_IntensityHash = Shader.PropertyToID("_Intensity");
    Gradient m_OrigGradient;
    Gradient m_CurrentGradient;
    float m_LineLength;
    float m_LineOffset;

    void Start()
    {
        m_InitAltitude = transform.position.y;

        m_RingMat = MaterialUtils.GetMaterialClone(ringTr.GetComponent<MeshRenderer>());
        m_OrigGradient = new Gradient();
        m_OrigGradient.SetKeys(lineA.colorGradient.colorKeys, lineA.colorGradient.alphaKeys);
        m_CurrentGradient = new Gradient();
        m_CurrentGradient.SetKeys(lineA.colorGradient.colorKeys, lineA.colorGradient.alphaKeys);

        m_LineLength = lineA.GetPosition(1).y;
        m_LineOffset = lineA.GetPosition(0).y;
    }

    void Update ()
    {
        m_Intensity = Mathf.Max(0f, m_Intensity - fadeAwaySpeed * Time.deltaTime);
        m_IntensityCore = Mathf.Max(0f, m_IntensityCore - fadeAwaySpeed * Time.deltaTime);
        m_IntensityCoreUp = Mathf.Max(0f, m_IntensityCoreUp - fadeAwaySpeed/2f * Time.deltaTime);
        m_IntensityCoreDown = Mathf.Max(0f, m_IntensityCoreDown - fadeAwaySpeed/2f * Time.deltaTime);

        float kAltitude = transform.position.y - m_InitAltitude;
        text.text = kAltitude.ToString("0.00") + " m";
        Vector3 pos = transform.position;
        pos = new Vector3(pos.x, pos.y - m_InitAltitude, pos.x);
        coords.text = "x:" + pos.x.ToString("0.00") + " y:" + pos.y.ToString("0.00") + " z:" + pos.z.ToString("0.00");

  
        m_RingMat.SetFloat(m_IntensityHash, m_Intensity);

        GradientAlphaKey[] aKeys = new GradientAlphaKey[m_OrigGradient.alphaKeys.Length];

        for (int i = 0; i < m_OrigGradient.alphaKeys.Length; i++)
        {
            aKeys[i] = new GradientAlphaKey(m_OrigGradient.alphaKeys[i].alpha * m_IntensityCore, m_CurrentGradient.alphaKeys[i].time);
        }

        m_CurrentGradient.SetKeys(m_OrigGradient.colorKeys, aKeys);

        lineA.colorGradient = m_CurrentGradient;
        lineB.colorGradient = m_CurrentGradient;

        lineA.SetPosition(1, new Vector3(0f, Mathf.Max(m_LineOffset, m_LineLength * m_IntensityCore * m_IntensityCoreUp, 0f)));
        lineB.SetPosition(1, new Vector3(0f, - Mathf.Max(m_LineOffset, m_LineLength * m_IntensityCore * m_IntensityCoreDown, 0f)));


        text.color = new Color(1f, 1f, 1f, m_IntensityCore);
        coords.color = new Color(1f, 1f, 1f, Mathf.Max(m_IntensityCore, m_Intensity));

        if (Input.GetMouseButtonDown(2))
        {
            coords.enabled = !coords.enabled;
        }
    }

    public void SetEffectWorldDirection(Vector3 movdir)
    {
        if (movdir.sqrMagnitude > 0f)
        {
            ringTr.rotation = Quaternion.LookRotation(movdir);
            m_Intensity = Mathf.Min(1f, m_Intensity + fadeInSpeed * Time.deltaTime);
        }
    }

    public void SetEffectCore()
    {
            m_IntensityCore = Mathf.Min(1f, m_IntensityCore + fadeInSpeed * Time.deltaTime);
    }

    public void SetEffectCoreUp()
    {
        m_IntensityCoreUp = Mathf.Min(1f, m_IntensityCoreUp + fadeInSpeed * Time.deltaTime);
    }

    public void SetEffectCoreDown()
    {
        m_IntensityCoreDown = Mathf.Min(1f, m_IntensityCoreDown + fadeInSpeed * Time.deltaTime);
    }

    void OnDestroy()
    {
        ObjectUtils.Destroy(m_RingMat);
    }
}
