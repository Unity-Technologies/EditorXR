#define DEBUGDRAW

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VR.Utilities;

public abstract class Workspace : MonoBehaviour
{
	public Bounds bounds { get; private set; }
	private const float handleMargin = 0.2f;
	[SerializeField]
	private GameObject basePrefab;

	protected WorkspaceHandle handle;

	//TODO: discuss/design starting transform
	private static readonly Vector3 s_StartPosition = new Vector3(0,-0.15f,0.8f);
	private static readonly Quaternion s_StartRotation = Quaternion.AngleAxis(-25, Vector3.right);
	private static readonly Vector3 s_InitBounds = new Vector3(0.8f, 0.6f, 0.6f);

	//TODO: decide how to track existing workspaces
	static readonly Dictionary<Type, List<Workspace>> s_ExistingTypes = new Dictionary<Type, List<Workspace>>(); 

	//TODO: Refactor this function to not use parent
	public static void ShowWorkspace<T>(Transform parent) where T: Workspace {
		ShowWorkspace(typeof(T), parent);
	}
	public static void ShowWorkspace(Type t, Transform parent)
	{
		if (!s_ExistingTypes.ContainsKey(t))
		{
			s_ExistingTypes[t] = new List<Workspace> { CreateWorkspace(t, parent) };
		}
	}
	public static T CreateNewWorkspace<T>(Transform parent) where T : Workspace {
		return CreateWorkspace<T>(parent);
	}

	static T CreateWorkspace<T>(Transform parent) where T : Workspace {
		return (T)CreateWorkspace(typeof(T), parent);
	}
	static Workspace CreateWorkspace(Type t, Transform parent)
	{
		//TODO: ASSERT if t isn't assignable to Workspace
		//Q: Is the cast necessary here? Could we return a Component instead?
		return (Workspace)U.Object.CreateGameObjectWithComponent(t, parent);
	}

	public virtual void Awake()
	{
		transform.position = s_StartPosition;
		transform.rotation = s_StartRotation;
		GameObject baseObject = U.Object.ClonePrefab(basePrefab.gameObject, gameObject);
		handle = baseObject.GetComponent<WorkspaceHandle>();
		handle.owner = this;
		//TODO: ASSERT if handleRect is null
		//Question: use SetParent(transform, false)?
		baseObject.transform.localPosition = Vector3.zero;
		baseObject.transform.localRotation = Quaternion.identity;
		//baseObject.transform.localScale = Vector3.one;   
		foreach (Canvas canvas in GetComponentsInChildren<Canvas>())
			canvas.worldCamera = EditorVR.eventCamera;
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
			handle.handle.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, b.size.x + handleMargin);
			handle.handle.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, b.size.z + handleMargin);
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

	void OnDestroy()
	{
		if (s_ExistingTypes[GetType()].Remove(this)) {
			s_ExistingTypes.Remove(GetType());
		}
	}
}
