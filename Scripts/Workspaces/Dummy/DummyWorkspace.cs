using UnityEngine;
using UnityEngine.VR.Utilities;

public class DummyWorkspace : Workspace
{
	[SerializeField]
	GameObject contentPrefab;

	public override void Awake()
	{
		base.Awake();
		GameObject content = U.Object.ClonePrefab(contentPrefab, sceneContainer);
		content.transform.localPosition = Vector3.zero;
		content.transform.localRotation = Quaternion.identity;
		content.transform.localScale = Vector3.one;
	}
}
