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
	private Quaternion m_RayOriginStartAngle;
	private bool m_ThrowDownTriggered = false;
	private Vector3 m_PreviousPosition;
	private float m_VerticalVelocity;

	GameObject m_TopHat;

	bool m_GrabbedInTopHat;
	float m_ThrowingTimeStamp;
	const float kThrowDelayAllowed = 0.2f;
	float m_CurrentTargetScale = 1.0f;

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
				HandleThrowDown();
				UpdateWorkspaceScales();

				if (standardInput.action.isHeld)
					UpdateWorkspaceManipulation();

				if (standardInput.action.wasJustReleased)
				{
					mode = ManipulateMode.Off;
					showDefaultRay(rayOrigin);

					foreach(var ws in m_AllWorkspaces)
					{
						ws.SetUIHighlights(false);
						var smoothMotion = ws.GetComponentInChildren<SmoothMotion>();
						smoothMotion.SetRotationSmoothing(10f);
						smoothMotion.SetPositionSmoothing(10f);
					}
				}
				break;
			}
		}
	}

	bool IsControllerAboveHMD()
	{
		if (m_TopHat.GetComponent<Collider>().bounds.Contains(rayOrigin.position))
			return true;

		return false;
	}

	bool FindWorkspaces()
	{
		m_ThrowDownTriggered = false;

		m_AllWorkspaces = GetComponentsInChildren<Workspace>();

		if (m_AllWorkspaces.Length > 0)
		{
			foreach (var ws in m_AllWorkspaces)
			{
				if (ws.m_Hidden)
					return false;
			}
			return true;
		}
		else
		{
			return false;
		}
	}

	void HandleDoubleTap()
	{
		if (Time.realtimeSinceStartup - m_TriggerPressedTimeStamp < 0.8f)
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

		foreach(var ws in m_AllWorkspaces)
		{
			float currentScale = ws.transform.localScale.x;
			
			//snap scale if close enough to target
			if (currentScale > m_CurrentTargetScale - 0.1f && currentScale < m_CurrentTargetScale + 0.1f)
			{
				ws.transform.localScale = Vector3.one * m_CurrentTargetScale;
				continue;
			}
			ws.transform.localScale = Vector3.Lerp(ws.transform.localScale, Vector3.one * m_CurrentTargetScale, Time.unscaledDeltaTime * kScaleSpeed);
		}
	}

	void HandleManipulationStart()
	{
		if (Time.realtimeSinceStartup - m_TriggerPressedTimeStamp > 1.0f)
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
					smoothMotion.SetRotationSmoothing(1f);
					smoothMotion.SetPositionSmoothing(1f);
				}
			}
		}
	}

	void UpdateWorkspaceManipulation()
	{
		if (m_ThrowDownTriggered)
			return;

		Quaternion rayOriginCurrentAngle = Quaternion.LookRotation(rayOrigin.up);
		float deltaAngleY = rayOriginCurrentAngle.eulerAngles.y - m_RayOriginStartAngle.eulerAngles.y;

		foreach (var ws in m_AllWorkspaces)
		{
			//don't move for tiny movements
			if (Mathf.Abs(m_VerticalVelocity) > 0.0001f)
			{
				// move on Y axis with corrected direction
				ws.transform.Translate(0.0f, m_VerticalVelocity * -35.0f, 0.0f, Space.World);
			}

			//don't rotate for tiny rotations
			if (Mathf.Abs(deltaAngleY) > (80.0f * Time.unscaledDeltaTime))
				ws.transform.RotateAround(VRView.viewerCamera.transform.position, Vector3.up, deltaAngleY);
		}
		//save current rotation for next frame math
		m_RayOriginStartAngle = rayOriginCurrentAngle;
	}

	void OnDestroy()
	{
		U.Object.Destroy(m_TopHat);
	}
}
