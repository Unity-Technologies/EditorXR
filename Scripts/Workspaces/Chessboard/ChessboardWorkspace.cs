using UnityEngine;
using UnityEngine.VR.Utilities;

public class ChessboardWorkspace : Workspace
{
	//HACK: dummy input for bounds change
	[SerializeField]
	private Renderer dummyBounds;
	//NOTE: since pretty much all workspaces will want a prefab, should this go in the base class?
	[SerializeField]
	private GameObject contentPrefab;

	private Chessboard chessboard;

	public override void Awake()
	{
		base.Awake();
		GameObject content = U.Object.ClonePrefab(contentPrefab, sceneContainer);
		content.transform.localPosition = Vector3.zero;
		content.transform.localRotation = Quaternion.identity;
		content.transform.localScale = Vector3.one;
		chessboard = GetComponentInChildren<Chessboard>();
		//TODO: ASSERT if chessboard is false		   
	}
	void Update()
	{
		if (dummyBounds)
			SetBounds(dummyBounds.bounds);
	}

	protected override void OnBoundsChanged()
	{						
		chessboard.SetBounds(bounds);
	}
}
