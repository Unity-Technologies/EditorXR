using System;
using System.Collections.Generic;
using UnityEngine.VR.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputNew;
using UnityEngine.UI;
using UnityEngine.VR.Proxies;
using UnityEngine.VR.Tools;

[ExecuteInEditMode]
public class MainMenuDev : MonoBehaviour, IRay, IInstantiateUI, IMainMenu
{

    [SerializeField]
    private Canvas m_MainMenuPrefab;
    
    private GameObject m_ButtonTemplate;
    private RectTransform m_Layout;
    private Canvas m_MenuCanvas;

    public Transform rayOrigin { get; set; }
    public List<Type> menuTools { private get; set; }
    public Func<int, Type, bool> selectTool { private get; set; }
    public Transform menuOrigin { get; set; }
    public Transform menuInputOrigin { get; set; }
    public MainMenuInput mainMenuInput  { get; set; }
    public int tagIndex { get; set; }
    public Camera eventCamera { get; set; }
    public Func<GameObject, GameObject> instantiateUI { private get; set; }

    void Start()
    {
        if (m_MenuCanvas == null)
        {
            var go = instantiateUI(m_MainMenuPrefab.gameObject);
            m_MenuCanvas = go.GetComponent<Canvas>();
            m_Layout = m_MenuCanvas.GetComponentInChildren<GridLayoutGroup>().GetComponent<RectTransform>();
            m_ButtonTemplate = m_Layout.GetChild(0).gameObject;
            m_ButtonTemplate.SetActive(false);
        }
        m_MenuCanvas.transform.SetParent(rayOrigin, false);
        CreateToolButtons();
    }

    void OnDestroy()
    {
        if (m_MenuCanvas != null)
            U.Object.Destroy(m_MenuCanvas.gameObject);
    }

    private void CreateToolButtons()
    {
        foreach (var menuTool in menuTools)
        {
            var newButton = U.Object.InstantiateAndSetActive(m_ButtonTemplate, m_Layout, false);
            newButton.name = menuTool.Name;
            var text = newButton.GetComponentInChildren<Text>();
            text.text = menuTool.Name;
            var button = newButton.GetComponent<Button>();
            AddButtonListener(button, menuTool);
        }
    }

    private void AddButtonListener(Button b, Type t)
    {
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(() =>
        {
            if (selectTool(tagIndex, t))
                U.Object.Destroy(this);
        });
        b.onClick.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
    }
    public void Disable()
    {
        throw new NotImplementedException();
    }

    public void Enable()
    {
        throw new NotImplementedException();
    }
}
