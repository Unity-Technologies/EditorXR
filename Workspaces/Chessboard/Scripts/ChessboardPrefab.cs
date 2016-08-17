using System;
using System.Collections.Generic;
using UnityEngine;			 

public class ChessboardPrefab : MonoBehaviour
{
	public Renderer grid;
	public Action<Transform, Transform> OnControlDragStart { private get; set; }
	public Action<Transform, Transform> OnControlDrag { private get; set; }
	public Action<Transform, Transform> OnControlDragEnd { private get; set; }

	public void ControlDragStart(Transform controlBox, Transform rayOrigin)
	{
		if (OnControlDragStart != null)
			OnControlDragStart(controlBox, rayOrigin);
	}

	public void ControlDrag(Transform controlBox, Transform rayOrigin)
	{
		if (OnControlDrag != null)
			OnControlDrag(controlBox, rayOrigin);
	}

	public void ControlDragEnd(Transform controlBox, Transform rayOrigin)
	{
		if (OnControlDragEnd != null)
			OnControlDragEnd(controlBox, rayOrigin);
	}
}
