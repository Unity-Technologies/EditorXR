#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	public sealed class SpatialHintModule : MonoBehaviour, IConnectInterfaces, IInstantiateUI, IRayToNode, IRayVisibilitySettings
	{
		[Flags]
		public enum SpatialHintStateFlags
		{
			Hidden = 1 << 0,
			PreDragReveal = 1 << 1,
			Scrolling = 1 << 2,
			CenteredScrolling = 2 << 3,
		}

		[SerializeField]
		SpatialHintUI m_SpatialHintUI;

		SpatialHintStateFlags m_State;
		Transform m_ControllingRayOrigin;

		public SpatialHintStateFlags state
		{
			get { return m_State; }
			set
			{
				m_State = value;
				switch (m_State)
				{
					case SpatialHintStateFlags.Hidden:
						m_SpatialHintUI.centeredScrolling = false;
						m_SpatialHintUI.preScrollArrowsVisible = false;
						m_SpatialHintUI.secondaryArrowsVisible = false;
						this.RemoveRayVisibilitySettings(m_ControllingRayOrigin, this);
						controllingRayOrigin = null;
						break;
					case SpatialHintStateFlags.PreDragReveal:
						m_SpatialHintUI.centeredScrolling = false;
						m_SpatialHintUI.preScrollArrowsVisible = true;
						m_SpatialHintUI.secondaryArrowsVisible = true;
						break;
					case SpatialHintStateFlags.Scrolling:
						m_SpatialHintUI.centeredScrolling = false;
						m_SpatialHintUI.preScrollArrowsVisible = false;
						m_SpatialHintUI.scrollVisualsVisible = true;
						break;
					case SpatialHintStateFlags.CenteredScrolling:
						m_SpatialHintUI.centeredScrolling = true;
						m_SpatialHintUI.preScrollArrowsVisible = false;
						m_SpatialHintUI.scrollVisualsVisible = true;
						break;

				}
			}
		}

		Transform controllingRayOrigin
		{
			set
			{
				if (value == m_ControllingRayOrigin)
					return;

				m_ControllingRayOrigin = value;
				if (m_ControllingRayOrigin == null)
				{
					m_SpatialHintUI.controllingNode = null;

				}
				else
				{
					state = SpatialHintStateFlags.PreDragReveal;
					m_SpatialHintUI.controllingNode = this.RequestNodeFromRayOrigin(m_ControllingRayOrigin);
				}
			}
		}

		Vector3 spatialHintScrollVisualsRotation { set { m_SpatialHintUI.scrollVisualsRotation = value; } }

		Transform spatialHintContentContainer { get { return m_SpatialHintUI.contentContainer; } }

		void Awake()
		{
			m_SpatialHintUI = this.InstantiateUI(m_SpatialHintUI.gameObject).GetComponent<SpatialHintUI>();
			this.ConnectInterfaces(m_SpatialHintUI);
		}

		internal void PulseScrollArrows()
		{
			m_SpatialHintUI.PulseScrollArrows();
		}

		internal void SetState(SpatialHintStateFlags newState)
		{
			state = newState;
		}

		internal void SetPosition(Vector3 newPosition)
		{
			spatialHintContentContainer.position = newPosition;
		}

		internal void SetContainerRotation(Quaternion newRotation)
		{
			m_SpatialHintUI.transform.rotation = newRotation;
		}

		internal void SetShowHideRotationTarget(Vector3 target)
		{
			spatialHintScrollVisualsRotation = target;
		}

		internal void LookAt(Vector3 position)
		{
			var orig = spatialHintContentContainer.rotation;
			spatialHintContentContainer.LookAt(position);
			spatialHintContentContainer.rotation = orig;
		}

		internal void SetDragThresholdTriggerPosition (Vector3 position)
		{
			if (state == SpatialHintStateFlags.Hidden || position == m_SpatialHintUI.scrollVisualsDragThresholdTriggerPosition)
				return;

			m_SpatialHintUI.scrollVisualsDragThresholdTriggerPosition = position;
		}

		internal void SetSpatialHintControlObject(Transform controlObject)
		{
			controllingRayOrigin = controlObject;
			this.AddRayVisibilitySettings(m_ControllingRayOrigin, this, false, false);
		}
	}
}
#endif
