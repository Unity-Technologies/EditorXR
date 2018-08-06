using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Ring : MonoBehaviour {

    public Transform ringTr;
    public TextMeshProUGUI text;
    public TextMeshProUGUI coords;
    public float fadeAwaySpeed = 1f;
    public float fadeInSpeed = 5f;
    public LineRenderer lineA, lineB;

    Material m_ringMat;
    float m_initAltitude;
    float m_intensity = 0f;
    float m_intensity_core = 0f;
    float m_intensity_core_up = 0f;
    float m_intensity_core_down = 0f;
    int m_intensityHash = Shader.PropertyToID("_Intensity");
    Gradient m_origGradient;
    Gradient m_currentGradient;
    float m_lineLength;
    float m_lineOffset;

    // Use this for initialization
    void Start () {
        m_initAltitude = transform.position.y;
        m_ringMat = ringTr.GetComponent<MeshRenderer>().material;
        m_origGradient = new Gradient();
        m_origGradient.SetKeys(lineA.colorGradient.colorKeys, lineA.colorGradient.alphaKeys);
        m_currentGradient = new Gradient();
        m_currentGradient.SetKeys(lineA.colorGradient.colorKeys, lineA.colorGradient.alphaKeys);

        m_lineLength = lineA.GetPosition(1).y;
        m_lineOffset = lineA.GetPosition(0).y;

    }

    // Update is called once per frame
    void Update () {
        //Decrease intensity over time
        m_intensity = Mathf.Max(0f, m_intensity - fadeAwaySpeed * Time.deltaTime);
        m_intensity_core = Mathf.Max(0f, m_intensity_core - fadeAwaySpeed * Time.deltaTime);
        m_intensity_core_up = Mathf.Max(0f, m_intensity_core_up - fadeAwaySpeed/2f * Time.deltaTime);
        m_intensity_core_down = Mathf.Max(0f, m_intensity_core_down - fadeAwaySpeed/2f * Time.deltaTime);


        //Update text
        float kAltitude = transform.position.y - m_initAltitude;
        text.text = kAltitude.ToString("0.00") + " m";
        Vector3 pos = transform.position;
        pos = new Vector3(pos.x, pos.y - m_initAltitude, pos.x);
        coords.text = "x:" + pos.x.ToString("0.00") + " y:" + pos.y.ToString("0.00") + " z:" + pos.z.ToString("0.00");

        //Update material
        m_ringMat.SetFloat(m_intensityHash, m_intensity);

        //Update lines color
        GradientAlphaKey[] aKeys = new GradientAlphaKey[m_origGradient.alphaKeys.Length];

        for (int i = 0; i < m_origGradient.alphaKeys.Length; i++)
        {
            aKeys[i] = new GradientAlphaKey(m_origGradient.alphaKeys[i].alpha * m_intensity_core, m_currentGradient.alphaKeys[i].time);
        }

        m_currentGradient.SetKeys(m_origGradient.colorKeys, aKeys);

        lineA.colorGradient = m_currentGradient;
        lineB.colorGradient = m_currentGradient;

        //Update lines points
        lineA.SetPosition(1, new Vector3(0f, Mathf.Max(m_lineOffset, m_lineLength * m_intensity_core * m_intensity_core_up, 0f)));
        lineB.SetPosition(1, new Vector3(0f, - Mathf.Max(m_lineOffset, m_lineLength * m_intensity_core * m_intensity_core_down, 0f)));


        //Update text
        text.color = new Color(1f, 1f, 1f, m_intensity_core);
        coords.color = new Color(1f, 1f, 1f, Mathf.Max(m_intensity_core, m_intensity));

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
            m_intensity = Mathf.Min(1f, m_intensity + fadeInSpeed * Time.deltaTime);
        }
    }

    public void SetEffectCore()
    {
            m_intensity_core = Mathf.Min(1f, m_intensity_core + fadeInSpeed * Time.deltaTime);
    }

    public void SetEffectCoreUp()
    {
        m_intensity_core_up = Mathf.Min(1f, m_intensity_core_up + fadeInSpeed * Time.deltaTime);
    }

    public void SetEffectCoreDown()
    {
        m_intensity_core_down = Mathf.Min(1f, m_intensity_core_down + fadeInSpeed * Time.deltaTime);
    }
}
