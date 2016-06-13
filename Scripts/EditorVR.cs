using UnityEngine;
using System.Collections;
using System;
using UnityEngine.VR.Proxies;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.VR;
#endif

[InitializeOnLoad]
public class EditorVR : MonoBehaviour {
	public const HideFlags kDefaultHideFlags = HideFlags.DontSave;

	void Awake()
	{
		U.CreateGameObjectWithComponent<SixenseProxy>(transform);  //TODO change to proxy interface
	}

#if UNITY_EDITOR
	private static EditorVR s_Instance;
	private static readonly Type kType = typeof(EditorVR);

	static EditorVR()
	{
		EditorVRView.onEnable += OnEVREnabled;
		EditorVRView.onDisable += OnEVRDisabled;
	}

	private static void OnEVREnabled()
	{
		s_Instance = U.CreateGameObjectWithComponent<EditorVR>();
	}

	private static void OnEVRDisabled()
	{
		U.Destroy(s_Instance.gameObject);
	}
#endif
}
