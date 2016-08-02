using UnityEngine;
using UnityEngine.VR.Utilities;

public class ChessboardWorkspace : Workspace
{
	//NOTE: since pretty much all workspaces will want a prefab, should this go in the base class?
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
