using System;
using UnityEngine;
using UnityEditor.VR;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;

public class JoystickLocomotionTool : MonoBehaviour, ITool, ILocomotor, ICustomActionMap
{

	[SerializeField]
	private float m_MoveSpeed = 1f;
	[SerializeField]
	private float m_TurnSpeed = 30f;

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

	public void ProcessInput(ActionMapInput input, Action<InputControl> consumeControl)
	{
		var joystickLocomotionInput = (JoystickLocomotion)input;

		var moveDirection =
			(Vector3.forward * joystickLocomotionInput.moveForward.value +
			 Vector3.right * joystickLocomotionInput.moveRight.value).normalized;
		moveDirection = VRView.viewerCamera.transform.TransformVector(moveDirection);
		m_ViewerPivot.Translate(moveDirection * m_MoveSpeed * Time.unscaledDeltaTime, Space.World);
		m_ViewerPivot.Rotate(Vector3.up, joystickLocomotionInput.yaw.value * m_TurnSpeed * Time.unscaledDeltaTime, Space.Self);
	}
}
