using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VR;
using UnityEngine.InputNew;
using UnityEngine.VR;
using UnityEngine.VR.Proxies;
using UnityEngine.VR.Tools;

public class JoystickLocomotionTool : MonoBehaviour, ITool, ILocomotion, ICustomActionMap
{

	[SerializeField]
	private float m_MoveSpeed = 1f;
	[SerializeField]
	private float m_TurnSpeed = 30f;
	[SerializeField]
	private PlayerInput m_PlayerInput;

	public Transform viewerPivot
	{
		set { m_ViewerPivot = value; }
	}
	[SerializeField]
	private Transform m_ViewerPivot;

	public ActionMap actionMap
	{
		get { return m_LocomotionActionMap; }
	}
	[SerializeField]
	private ActionMap m_LocomotionActionMap;

	public ActionMapInput actionMapInput
	{
		get { return m_JoystickLocomotionInput; }
		set { m_JoystickLocomotionInput = (JoystickLocomotion)value; }
	}
	private JoystickLocomotion m_JoystickLocomotionInput;

	public Node selfNode { get; set; }

	void Start()
	{
		if (m_JoystickLocomotionInput == null && m_PlayerInput)
			m_JoystickLocomotionInput = m_PlayerInput.GetActions<JoystickLocomotion>();
	
	}

	void Update()
	{
		var moveDirection =
			(Vector3.forward * m_JoystickLocomotionInput.moveForward.value +
			 Vector3.right * m_JoystickLocomotionInput.moveRight.value).normalized;
		moveDirection = VRView.viewerCamera.transform.TransformVector(moveDirection);
		m_ViewerPivot.Translate(moveDirection * m_MoveSpeed * Time.unscaledDeltaTime, Space.World);
		m_ViewerPivot.Rotate(Vector3.up, m_JoystickLocomotionInput.yaw.value * m_TurnSpeed * Time.unscaledDeltaTime, Space.Self);	    
	}
}
