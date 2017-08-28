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
		SpatialHintModuleUI m_SpatialHintModuleUI;

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
						m_SpatialHintModuleUI.centeredScrolling = false;
						m_SpatialHintModuleUI.preScrollArrowsVisible = false;
						m_SpatialHintModuleUI.secondaryArrowsVisible = false;
						this.RemoveRayVisibilitySettings(m_ControllingRayOrigin, this);
						controllingRayOrigin = null;
						break;
					case SpatialHintStateFlags.PreDragReveal:
						m_SpatialHintModuleUI.centeredScrolling = false;
						m_SpatialHintModuleUI.preScrollArrowsVisible = true;
						m_SpatialHintModuleUI.secondaryArrowsVisible = true;
						break;
					case SpatialHintStateFlags.Scrolling:
						m_SpatialHintModuleUI.centeredScrolling = false;
						m_SpatialHintModuleUI.preScrollArrowsVisible = false;
						m_SpatialHintModuleUI.scrollVisualsVisible = true;
						break;
					case SpatialHintStateFlags.CenteredScrolling:
						m_SpatialHintModuleUI.centeredScrolling = true;
						m_SpatialHintModuleUI.preScrollArrowsVisible = false;
						m_SpatialHintModuleUI.scrollVisualsVisible = true;
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
					m_SpatialHintModuleUI.controllingNode = null;

				}
				else
				{
					state = SpatialHintStateFlags.PreDragReveal;
					m_SpatialHintModuleUI.controllingNode = this.RequestNodeFromRayOrigin(m_ControllingRayOrigin);
				}
			}
		}

		Vector3 spatialHintScrollVisualsRotation { set { m_SpatialHintModuleUI.scrollVisualsRotation = value; } }

		Transform spatialHintContentContainer { get { return m_SpatialHintModuleUI.contentContainer; } }

		void Awake()
		{
			m_SpatialHintModuleUI = this.InstantiateUI(m_SpatialHintModuleUI.gameObject).GetComponent<SpatialHintModuleUI>();
			this.ConnectInterfaces(m_SpatialHintModuleUI);
		}

		internal void PulseScrollArrows()
		{
			m_SpatialHintModuleUI.PulseScrollArrows();
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
			m_SpatialHintModuleUI.transform.rotation = newRotation;
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
			if (state == SpatialHintStateFlags.Hidden || position == m_SpatialHintModuleUI.scrollVisualsDragThresholdTriggerPosition)
				return;

			m_SpatialHintModuleUI.scrollVisualsDragThresholdTriggerPosition = position;
		}

		internal void SetSpatialHintControlObject(Transform controlObject)
		{
			controllingRayOrigin = controlObject;
			this.AddRayVisibilitySettings(m_ControllingRayOrigin, this, false, false);
		}
	}
}
#endif
