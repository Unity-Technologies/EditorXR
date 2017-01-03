using UnityEngine;
using UnityEngine.Experimental.EditorVR.Actions;

/// <summary>
/// A convenience class for simple action implementations
/// </summary>
public abstract class BaseAction : MonoBehaviour, IAction
{
	public Sprite icon { get { return m_Icon; } }
	[SerializeField]
	Sprite m_Icon;

	public abstract void ExecuteAction();
}
