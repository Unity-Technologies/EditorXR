using UnityEngine;
using UnityEngine.VR.Workspaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;
using UnityEditor.VR;
using System;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Helpers;

[ExecuteInEditMode]
public class MoveWorkspacesModule : MonoBehaviour, IStandardActionMap, IUsesRayOrigin, ICustomRay, IMoveWorkspaces
{
	public Standard standardInput { set; get; }

	public Transform rayOrigin { get; set; }

	public DefaultRayVisibilityDelegate showDefaultRay { private get; set; }

	public DefaultRayVisibilityDelegate hideDefaultRay { private get; set; }

	public Action<Workspace> resetWorkspaces { get; set; }

	private float m_TriggerPressedTimeStamp = 0.0f;
	private Workspace[] m_AllWorkspaces;
	private Quaternion[] m_WorkspaceLocalRotaions;
	private float[] m_ExtraOffset;

	private Quaternion m_RayOriginStartAngle;
	private bool m_ThrowDownTriggered = false;
	private Vector3 m_PreviousPosition;
	private float m_VerticalVelocity;

	GameObject m_TopHat;

	bool m_GrabbedInTopHat;
	float m_ThrowingTimeStamp;
	const float kThrowDelayAllowed = 0.2f;
	float m_CurrentTargetScale = 1.0f;

	float m_targetAngleY = 0.0f;

	const float kThresholdY = 0.2f;

	private enum ManipulateMode
	{
		On,
		Off,
	}
	private ManipulateMode mode = ManipulateMode.Off;

	void Start()
	{
		m_TopHat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		m_TopHat.transform.position = VRView.viewerCamera.transform.position;
		m_TopHat.transform.rotation = VRView.viewerCamera.transform.rotation;
		m_TopHat.transform.parent = VRView.viewerCamera.transform;

		//set cylinder on top of head
		m_TopHat.transform.localScale = new Vector3(0.2f, 0.15f, 0.2f);
		m_TopHat.transform.Translate(Vector3.up * 0.2f);
		m_TopHat.GetComponent<Collider>().isTrigger = true;
		m_TopHat.GetComponent<MeshRenderer>().enabled = false;
	}

	void Update()
	{
		if (standardInput == null)
			return;

		switch (mode)
		{
			case ManipulateMode.Off:
			{
				standardInput.active = IsControllerAboveHMD();
				if (!standardInput.active)
					return;

				if (standardInput.action.wasJustPressed)
					HandleDoubleTap();

				if (m_GrabbedInTopHat)
				{
					if (standardInput.action.isHeld)
						HandleManipulationStart();
				}

				if (standardInput.action.wasJustReleased)
					m_GrabbedInTopHat = false;

				break;
			}
			case ManipulateMode.On:
			{
				if (standardInput.action.isHeld)
				{
					HandleThrowDown();
					UpdateWorkspaceManipulation();
					UpdateLookAtPlayer();
				}

				if (standardInput.action.wasJustReleased)
					HandleManipulationEnd();

				break;
			}
		}
	}

	bool IsControllerAboveHMD()
	{
		return m_TopHat.GetComponent<Collider>().bounds.Contains(rayOrigin.position);
	}

	bool FindWorkspaces()
	{
		m_ThrowDownTriggered = false;

		m_AllWorkspaces = GetComponentsInChildren<Workspace>();
		m_WorkspaceLocalRotaions = new Quaternion[m_AllWorkspaces.Length];
		m_ExtraOffset = new float[m_AllWorkspaces.Length];

		var cameraPosition = VRView.viewerCamera.transform.position;
		for (int i = 0; i < m_AllWorkspaces.Length; i++)
		{
			float yOffset = m_AllWorkspaces[i].transform.position.y - cameraPosition.y;

			m_WorkspaceLocalRotaions[i] = Quaternion.Euler(m_AllWorkspaces[i].transform.localRotation.eulerAngles.x, 0.0f, 0.0f);

			if (yOffset > kThresholdY)
				m_ExtraOffset[i] = yOffset - kThresholdY;
			else if (yOffset < -kThresholdY)
				m_ExtraOffset[i] = yOffset + kThresholdY;
			else
				m_ExtraOffset[i] = 0.0f;
		}

		return m_AllWorkspaces.Length > 0;
	}

