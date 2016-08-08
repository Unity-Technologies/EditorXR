#define DEBUGDRAW

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VR.Utilities;

public abstract class Workspace : MonoBehaviour, IInstantiateUI
{
	public static readonly Vector3 defaultBounds = new Vector3(0.6f, 0.4f, 0.4f);
	//Amount of space (in World units) between handle and content bounds
	public const float handleMargin = 0.2f;
	public const float contentHeight = 0.075f;
	/// <summary>
	/// Bounding box for workspace content. 
	/// </summary>
	public Bounds bounds { get; private set; }

	public Bounds outerBounds
	{
		get
		{
			return new Bounds(bounds.center + Vector3.down * contentHeight * 0.5f,
				new Vector3(
					bounds.size.x + handleMargin,
					bounds.size.y + contentHeight,
					bounds.size.z + handleMargin
					));
		}
	}

	public Func<GameObject, GameObject> instantiateUI { private get; set; }
	protected WorkspaceHandle handle;
	
	[SerializeField]
	private GameObject m_BasePrefab;

	public virtual void Setup()
	{															   
		GameObject baseObject = instantiateUI(m_BasePrefab);
		baseObject.transform.SetParent(transform);
		handle = baseObject.GetComponent<WorkspaceHandle>();
		handle.owner = this;
		handle.sceneContainer.transform.localPosition = Vector3.up * contentHeight;
		//TODO: ASSERT if handleRect is null
		//Q: use SetParent(transform, false)?
		baseObject.transform.localPosition = Vector3.zero;
		baseObject.transform.localRotation = Quaternion.identity;
		//baseObject.transform.localScale = Vector3.one;   
		bounds = new Bounds(Vector3.up * defaultBounds.y * 0.5f, defaultBounds);
		handle.SetBounds(bounds);
	}
#if DEBUGDRAW
	void OnDrawGizmos()
	{
		Gizmos.matrix = handle.sceneContainer.transform.localToWorldMatrix;
		Gizmos.DrawWireCube(bounds.center, bounds.size);
		Gizmos.DrawWireCube(outerBounds.center, outerBounds.size);
	}
#endif
	//Q: Should we allow SetBounds to change position?
	public void SetBounds(Bounds b)
	{
		if (!b.Equals(bounds))
		{
			b.center = bounds.center;
			bounds = b;
			handle.SetBounds(bounds);
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
