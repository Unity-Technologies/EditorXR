using UnityEngine;
using System.Collections;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;

public class JoystickLocomotionTool : MonoBehaviour, ITool, ILocomotion
{
    public ActionMap ActionMap
    {
        get { return m_LocomotionActionMap; }
    }

    public ActionMapInput ActionMapInput
    {
        get { return m_JoystickLocomotionInput; }
        set { m_JoystickLocomotionInput = (JoystickLocomotion)value; }
    }

    public bool SingleInstance
    {
        get { return true; }
    }

    public Transform ViewerPivot
    {
        set { m_ViewerPivot = value; }
    }

    [Header("Settings")]
    [SerializeField]
    private float m_MoveSpeed = 1f;
    [SerializeField]
    private float m_TurnSpeed = 30f;

    [Header("References")]
    [SerializeField]
    private Transform m_ViewerPivot;
    [SerializeField]
    private ActionMap m_LocomotionActionMap;
    [SerializeField]
    private PlayerInput m_PlayerInput;

    private JoystickLocomotion m_JoystickLocomotionInput;

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
        m_ViewerPivot.Translate(moveDirection * m_MoveSpeed * Time.unscaledDeltaTime, Space.Self);
        m_ViewerPivot.Rotate(Vector3.up, m_JoystickLocomotionInput.yaw.value * m_TurnSpeed * Time.unscaledDeltaTime, Space.Self);
    }

}
