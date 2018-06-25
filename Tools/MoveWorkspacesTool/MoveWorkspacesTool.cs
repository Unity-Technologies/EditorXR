#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEngine;
using UnityEngine.InputNew;

[ExecuteInEditMode]
public class MoveWorkspacesTool : MonoBehaviour, ITool, IStandardActionMap, IUsesRayOrigin, IUsesViewerBody,
    IResetWorkspaces, IAllWorkspaces, IUsesViewerScale, IRayVisibilitySettings
{
    enum State
    {
        WaitingForInput,
        WaitingForReset,
        MoveWorkspaces
    }

    float m_TriggerPressedTimeStamp;

    List<IWorkspace> m_Workspaces;
    Quaternion[] m_WorkspaceLocalRotations;
    Vector3[] m_WorkspacePositions;
    Vector3[] m_WorkspaceLocalScales;
    Vector3[] m_ScaleVelocities;

    Vector3 m_RayOriginStartPosition;
    Quaternion m_RayOriginStartRotation;
    Vector3 m_RayOriginPreviousPosition;

    float m_ThrowingTimeStart;
    float m_TargetScale = 1.0f;

    State m_State = State.WaitingForInput;

    public Transform rayOrigin { private get; set; }
    public List<IWorkspace> allWorkspaces { private get; set; }
    public ActionMap standardActionMap { private get; set; }

    public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
    {
        var action = ((Standard)input).action;

        if (m_State != State.MoveWorkspaces)
        {
            if (!this.IsAboveHead(rayOrigin))
                return;

            if (action.wasJustPressed)
            {
                if (UIUtils.IsDoubleClick(Time.realtimeSinceStartup - m_TriggerPressedTimeStamp))
                {
                    this.ResetWorkspaceRotations();
                    m_State = State.WaitingForReset;
                }

                m_TriggerPressedTimeStamp = Time.realtimeSinceStartup;
                consumeControl(action);
            }
            else if (m_State == State.WaitingForInput && action.isHeld)
            {
                StartMove();
            }
            else if (m_State == State.WaitingForReset && action.wasJustReleased)
            {
                m_State = State.WaitingForInput;
            }
        }
        else
        {
            consumeControl(action);

            var throwDownTriggered = false;
            if (ThrowingDown() && action.wasJustReleased)
            {
                foreach (var ws in m_Workspaces)
                    ws.Close();

                throwDownTriggered = true;
            }

            UpdateWorkspaceScales();

            if (!throwDownTriggered && action.isHeld)
                MoveWorkspaces();

            if (action.wasJustReleased)
                EndMove();
        }
    }

    bool LatchWorkspaces()
    {
        m_Workspaces = new List<IWorkspace>(allWorkspaces);
        var workspaceCount = m_Workspaces.Count;
        m_WorkspaceLocalRotations = new Quaternion[workspaceCount];
        m_WorkspacePositions = new Vector3[workspaceCount];
        m_WorkspaceLocalScales = new Vector3[workspaceCount];
        m_ScaleVelocities = new Vector3[workspaceCount];

        for (int i = 0; i < allWorkspaces.Count; i++)
        {
            var workspaceTransform = allWorkspaces[i].transform;
            m_WorkspaceLocalRotations[i] = workspaceTransform.localRotation;
            m_WorkspacePositions[i] = workspaceTransform.position;
            m_WorkspaceLocalScales[i] = workspaceTransform.localScale;
        }

        return workspaceCount > 0;
    }

    bool ThrowingDown()
    {
        const float kThrowVelocityThreshold = 1.5f;
        const float kLocalScaleWhenReadyToThrow = 0.5f;
        const float kThrowDelayAllowed = 0.2f;

        var verticalVelocity = (m_RayOriginPreviousPosition.y - rayOrigin.position.y) / Time.deltaTime / this.GetViewerScale();
        m_RayOriginPreviousPosition = rayOrigin.position;

        if (verticalVelocity > kThrowVelocityThreshold)
        {
            m_TargetScale = kLocalScaleWhenReadyToThrow;
            m_ThrowingTimeStart = Time.realtimeSinceStartup;
            return true;
        }

        if (Time.realtimeSinceStartup - m_ThrowingTimeStart < kThrowDelayAllowed)
            return true;

        m_TargetScale = 1.0f;
        return false;
    }

    void UpdateWorkspaceScales()
    {
        for (int i = 0; i < m_Workspaces.Count; i++)
        {
            var workspaceTransform = m_Workspaces[i].transform;

            var targetScale = m_WorkspaceLocalScales[i] * m_TargetScale;

            workspaceTransform.localScale = MathUtilsExt.SmoothDamp(workspaceTransform.localScale, targetScale,
                ref m_ScaleVelocities[i], 0.25f, Mathf.Infinity, Time.deltaTime);
        }
    }

    void StartMove()
    {
        const float kEnterMovementModeTime = 0.5f;

        if (Time.realtimeSinceStartup - m_TriggerPressedTimeStamp > kEnterMovementModeTime)
        {
            if (LatchWorkspaces())
            {
                m_RayOriginStartPosition = rayOrigin.position;
                m_RayOriginStartRotation = rayOrigin.rotation;

                m_RayOriginPreviousPosition = rayOrigin.position;

                m_State = State.MoveWorkspaces;

                this.AddRayVisibilitySettings(rayOrigin, this, false, false);

                foreach (var ws in allWorkspaces)
                {
                    var workspace = ws as Workspace;
                    if (workspace)
                        workspace.SetUIHighlightsVisible(true);
                }
            }
        }
    }

    void MoveWorkspaces()
    {
        const float kMoveMultiplier = 2;

        for (int i = 0; i < allWorkspaces.Count; i++)
        {
            var workspaceTransform = allWorkspaces[i].transform;
            var deltaRotation = rayOrigin.rotation * Quaternion.Inverse(m_RayOriginStartRotation);
            var deltaPosition = rayOrigin.position - m_RayOriginStartPosition;
            var yawRotation = MathUtilsExt.ConstrainYawRotation(deltaRotation);
            var localOffset = m_WorkspacePositions[i] - m_RayOriginStartPosition;
            workspaceTransform.position = m_RayOriginStartPosition + deltaPosition * kMoveMultiplier + yawRotation * localOffset;
        }

        // Adjust look direction
        this.ResetWorkspaceRotations();
    }

    void EndMove()
    {
        m_State = State.WaitingForInput;

        this.RemoveRayVisibilitySettings(rayOrigin, this);

        foreach (var ws in allWorkspaces)
        {
            var workspace = ws as Workspace;
            if (workspace)
                workspace.SetUIHighlightsVisible(false);
        }
    }
}
#endif
