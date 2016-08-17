using System;
using System.Collections.Generic;
using UnityEngine;			 

public class ChessboardPrefab : MonoBehaviour
{
	public Renderer grid;
	public Action<Transform, Transform> OnControlDragStart { private get; set; }
	public Action<Transform, Workspace.Direction> OnControlDrag { private get; set; }
	public Action<Transform, Workspace.Direction> OnControlEnd { private get; set; }

	private readonly Dictionary<Transform, Vector3> m_DragStarts = new Dictionary<Transform,Vector3>();

	public void ControlDragStart(Transform controlBox, Transform rayOrigin)
	{
		m_DragStarts[rayOrigin] = rayOrigin.position;
	}

	public void ControlDrag(Transform controlBox, Transform rayOrigin)
	{
		switch (m_DragStarts.Count)
		{
			case 1:
				//Translate
				break;
			case 2:
				//Scale
				break;
		}
	}

	public void ControlDragEnd(Transform controlBox, Transform rayOrigin)
	{
		m_DragStarts.Remove(rayOrigin);
	}
}
