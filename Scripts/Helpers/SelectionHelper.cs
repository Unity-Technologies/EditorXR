using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class SelectionHelper : MonoBehaviour
{
	[Serializable]
	public class SelectionEvent : UnityEvent<Transform, Transform> { }
	public enum SelectionMode
	{
		DIRECT,
		REMOTE,
		BOTH
	}

	public enum TransformMode
	{
		DIRECT,
		REMOTE,
		CUSTOM
	}

	public SelectionMode selectionMode;
	public TransformMode transformMode;
	public GameObject selectionTarget;

	public SelectionEvent onSelect = new SelectionEvent();
	public SelectionEvent onHeld = new SelectionEvent();
	public SelectionEvent onRelease = new SelectionEvent();

	void Start()
	{
		if (selectionTarget == null)
			selectionTarget = gameObject;
	}
}