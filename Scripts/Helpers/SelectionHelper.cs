using UnityEngine;

public class SelectionHelper : MonoBehaviour
{
	public enum SelectionMode
	{
		DIRECT,
		REMOTE,
		BOTH
	}

	public SelectionMode selectionMode;
	public GameObject selectionTarget;
}