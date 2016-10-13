using UnityEngine;
using UnityEngine.VR.Workspaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;

[ExecuteInEditMode]
public class MoveWorkspacesModule : MonoBehaviour, ITool, ICustomActionMap, IRay
{
	private Workspace[] m_AllWorkspaces;
	private Vector3[] m_StartPositions;

	private Vector3 m_RayOriginStartPos;

	public ActionMap actionMap
	{
		get
		{
			return m_MoveWorkspacesModuleActionMap;
        }
	}
	[SerializeField]
	private ActionMap m_MoveWorkspacesModuleActionMap;

	public ActionMapInput actionMapInput
	{
		get
		{
			return m_MoveWorkspacesInput;
		}
		set
		{
			m_MoveWorkspacesInput = (MoveWorkspacesInput)value;
		}
	}
	[SerializeField]
	private MoveWorkspacesInput m_MoveWorkspacesInput;

	public Transform rayOrigin
	{
		get; set;
	}

	float m_HeldTimeStamp = 0.0f;
	const float kPressedTime = 1.0f;
	Vector3 m_StartPos;
	Vector3 m_StartRot;

	private enum ManipulateMode
	{
		On,
		Off,
		Move,
		Rotate,
	}
	private ManipulateMode mode = ManipulateMode.Off;

	void Update()
	{
		switch(mode)
		{
			case ManipulateMode.Off:
			{
				if(m_MoveWorkspacesInput.trig.wasJustPressed)
				{
					m_HeldTimeStamp = Time.realtimeSinceStartup;
				}
				else if(m_MoveWorkspacesInput.trig.isHeld)
				{
					if(Time.realtimeSinceStartup - m_HeldTimeStamp > kPressedTime)
					{
						m_AllWorkspaces = GetComponentsInChildren<Workspace>();
						if(m_AllWorkspaces.Length > 0)
						{
							m_RayOriginStartPos = rayOrigin.position;
							m_StartPositions = new Vector3[m_AllWorkspaces.Length];
							for(int i = 0; i < m_StartPositions.Length; i++)
							{
								m_StartPositions[i] = m_AllWorkspaces[i].transform.position;
							}
							mode = ManipulateMode.On;
						}
						else
						{
							return;
						}
                    }
				}
				break;
			}
			case ManipulateMode.On:
			{
				if(m_MoveWorkspacesInput.trig.wasJustPressed)
				{
					m_AllWorkspaces = GetComponentsInChildren<Workspace>();
					if(m_AllWorkspaces.Length > 0)
					{
						m_RayOriginStartPos = rayOrigin.position;
						m_StartPositions = new Vector3[m_AllWorkspaces.Length];
						for(int i = 0; i < m_StartPositions.Length; i++)
						{
							m_StartPositions[i] = m_AllWorkspaces[i].transform.position;
						}
					}
				}
				else if(m_MoveWorkspacesInput.trig.isHeld)
				{
					Vector3 rayDelta = (rayOrigin.position - m_RayOriginStartPos);
					if(Mathf.Abs(rayDelta.y) > 0.1f)
					{
						mode = ManipulateMode.Move;
					}
					else if(Mathf.Abs(rayDelta.x) > 1.0f || Mathf.Abs(rayDelta.z) > 1.0f)
					{
						mode = ManipulateMode.Rotate;
					}
				}
				break;
			}
			case ManipulateMode.Move:
			{
				//move more than hand movement
				Vector3 rayDelta = (rayOrigin.position - m_RayOriginStartPos) * 4.5f;
				for(int i = 0; i < m_AllWorkspaces.Length; i++)
				{
					m_AllWorkspaces[i].transform.position = new Vector3(m_StartPositions[i].x,m_StartPositions[i].y + rayDelta.y,m_StartPositions[i].z);
				}

				if(m_MoveWorkspacesInput.trig.wasJustReleased)
				{
					mode = ManipulateMode.On;
				}
				break;
			}
			case ManipulateMode.Rotate:
			{
				if(m_MoveWorkspacesInput.trig.wasJustReleased)
				{
					mode = ManipulateMode.On;
				}
				break;
			}
		}
	}


}