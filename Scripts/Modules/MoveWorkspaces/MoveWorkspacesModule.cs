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

				if (standardInput.action.isHeld)
					UpdateWorkspaceManipulation();

				if (standardInput.action.wasJustReleased)
				{
					mode = ManipulateMode.Off;
					showDefaultRay(rayOrigin);

					for (int i = 0; i < m_AllWorkspaces.Length; i++)
					{
						m_AllWorkspaces[i].SetUIHighlights(false);
						m_AllWorkspaces[i].GetComponentInChildren<SmoothMotion>().SetRotationSmoothing(10f);
						m_AllWorkspaces[i].GetComponentInChildren<SmoothMotion>().SetPositionSmoothing(10f);
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

					for (int i = 0; i < m_AllWorkspaces.Length; i++)
						m_AllWorkspaces[i].OnCloseClicked();
				}
			}
		}
	}

	bool UserThrowsDown()
	{
		const float kLocalScaleWhenReadyToThrow = 0.5f;

		m_VerticalVelocity = (m_PreviousPosition.y - rayOrigin.position.y) * Time.unscaledDeltaTime;
		m_PreviousPosition = rayOrigin.position;

		if (m_VerticalVelocity > 0.003f)
		{
			for (int i = 0; i < m_AllWorkspaces.Length; i++)
			{
				m_AllWorkspaces[i].transform.localScale = Vector3.one * kLocalScaleWhenReadyToThrow;
			}

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
				for (int i = 0; i < m_AllWorkspaces.Length; i++)
				{
					m_AllWorkspaces[i].transform.localScale = Vector3.one;
				}
				return false;
			}
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
				for (int i = 0; i < m_AllWorkspaces.Length; i++)
				{
					m_AllWorkspaces[i].SetUIHighlights(true);
					m_AllWorkspaces[i].GetComponentInChildren<SmoothMotion>().SetRotationSmoothing(2f);
					m_AllWorkspaces[i].GetComponentInChildren<SmoothMotion>().SetPositionSmoothing(2f);
				}
			}
			else
			{
				return;
			}
		}
	}

	void UpdateWorkspaceManipulation()
	{
		if (m_ThrowDownTriggered)
			return;

		Quaternion rayOriginCurrentAngle = Quaternion.LookRotation(rayOrigin.up);
		float deltaAngleY = rayOriginCurrentAngle.eulerAngles.y - m_RayOriginStartAngle.eulerAngles.y;

		for (int i = 0; i < m_AllWorkspaces.Length; i++)
		{
			//don't move for tiny movements
			if (Mathf.Abs(m_VerticalVelocity) > 0.00015f)
			{
				// move on Y axis with corrected direction
				m_AllWorkspaces[i].transform.Translate(0.0f, m_VerticalVelocity * -50.0f, 0.0f, Space.World);
			}

			//don't rotate for tiny rotations
			if (Mathf.Abs(deltaAngleY) > (80.0f * Time.unscaledDeltaTime))
				m_AllWorkspaces[i].transform.RotateAround(VRView.viewerCamera.transform.position, Vector3.up, deltaAngleY);
		}
		//save current rotation for next frame math
		m_RayOriginStartAngle = rayOriginCurrentAngle;
	}

	void OnDestroy()
	{
		U.Object.Destroy(m_TopHat);
	}
}