	void HandleDoubleTap()
	{
		const float kDoubleTapTime = 0.8f;

		if (Time.realtimeSinceStartup - m_TriggerPressedTimeStamp < kDoubleTapTime)
		{
			m_ThrowDownTriggered = false;
			resetWorkspaces(null);
		}
		m_TriggerPressedTimeStamp = Time.realtimeSinceStartup;
		m_GrabbedInTopHat = true;
	}

	void HandleThrowDown()
	{
		if (UserThrowsDown() && !m_ThrowDownTriggered)
		{
			if (standardInput.action.wasJustReleased)
			{
				if (FindWorkspaces())
				{
					m_ThrowDownTriggered = true;

					foreach (var ws in m_AllWorkspaces)
						ws.OnCloseClicked();
				}
			}
		}

		UpdateWorkspaceScales();
	}

	bool UserThrowsDown()
	{
		const float kLocalScaleWhenReadyToThrow = 0.5f;
		const float kThrowVelocityThreshold = 0.003f;

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
		const float kClampScaleValue = 0.1f;

		foreach(var ws in m_AllWorkspaces)
		{
			float currentScale = ws.transform.localScale.x;
			
			//snap scale if close enough to target
			if (currentScale > m_CurrentTargetScale - kClampScaleValue && currentScale < m_CurrentTargetScale + kClampScaleValue)
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
		const float kSmoothValue = 5.0f;

		if (Time.realtimeSinceStartup - m_TriggerPressedTimeStamp > kEnterMovementModeTime)
		{
			if (FindWorkspaces())
			{
				m_PreviousPosition = rayOrigin.position;
				m_RayOriginStartAngle = Quaternion.LookRotation(rayOrigin.up);
				mode = ManipulateMode.On;
				hideDefaultRay(rayOrigin);
				foreach (var ws in m_AllWorkspaces)
				{
					ws.SetUIHighlights(true);
					var smoothMotion = ws.GetComponentInChildren<SmoothMotion>();
					smoothMotion.enabled = true;
					smoothMotion.SetRotationSmoothing(kSmoothValue);
					smoothMotion.SetPositionSmoothing(kSmoothValue);
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
		float deltaAngleY = rayOriginCurrentAngle.eulerAngles.y - m_RayOriginStartAngle.eulerAngles.y;
		m_targetAngleY += deltaAngleY;

		float rotateAmount = m_targetAngleY * Time.unscaledDeltaTime * kRotateAroundSpeed;
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
		float kWorkspaceRotationSpeed = Time.unscaledDeltaTime * 10f;

		// workspaces look at player on their X axis beyond Y thresholds
		Vector3 cameraPosition = VRView.viewerCamera.transform.position;
		for (int i = 0; i < m_AllWorkspaces.Length; i++)
		{
			Transform wsTrans = m_AllWorkspaces[i].transform;
			float yOffset = wsTrans.position.y - cameraPosition.y;

			if (Mathf.Abs(yOffset) > kThresholdY)
			{
				float sign = Mathf.Sign(yOffset);
				Vector3 offset = Vector3.up * kThresholdY * sign + Vector3.up * m_ExtraOffset[i];
				Vector3 wsForward = wsTrans.position - (cameraPosition + offset);
				Quaternion targetRotation = Quaternion.LookRotation(wsForward) * m_WorkspaceLocalRotaions[i];

				wsTrans.rotation = Quaternion.Lerp(wsTrans.rotation, targetRotation, kWorkspaceRotationSpeed);
			}
		}
	}

	void HandleManipulationEnd()
	{
		const float kSetOriginalSmoothValue = 10.0f;

		mode = ManipulateMode.Off;
		showDefaultRay(rayOrigin);

		foreach (var ws in m_AllWorkspaces)
		{
			ws.SetUIHighlights(false);
			var smoothMotion = ws.GetComponentInChildren<SmoothMotion>();
			smoothMotion.SetRotationSmoothing(kSetOriginalSmoothValue);
			smoothMotion.SetPositionSmoothing(kSetOriginalSmoothValue);
		}
	}

	void OnDestroy()
	{
		U.Object.Destroy(m_TopHat);
	}
}
