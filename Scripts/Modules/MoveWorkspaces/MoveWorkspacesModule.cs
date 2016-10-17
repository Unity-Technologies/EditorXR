using UnityEngine;
using UnityEngine.VR.Workspaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;
using UnityEditor.VR;
using System;

[ExecuteInEditMode]
public class MoveWorkspacesModule : MonoBehaviour, IStandardActionMap, IRay, ICustomRay, IMoveWorkspaces
{
	public Standard standardInput { set; get; }

	public Transform rayOrigin { get; set; }

	public Action showDefaultRay { private get; set; }

	public Action hideDefaultRay { private get; set; }

	private float m_TriggerPressedTimeStamp = 0.0f;
	private Workspace[] m_AllWorkspaces;
	private Vector3[] m_StartPositions;
	private Vector3 m_RayOriginStartPos;
	private Quaternion m_RayOriginStartAngle;
	private Vector3 m_StartThrowPosition;
	private bool m_StartedThrowing = false;
	private bool m_ThrowDownTriggered = false;

	private enum ManipulateMode
	{
		On,
		Off,
	}
	private ManipulateMode mode = ManipulateMode.Off;

	void Update()
	{
		if(standardInput == null)
			return;

		standardInput.active = IsControllerAboveHMD();
		if(!standardInput.active)
			return;

		switch(mode)
		{
			case ManipulateMode.Off:
			{
				if(standardInput.action.wasJustPressed)
				{
					HandleDoubleTap();
				}
				else if(standardInput.action.isHeld)
				{
					HandleThrowDown();
					HandleManipulationStart();
				}
				break;
			}
			case ManipulateMode.On:
			{
				if(standardInput.action.isHeld)
				{
					UpdateWorkspaceManipulation();
				}
				else if(standardInput.action.wasJustReleased)
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

	bool FindWorkspaces()
	{
		m_AllWorkspaces = GetComponentsInChildren<Workspace>();

		if(m_AllWorkspaces.Length > 0)
			return true;
		else
			return false;
	}

	void HandleDoubleTap()
	{
		m_StartedThrowing = false;
		if(Time.realtimeSinceStartup - m_TriggerPressedTimeStamp < 1.5f)
		{
			if(FindWorkspaces())
			{
				m_ThrowDownTriggered = false;

				for(int i = 0; i < m_AllWorkspaces.Length; i++)
					m_AllWorkspaces[i].OnDoubleTriggerTapAboveHMD();
			}
		}
		m_TriggerPressedTimeStamp = Time.realtimeSinceStartup;
	}

	void HandleThrowDown()
	{
		if(Time.realtimeSinceStartup - m_TriggerPressedTimeStamp < 0.6f)
		{
			if(UserThrowsDown() && !m_ThrowDownTriggered)
			{
				if(FindWorkspaces())
				{
					m_ThrowDownTriggered = true;

					for(int i = 0; i < m_AllWorkspaces.Length; i++)
						m_AllWorkspaces[i].OnCloseClicked();
				}
			}
		}
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
			if(deltaY > 0.1f)
			{
				return true;
			}
			return false;
		}
	}

	void HandleManipulationStart()
	{
		if(Time.realtimeSinceStartup - m_TriggerPressedTimeStamp > 1.0f)
		{
			if(FindWorkspaces())
			{
				m_RayOriginStartPos = rayOrigin.position;
				m_RayOriginStartAngle = Quaternion.LookRotation(rayOrigin.up);
				m_StartPositions = new Vector3[m_AllWorkspaces.Length];

				for(int i = 0; i < m_StartPositions.Length; i++)
					m_StartPositions[i] = m_AllWorkspaces[i].transform.position;
				
				mode = ManipulateMode.On;
				hideDefaultRay();
			}
			else
			{
				return;
			}
		}
	}

	void UpdateWorkspaceManipulation()
	{
		float deltaPosY = rayOrigin.position.y - m_RayOriginStartPos.y;

		Quaternion rayOriginCurrentAngle = Quaternion.LookRotation(rayOrigin.up);
		float deltaAngleY = rayOriginCurrentAngle.eulerAngles.y - m_RayOriginStartAngle.eulerAngles.y;

		for(int i = 0; i < m_AllWorkspaces.Length; i++)
		{
			//don't move for tiny movements
			if(Mathf.Abs(deltaPosY) > (1.0f * Time.unscaledDeltaTime))
			{
				m_AllWorkspaces[i].transform.position = new Vector3(m_AllWorkspaces[i].transform.position.x,m_StartPositions[i].y + (deltaPosY * 80.0f * Time.unscaledDeltaTime),m_AllWorkspaces[i].transform.position.z);
				m_StartPositions[i] = m_AllWorkspaces[i].transform.position;
			}

			//don't rotate for tiny rotations
			if(Mathf.Abs(deltaAngleY) > (60.0f * Time.unscaledDeltaTime))
				m_AllWorkspaces[i].transform.RotateAround(VRView.viewerPivot.position,Vector3.up,deltaAngleY);

			//workspaces should look at center on Y axis
			Quaternion temp = m_AllWorkspaces[i].transform.rotation;
			temp.SetLookRotation(m_AllWorkspaces[i].transform.position,Vector3.up - VRView.viewerPivot.position);
			m_AllWorkspaces[i].transform.rotation = temp;
		}
		//save current pos and angle for next frame math
		m_RayOriginStartPos = rayOrigin.position;
		m_RayOriginStartAngle = rayOriginCurrentAngle;
	}
}