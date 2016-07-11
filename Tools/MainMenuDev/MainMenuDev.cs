using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.VR.Tools;

[ExecuteInEditMode]
public class MainMenuDev : MonoBehaviour, IRay, IInstantiateUI, IMainMenu
{
	public Transform RayOrigin
	{
		get; set;
	}

	public Func<GameObject, GameObject> InstantiateUI
	{
		private get; set;
	}

	public List<Type> MenuTools { private get; set; }

	public Func<IMainMenu, Type, bool> SelectTool { private get; set; }

	[SerializeField]
	private Canvas m_MainMenuPrefab;
	private Canvas m_MenuCanvas;

	private RectTransform m_Layout;
	private GameObject m_ButtonTemplate;

	void Start()
	{	
		if (m_MenuCanvas == null)
		{
			var go = InstantiateUI(m_MainMenuPrefab.gameObject);
			m_MenuCanvas = go.GetComponent<Canvas>();
			m_Layout = m_MenuCanvas.GetComponentInChildren<GridLayoutGroup>().GetComponent<RectTransform>();
			m_ButtonTemplate = m_Layout.GetChild(0).gameObject;
			m_ButtonTemplate.SetActive(false);
		}
		m_MenuCanvas.transform.SetParent(RayOrigin, false);
		CreateToolButtons();
	}

	void OnDestroy()
	{
		if (m_MenuCanvas != null)
            U.Destroy(m_MenuCanvas.gameObject);
	}

	private void CreateToolButtons()
	{
		foreach (var menuTool in MenuTools)
		{
			var newButton = U.InstantiateAndSetActive(m_ButtonTemplate, m_Layout, false);
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
            if (SelectTool(this, t))
                U.Destroy(this);
        });
        b.onClick.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
    }
}
