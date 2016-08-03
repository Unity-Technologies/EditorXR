using System;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class DummyWorkspace : Workspace
{
	[SerializeField]
	private GameObject m_ContentPrefab;

	public override void Awake()
	{
		base.Awake();
		GameObject content = U.Object.ClonePrefab(m_ContentPrefab, handle.sceneContainer);
		content.transform.localPosition = Vector3.zero;
		content.transform.localRotation = Quaternion.identity;
		content.transform.localScale = Vector3.one;
	}

	protected override void OnBoundsChanged()
	{											 
	}
}
