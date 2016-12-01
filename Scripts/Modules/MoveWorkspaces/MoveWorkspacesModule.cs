using System;
using UnityEditor.VR;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Workspaces;

[ExecuteInEditMode]
public class MoveWorkspacesModule : MonoBehaviour, IStandardActionMap, IUsesRayOrigin, ICustomRay, IMoveWorkspaces
{
	float m_TriggerPressedTimeStamp;

	Workspace[] m_AllWorkspaces;
	Quaternion[] m_WorkspaceLocalRotaions;
	float[] m_ExtraYOffsetForLookat;

	Quaternion m_RayOriginStartAngle;
	bool m_ThrowDownTriggered;
	Vector3 m_PreviousPosition;
	float m_VerticalVelocity;

	Bounds m_TopHatBounds;
	bool m_GrabbedInTopHat;

	float m_ThrowingTimeStamp;
	float m_CurrentTargetScale = 1.0f;

	float m_targetAngleY;

	const float kThresholdY = 0.2f;
	
	bool m_ManipulateModeOn;

	public Transform rayOrigin { get; set; }

	public DefaultRayVisibilityDelegate showDefaultRay { private get; set; }

	public DefaultRayVisibilityDelegate hideDefaultRay { private get; set; }

	public Action resetWorkspaces { get; set; }

	void Start()
	{
		m_ManipulateModeOn = false;
		m_TopHatBounds = new Bounds(Vector3.up * 0.2f, new Vector3(0.2f, 0.15f, 0.2f));
	}

	public void ProcessInput(ActionMapInput input, Action<InputControl> consumeControl)
	{
		var standardInput = (Standard)input;

		if (!m_ManipulateModeOn)
		{
			standardInput.active = IsControllerAboveHMD();
			if (!standardInput.active)
				return;

			if (standardInput.action.wasJustPressed)
				HandleDoubleTap();

			if (m_GrabbedInTopHat && standardInput.action.isHeld)
				HandleManipulationStart();

			if (standardInput.action.wasJustReleased)
				m_GrabbedInTopHat = false;
		}
		else
		{
			HandleThrowDown(standardInput.action.wasJustReleased);
			UpdateWorkspaceScales();

			if (standardInput.action.isHeld)
			{
				UpdateWorkspaceManipulation();
				UpdateLookAtPlayer();
			}

			if (standardInput.action.wasJustReleased)
				HandleManipulationEnd();
		}
	}

	bool IsControllerAboveHMD()
	{
		Vector3 controllerLocalPos = VRView.viewerCamera.transform.worldToLocalMatrix.MultiplyPoint3x4(rayOrigin.position);
		return m_TopHatBounds.Contains(controllerLocalPos);
	}

	bool FindWorkspaces()
	{
		m_ThrowDownTriggered = false;

		m_AllWorkspaces = GetComponentsInChildren<Workspace>();
		m_WorkspaceLocalRotaions = new Quaternion[m_AllWorkspaces.Length];
		m_ExtraYOffsetForLookat = new float[m_AllWorkspaces.Length];

		var cameraPosition = VRView.viewerCamera.transform.position;
		for (int i = 0; i < m_AllWorkspaces.Length; i++)
		{
			m_WorkspaceLocalRotaions[i] = Quaternion.Euler(m_AllWorkspaces[i].transform.localRotation.eulerAngles.x, 0.0f, 0.0f);

			var yOffset = m_AllWorkspaces[i].transform.position.y - cameraPosition.y;
			if (yOffset > kThresholdY)
				m_ExtraYOffsetForLookat[i] = yOffset - kThresholdY;
			else if (yOffset < -kThresholdY)
				m_ExtraYOffsetForLookat[i] = yOffset + kThresholdY;
			else
				m_ExtraYOffsetForLookat[i] = 0.0f;
		}

		return m_AllWorkspaces.Length > 0;
	}

	void HandleDoubleTap()
	{
		const float kDoubleTapTime = 0.8f;

		if (Time.realtimeSinceStartup - m_TriggerPressedTimeStamp < kDoubleTapTime)
		{
			m_ThrowDownTriggered = false;
			resetWorkspaces();
		}
		m_TriggerPressedTimeStamp = Time.realtimeSinceStartup;
		m_GrabbedInTopHat = true;
	}

	void HandleThrowDown(bool wasJustReleased)
	{
		if (UserThrowsDown() && !m_ThrowDownTriggered)
		{
			if (wasJustReleased && FindWorkspaces())
			{
				m_ThrowDownTriggered = true;

				foreach (var ws in m_AllWorkspaces)
					ws.OnCloseClicked();
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
				hideDefaultRay(rayOrigin);

				foreach (var ws in m_AllWorkspaces)
					ws.SetUIHighlights(true);
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
		m_targetAngleY += deltaAngleY;

		var rotateAmount = m_targetAngleY * Time.unscaledDeltaTime * kRotateAroundSpeed;
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
		m_targetAngleY -= rotateAmount;
		m_RayOriginStartAngle = rayOriginCurrentAngle;
	}

	void UpdateLookAtPlayer()
	{
		var workspaceRotationSpeed = Time.unscaledDeltaTime * 10.0f;

		// workspaces look at player on their X axis beyond Y thresholds
		Vector3 cameraPosition = VRView.viewerCamera.transform.position;
		for (int i = 0; i < m_AllWorkspaces.Length; i++)
		{
			Transform wsTrans = m_AllWorkspaces[i].transform;
			float yOffset = wsTrans.position.y - cameraPosition.y;

			if (Mathf.Abs(yOffset) > kThresholdY)
			{
				float sign = Mathf.Sign(yOffset);
				Vector3 offset = Vector3.up * kThresholdY * sign + Vector3.up * m_ExtraYOffsetForLookat[i];
				Vector3 wsForward = wsTrans.position - (cameraPosition + offset);
				Quaternion targetRotation = Quaternion.LookRotation(wsForward) * m_WorkspaceLocalRotaions[i];
				wsTrans.rotation = Quaternion.Lerp(wsTrans.rotation, targetRotation, workspaceRotationSpeed);
			}
		}
	}

	void HandleManipulationEnd()
	{
		m_ManipulateModeOn = false;
		showDefaultRay(rayOrigin);

		foreach (var ws in m_AllWorkspaces)
			ws.SetUIHighlights(false);
	}
}
