#define DEBUGDRAW

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VR.Utilities;

public abstract class Workspace : MonoBehaviour, IInstantiateUI
{
	public Bounds bounds { get; private set; }
	public Func<GameObject, GameObject> instantiateUI { private get; set; }
	protected WorkspaceHandle handle;

	private const float k_HandleMargin = 0.2f;	//Amount of space (in UI units) between handle and content bounds
	[SerializeField]
	private GameObject m_BasePrefab;

	//TODO: discuss/design starting transform
	private static readonly Vector3 s_StartPosition = new Vector3(0,-0.15f,0.8f);
	private static readonly Quaternion s_StartRotation = Quaternion.AngleAxis(-25, Vector3.right);
	private static readonly Vector3 s_InitBounds = new Vector3(0.8f, 0.6f, 0.6f);

	public virtual void Setup()
	{							
		transform.position += transform.rotation * s_StartPosition;
		transform.rotation = s_StartRotation;
		GameObject baseObject = instantiateUI(m_BasePrefab);
		baseObject.transform.SetParent(transform);
		handle = baseObject.GetComponent<WorkspaceHandle>();
		handle.owner = this;
		//TODO: ASSERT if handleRect is null
		//Q: use SetParent(transform, false)?
		baseObject.transform.localPosition = Vector3.zero;
		baseObject.transform.localRotation = Quaternion.identity;
		//baseObject.transform.localScale = Vector3.one;   
		bounds = new Bounds(handle.sceneContainer.transform.position, s_InitBounds);
	}
#if DEBUGDRAW
	void OnDrawGizmos()
	{
		Gizmos.matrix = handle.sceneContainer.transform.localToWorldMatrix;
		Gizmos.DrawWireCube(Vector3.up * bounds.size.y, bounds.size * 2);
	}
#endif
	//Q: Should we allow SetBounds to change position?
	public void SetBounds(Bounds b)
	{
		if (!b.Equals(bounds))
		{
			b.center = bounds.center;
			bounds = b;
			handle.handle.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, b.size.x + k_HandleMargin);
			handle.handle.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, b.size.z + k_HandleMargin);
			OnBoundsChanged();
		}
	}
	protected abstract void OnBoundsChanged();

	public void OnBaseClick()
	{
		Debug.Log("click");
	}

	public void Close()
	{
		U.Object.Destroy(gameObject);
	} 
}
