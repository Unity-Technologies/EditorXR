using System;
using UnityEngine;
using UnityEditor.VR;
using UnityEngine.VR.Tools;

public class CreatePrimitiveTool : MonoBehaviour, ITool, IStandardActionMap, IRay, IInstantiateUI
{
    public Standard StandardInput
    {
        get; set;
    }

    public Transform RayOrigin
    {
        get; set;
    }

    public Func<GameObject, GameObject> InstantiateUI
    {
        private get; set;
    }

    [SerializeField]
    private Canvas CanvasPrefab;
    private Canvas m_ToolCanvas;

    void Update()
    {
        if (StandardInput.action.wasJustPressed)
        {
                if (m_ToolCanvas == null)
                {
                    var go = InstantiateUI(CanvasPrefab.gameObject);
                    m_ToolCanvas = go.GetComponent<Canvas>();
                }
                m_ToolCanvas.transform.position = RayOrigin.position + RayOrigin.forward*5f;
                m_ToolCanvas.transform.rotation = Quaternion.LookRotation(m_ToolCanvas.transform.position - EditorVRView.viewerCamera.transform.position);            
        }
    }
}
