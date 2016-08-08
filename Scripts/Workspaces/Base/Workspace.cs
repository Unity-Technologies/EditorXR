using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VR.Utilities;

public abstract class Workspace : MonoBehaviour, IInstantiateUI
{
	public static readonly Vector3 defaultBounds = new Vector3(0.6f, 0.4f, 0.4f);
	//Amount of space (in World units) between handle and content bounds
	public const float handleMargin = 0.25f;
	public const float contentHeight = 0.075f;
	/// <summary>
	/// Bounding box for workspace content. 
	/// </summary>
	public Bounds contentBounds {
		get { return m_ContentBounds; }
		set
		{
			if (!value.Equals(contentBounds))
			{
				value.center = contentBounds.center;
				contentBounds = value;
				handle.SetBounds(contentBounds);
				OnBoundsChanged();
			}
		}
	}

	/// <summary>
	/// Bounding box for entire workspace, including UI handles
	/// </summary>
	public Bounds outerBounds
	{
		get
		{
			return new Bounds(contentBounds.center + Vector3.down * contentHeight * 0.5f,
				new Vector3(
					contentBounds.size.x + handleMargin,
					contentBounds.size.y + contentHeight,
					contentBounds.size.z + handleMargin
					));
		}
	}

	public Func<GameObject, GameObject> instantiateUI { private get; set; }
	protected WorkspaceUI handle;
	
	[SerializeField]
	private GameObject m_BasePrefab;

	private Bounds m_ContentBounds;

	public virtual void Setup()
	{
		GameObject baseObject = instantiateUI(m_BasePrefab);
		baseObject.transform.SetParent(transform);
		handle = baseObject.GetComponent<WorkspaceUI>();
		handle.OnHandleClick = OnHandleClick;
		handle.OnCloseClick = Close;
		handle.sceneContainer.transform.localPosition = Vector3.up*contentHeight;
		baseObject.transform.localPosition = Vector3.zero;
		baseObject.transform.localRotation = Quaternion.identity;
		//baseObject.transform.localScale = Vector3.one;   
		//Do not set bounds directly, in case OnBoundsChanged requires Setup override to complete
		m_ContentBounds = new Bounds(Vector3.up*defaultBounds.y*0.5f, defaultBounds);
		handle.SetBounds(contentBounds);
	}

	protected abstract void OnBoundsChanged();

	public virtual void OnHandleClick()
	{
		Debug.Log("click");
	}

	public virtual void Close()
	{
		U.Object.Destroy(gameObject);
	} 
}
