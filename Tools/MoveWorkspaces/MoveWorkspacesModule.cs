using UnityEngine;
using UnityEngine.VR.Workspaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;
using UnityEditor.VR;
using System;

[ExecuteInEditMode]
public class MoveWorkspacesModule : MonoBehaviour, ITool, ICustomActionMap, IRay, ICustomRay
{
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

	public Action showDefaultRay
	{
		private get; set;
	}

	public Action hideDefaultRay
	{
		private get; set;
	}

	float m_HeldTimeStamp = 0.0f;
	const float kMoveTime = 1.0f;
	const float kDestroyThrowTime = 0.3f;
	const float kDoubleTriggerTapTime = 0.3f;
	private Workspace[] m_AllWorkspaces;
	private Vector3[] m_StartPositions;
	private Quaternion[] m_StartAngles;
	private Vector3 m_RayOriginStartPos;
	private Quaternion m_RayOriginStartAngle;

	private Vector3 m_StartThrowPosition;
	private bool m_StartedThrowing = false;

	private enum ManipulateMode
	{
		On,
		Off,
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
					m_StartedThrowing = false;
					if(IsControllerAboveHMD())
					{
						if(Time.realtimeSinceStartup - m_HeldTimeStamp < kDoubleTriggerTapTime)
						{
							m_AllWorkspaces = GetComponentsInChildren<Workspace>();
							if(m_AllWorkspaces.Length > 0)
							{
								for(int i = 0; i < m_AllWorkspaces.Length; i++)
								{
									m_AllWorkspaces[i].OnDoubleTriggerTapAboveHMD();
								}
							}
						}
						m_HeldTimeStamp = Time.realtimeSinceStartup;
					}
				}
				else if(m_MoveWorkspacesInput.trig.isHeld)
				{
					if(IsControllerAboveHMD())
					{
						if(Time.realtimeSinceStartup - m_HeldTimeStamp > kDestroyThrowTime)
						{
							if(UserThrowsDown())
							{
								m_AllWorkspaces = GetComponentsInChildren<Workspace>();
								if(m_AllWorkspaces.Length > 0)
								{
									for(int i = 0; i < m_AllWorkspaces.Length; i++)
									{
										m_AllWorkspaces[i].OnCloseClicked();
									}
								}
							}
						}
						if(Time.realtimeSinceStartup - m_HeldTimeStamp > kMoveTime)
						{
							m_AllWorkspaces = GetComponentsInChildren<Workspace>();
							if(m_AllWorkspaces.Length > 0)
								SetManipulationStarted();
							else
								return;
						} 
					}
				}
				break;
			}
			case ManipulateMode.On:
			{
				if(m_MoveWorkspacesInput.trig.isHeld)
				{
					Vector3 rayDelta = (rayOrigin.position - m_RayOriginStartPos) * 1.5f;
					Quaternion rayCurrentAngle = Quaternion.LookRotation(rayOrigin.forward);
					float rayAngleDelta = Quaternion.Angle(m_RayOriginStartAngle,rayCurrentAngle);
					float sign = rayCurrentAngle.eulerAngles.y - m_RayOriginStartAngle.eulerAngles.y;
					if(sign < 0.0f)
						rayAngleDelta *= -1;

					for(int i = 0; i < m_AllWorkspaces.Length; i++)
					{
						if(Mathf.Abs(rayDelta.y) > 0.1f)
							m_AllWorkspaces[i].transform.position = new Vector3(m_AllWorkspaces[i].transform.position.x,m_StartPositions[i].y + rayDelta.y,m_AllWorkspaces[i].transform.position.z);

						if(Mathf.Abs(rayAngleDelta) > 1.0f)
							m_AllWorkspaces[i].transform.RotateAround(VRView.viewerPivot.position,Vector3.up,rayAngleDelta);

						//workspaces should look at center on Y axis
						Quaternion temp = m_AllWorkspaces[i].transform.rotation;
						temp.SetLookRotation(m_AllWorkspaces[i].transform.position,Vector3.up - VRView.viewerPivot.position);
						m_AllWorkspaces[i].transform.rotation = temp;
					}
					m_RayOriginStartAngle = rayCurrentAngle;
				}
				if(m_MoveWorkspacesInput.trig.wasJustReleased)
				{
					mode = ManipulateMode.Off;
					showDefaultRay();
				}
				break;
			}
		}
	}

	bool IsControllerAboveHMD()
	{
		if(rayOrigin.position.y > VRView.viewerCamera.transform.position.y)
			return true;

		return false;
	}

	void SetManipulationStarted()
	{
		m_RayOriginStartPos = rayOrigin.position;
		m_RayOriginStartAngle = Quaternion.LookRotation(rayOrigin.forward);
		m_StartPositions = new Vector3[m_AllWorkspaces.Length];
		for(int i = 0; i < m_StartPositions.Length; i++)
		{
			m_StartPositions[i] = m_AllWorkspaces[i].transform.position;
			Vector3 wsDir = VRView.viewerPivot.position - m_AllWorkspaces[i].transform.position;
		}
		mode = ManipulateMode.On;
		hideDefaultRay();
	}

	bool UserThrowsDown()
	{
		if(!m_StartedThrowing)
		{
			m_StartedThrowing = true;
			m_StartThrowPosition = rayOrigin.position;
			return false;
		}
		else
		{
			float deltaY = m_StartThrowPosition.y - rayOrigin.position.y;
			Debug.Log(deltaY.ToString());
			if(deltaY > 0.1f)
			{
				return true;
			}
			return false;
		}
	}
}