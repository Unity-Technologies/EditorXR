using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEngine;
using UnityEngine.InputNew;

[ExecuteInEditMode]
public class MoveWorkspacesTool : MonoBehaviour, ITool, IStandardActionMap, IUsesRayOrigin, ICustomRay, IUsesViewerBody, 
	IMoveWorkspaces, IGetAllWorkspaces
{
	float m_TriggerPressedTimeStamp;

	List<IWorkspace> m_AllWorkspaces;
	Quaternion[] m_WorkspaceLocalRotaions;
	float[] m_ExtraYOffsetForLookat;

	Quaternion m_RayOriginStartAngle;
	bool m_ThrowDownTriggered;
	Vector3 m_PreviousPosition;
	float m_VerticalVelocity;

	float m_ThrowingTimeStamp;
	float m_CurrentTargetScale = 1.0f;

	float m_TargetAngleY;

	const float k_ThresholdY = 0.2f;
	
	bool m_ManipulateModeOn;

	public Transform rayOrigin { get; set; }

	public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
	{
		var action = ((Standard)input).action;

		if (!m_ManipulateModeOn)
		{
			if (!this.IsAboveHead(rayOrigin))
				return;

			if (action.wasJustPressed)
			{
				if (UIUtils.IsDoubleClick(Time.realtimeSinceStartup - m_TriggerPressedTimeStamp))
				{
					m_ThrowDownTriggered = false;
					this.ResetWorkspaces();
				}
				m_TriggerPressedTimeStamp = Time.realtimeSinceStartup;
				consumeControl(action);
			}
			else if (action.isHeld)
				HandleManipulationStart();
		}
		else
		{
			HandleThrowDown(action.wasJustReleased);
			UpdateWorkspaceScales();

			if (action.isHeld)
			{
				UpdateWorkspaceManipulation();
				UpdateLookAtPlayer();
			}

			if (action.wasJustReleased)
				HandleManipulationEnd();
		}
	}

	bool FindWorkspaces()
	{
		m_ThrowDownTriggered = false;

		m_AllWorkspaces = this.GetAllWorkspaces();
		var workspacesCount = m_AllWorkspaces.Count;
		m_WorkspaceLocalRotaions = new Quaternion[workspacesCount];
		m_ExtraYOffsetForLookat = new float[workspacesCount];

		var cameraPosition = VRView.viewerCamera.transform.position;
		for (int i = 0; i < m_AllWorkspaces.Count; i++)
		{
			m_WorkspaceLocalRotaions[i] = Quaternion.Euler(m_AllWorkspaces[i].transform.localRotation.eulerAngles.x, 0.0f, 0.0f);

			var yOffset = m_AllWorkspaces[i].transform.position.y - cameraPosition.y;
			if (yOffset > k_ThresholdY)
				m_ExtraYOffsetForLookat[i] = yOffset - k_ThresholdY;
			else if (yOffset < -k_ThresholdY)
				m_ExtraYOffsetForLookat[i] = yOffset + k_ThresholdY;
			else
				m_ExtraYOffsetForLookat[i] = 0.0f;
		}

		return workspacesCount > 0;
	}

	void HandleThrowDown(bool wasJustReleased)
	{
		if (UserThrowsDown() && !m_ThrowDownTriggered)
		{
			if (wasJustReleased && FindWorkspaces())
			{
				m_ThrowDownTriggered = true;

				foreach (var ws in m_AllWorkspaces)
					ws.Close();
			}
		}
	}

	bool UserThrowsDown()
	{
		const float kLocalScaleWhenReadyToThrow = 0.5f;
		const float kThrowVelocityThreshold = 0.003f;
		const float kThrowDelayAllowed = 0.2f;

		m_VerticalVelocity = (m_PreviousPosition.y - rayOrigin.position.y) * Time.unscaledDeltaTime;
		m_PreviousPosition = rayOrigin.position;

		if (m_VerticalVelocity > kThrowVelocityThreshold)
		{
			m_CurrentTargetScale = kLocalScaleWhenReadyToThrow;
			m_ThrowingTimeStamp = Time.realtimeSinceStartup;
			return true;
		}
		else
		{
			if (Time.realtimeSinceStartup - m_ThrowingTimeStamp < kThrowDelayAllowed)
			{
				return true;
			}
			else
			{
				m_CurrentTargetScale = 1.0f;
				return false;
			}
		}
	}

	void UpdateWorkspaceScales()
	{
		const float kScaleSpeed = 15.0f;
		const float kSnapScaleValue = 0.1f;

		foreach(var ws in m_AllWorkspaces)
		{
			float currentScale = ws.transform.localScale.x;
			
			//snap scale if close enough to target
			if (currentScale > m_CurrentTargetScale - kSnapScaleValue && currentScale < m_CurrentTargetScale + kSnapScaleValue)
			{
				ws.transform.localScale = Vector3.one * m_CurrentTargetScale;
				continue;
			}
			ws.transform.localScale = Vector3.Lerp(ws.transform.localScale, Vector3.one * m_CurrentTargetScale, Time.unscaledDeltaTime * kScaleSpeed);
		}
	}

	void HandleManipulationStart()
	{
		const float kEnterMovementModeTime = 1.0f;

		if (Time.realtimeSinceStartup - m_TriggerPressedTimeStamp > kEnterMovementModeTime)
		{
			if (FindWorkspaces())
			{
				m_PreviousPosition = rayOrigin.position;
				m_RayOriginStartAngle = Quaternion.LookRotation(rayOrigin.up);
				m_ManipulateModeOn = true;

				this.HideDefaultRay(rayOrigin);
				this.LockRay(rayOrigin, this);

				foreach (var ws in m_AllWorkspaces)
				{
					var workspace = ws as Workspace;
					if (workspace)
						workspace.SetUIHighlights(true);
				}
			}
		}
	}
	
	void UpdateWorkspaceManipulation()
	{
		if (m_ThrowDownTriggered)
			return;

		const float kRotateAroundSpeed = 5.0f;
		const float kVerticalMoveSpeed = 45.0f;

		Quaternion rayOriginCurrentAngle = Quaternion.LookRotation(rayOrigin.up);
		var deltaAngleY = rayOriginCurrentAngle.eulerAngles.y - m_RayOriginStartAngle.eulerAngles.y;
		m_TargetAngleY += deltaAngleY;

		var rotateAmount = m_TargetAngleY * Time.unscaledDeltaTime * kRotateAroundSpeed;
		foreach (var ws in m_AllWorkspaces)
		{
			//don't rotate for tiny rotations
			if (Mathf.Abs(rotateAmount) > 0.1f)
				ws.transform.RotateAround(VRView.viewerCamera.transform.position, Vector3.up, rotateAmount);
			
			//don't move for tiny movements
			if (Mathf.Abs(m_VerticalVelocity) > 0.0001f)
				ws.transform.Translate(0.0f, m_VerticalVelocity * -kVerticalMoveSpeed, 0.0f, Space.World);
		}

		//update variables for next frame math
		m_TargetAngleY -= rotateAmount;
		m_RayOriginStartAngle = rayOriginCurrentAngle;
	}

	void UpdateLookAtPlayer()
	{
		var workspaceRotationSpeed = Time.unscaledDeltaTime * 10.0f;

		// workspaces look at player on their X axis beyond Y thresholds
		Vector3 cameraPosition = VRView.viewerCamera.transform.position;
		for (int i = 0; i < m_AllWorkspaces.Count; i++)
		{
			Transform wsTrans = m_AllWorkspaces[i].transform;
			float yOffset = wsTrans.position.y - cameraPosition.y;

			if (Mathf.Abs(yOffset) > k_ThresholdY)
			{
				float sign = Mathf.Sign(yOffset);
				Vector3 offset = Vector3.up * k_ThresholdY * sign + Vector3.up * m_ExtraYOffsetForLookat[i];
				Vector3 wsForward = wsTrans.position - (cameraPosition + offset);
				Quaternion targetRotation = Quaternion.LookRotation(wsForward) * m_WorkspaceLocalRotaions[i];
				wsTrans.rotation = Quaternion.Lerp(wsTrans.rotation, targetRotation, workspaceRotationSpeed);
			}
		}
	}

	void HandleManipulationEnd()
	{
		m_ManipulateModeOn = false;
		this.UnlockRay(rayOrigin, this);
		this.ShowDefaultRay(rayOrigin);

		foreach (var ws in m_AllWorkspaces)
		{
			var workspace = ws as Workspace;
			if (workspace)
				workspace.SetUIHighlights(false);
		}
	}
}
