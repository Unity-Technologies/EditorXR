using System;
using UnityEngine;
using UnityEngine.VR.Utilities;

public abstract class Workspace : MonoBehaviour, IInstantiateUI
{
	public static readonly Vector3 kDefaultBounds = new Vector3(0.6f, 0.4f, 0.4f);

	/// <summary>
	/// Amount of space (in World units) between handle and content bounds in X and Z
	/// </summary>
	public const float kHandleMargin = 0.25f;

	/// <summary>
	/// Amount of height (in World units) between handle and content bounds
	/// </summary>
	public const float kContentHeight = 0.075f;

	/// <summary>
	/// Bounding box for workspace content. 
	/// </summary>
	public Bounds contentBounds
	{
		get { return m_ContentBounds; }
		set
		{
			if (!value.Equals(contentBounds))
			{
				value.center = contentBounds.center;
				m_ContentBounds = value;
				handle.SetBounds(contentBounds);
				OnBoundsChanged();
			}
		}
	}
	private Bounds m_ContentBounds;

	/// <summary>
	/// Bounding box for entire workspace, including UI handles
	/// </summary>
	public Bounds outerBounds
	{
		get
		{
			return new Bounds(contentBounds.center + Vector3.down * kContentHeight * 0.5f,
				new Vector3(
					contentBounds.size.x + kHandleMargin,
					contentBounds.size.y + kContentHeight,
					contentBounds.size.z + kHandleMargin
					));
		}
	}

	public Func<GameObject, GameObject> instantiateUI { private get; set; }
	protected WorkspaceUI handle;

	[SerializeField]
	private GameObject m_BasePrefab;

	public virtual void Setup()
	{
		GameObject baseObject = instantiateUI(m_BasePrefab);
		baseObject.transform.SetParent(transform);
		handle = baseObject.GetComponent<WorkspaceUI>();
		handle.OnHandleClick = OnHandleClick;
		handle.OnCloseClick = Close;
		handle.sceneContainer.transform.localPosition = Vector3.up * kContentHeight;
		baseObject.transform.localPosition = Vector3.zero;
		baseObject.transform.localRotation = Quaternion.identity;  
		//Do not set bounds directly, in case OnBoundsChanged requires Setup override to complete
		m_ContentBounds = new Bounds(Vector3.up * kDefaultBounds.y * 0.5f, kDefaultBounds);
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