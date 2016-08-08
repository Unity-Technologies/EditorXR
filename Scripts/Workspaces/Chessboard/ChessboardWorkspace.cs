using UnityEngine;
using UnityEngine.VR.Utilities;

public class ChessboardWorkspace : Workspace
{
	//HACK: dummy input for bounds change
	[SerializeField]
	private Renderer m_DummyBounds;
	//NOTE: since pretty much all workspaces will want a prefab, should this go in the base class?
	[SerializeField]
	private GameObject m_ContentPrefab;

	private Chessboard m_Chessboard;

	public override void Setup()
	{
		base.Setup();					
		GameObject content = U.Object.ClonePrefab(m_ContentPrefab, handle.sceneContainer);
		content.transform.localPosition = Vector3.zero;
		content.transform.localRotation = Quaternion.identity;
		content.transform.localScale = Vector3.one;
		m_Chessboard = GetComponentInChildren<Chessboard>();
		OnBoundsChanged();
		//TODO: ASSERT if chessboard is false		   
	}
	void Update()
	{
		if (m_DummyBounds)
			contentBounds = m_DummyBounds.bounds;
	}

	protected override void OnBoundsChanged()
	{
		m_Chessboard.SetBounds(contentBounds);
	}
}
