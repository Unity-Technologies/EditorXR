#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	public sealed class SpatialHintModule : MonoBehaviour, IConnectInterfaces, IInstantiateUI, IRayToNode
	{
		[Flags]
		public enum SpatialHintStateFlags
		{
			Hidden = 1 << 0,
			PreDragReveal = 1 << 1,
			Scrolling = 1 << 2,
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
				//if (m_State == value)
					//return;

				m_State = value;
				switch (m_State)
				{
					case SpatialHintStateFlags.Hidden:
						Debug.LogError("<color=orange>SpatialHintState : </color>Hidden");
						//m_SpatialHintModuleUI.preScrollVisualsVisible = false;
						m_SpatialHintModuleUI.preScrollArrowsVisible = false;
						m_SpatialHintModuleUI.secondaryArrowsVisible = false;
						controllingRayOrigin = null;
						//spatialHintPrimaryArrowsVisible = false;
						//spatialHintSecondaryArrowsVisible = false;
						break;
					case SpatialHintStateFlags.PreDragReveal:
						Debug.LogError("<color=orange>SpatialHintState : </color>Pre drag reveal state");
						//m_SpatialHintModuleUI.preScrollVisualsVisible = true;
						m_SpatialHintModuleUI.preScrollArrowsVisible = true;
						m_SpatialHintModuleUI.secondaryArrowsVisible = true;
						break;
					case SpatialHintStateFlags.Scrolling:
						//m_SpatialHintModuleUI.preScrollVisualsVisible = false;
						m_SpatialHintModuleUI.preScrollArrowsVisible = false;
						m_SpatialHintModuleUI.scrollVisualsVisible = true;
						Debug.LogError("<color=orange>SpatialHintState : </color>Scrolling");
						break;
				}
			}
		}

		private Transform controllingRayOrigin
		{
			get
			{
				return m_ControllingRayOrigin;
			}

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
					state = SpatialHintModule.SpatialHintStateFlags.PreDragReveal;
					m_SpatialHintModuleUI.controllingNode = this.RequestNodeFromRayOrigin(m_ControllingRayOrigin);
					//spatialHintScrollVisualsRotation = Vector3.zero;
				}
			}
		}

		/*
		/// <summary>
		/// Description
		/// </summary>
		public bool spatialHintVisualsVisible
		{
			get { return m_SpatialHintModuleUI.visible; }
			set { m_SpatialHintModuleUI.visible = value; }
		}

		/// <summary>
		/// Enables/disables the visual elements that should be shown when beginning to initiate a spatial selection action
		/// This is only enabled before the enabling of the main select visuals
		/// </summary>
		bool spatialHintPreScrollVisualsVisible
		{
			get { return m_SpatialHintModuleUI.preScrollVisualsVisible; }
			set { m_SpatialHintModuleUI.preScrollVisualsVisible = value; }
		}

		/// <summary>
		/// Description
		/// </summary>
		bool spatialHintPrimaryArrowsVisible
		{
			get { return m_SpatialHintModuleUI.primaryArrowsVisible; }
			set { m_SpatialHintModuleUI.primaryArrowsVisible = value; }
		}

		/// <summary>
		/// Description
		/// </summary>
		bool spatialHintSecondaryArrowsVisible
		{
			get { return m_SpatialHintModuleUI.secondaryArrowsVisible; }
			set { m_SpatialHintModuleUI.secondaryArrowsVisible = value; }
		}

		/// <summary>
		/// Description
		/// </summary>
		public Vector3 spatialHintScrollVisualsDragThresholdTriggerPosition
		{
			get { return m_SpatialHintModuleUI.scrollVisualsDragThresholdTriggerPosition; }
			set { m_SpatialHintModuleUI.scrollVisualsDragThresholdTriggerPosition = value; }
		}
		*/

		/// <summary>
		/// Description
		/// </summary>
		Vector3 spatialHintScrollVisualsRotation
		{
			get { return m_SpatialHintModuleUI.scrollVisualsRotation; }

			set
			{
				Debug.LogError("Spatial Rotation target being set to : " + value);
				//if (value == Vector3.zero)
					//state = SpatialHintStateFlags.Hidden; // Hide the non-spatial-scrolling visuals

				m_SpatialHintModuleUI.scrollVisualsRotation = value;
			}
		}

		/// <summary>
		/// Description
		/// </summary>
		Transform spatialHintContentContainer { get { return m_SpatialHintModuleUI.contentContainer; } }

		void Awake()
		{
			Debug.LogError("<color=green>PinnedToolsMenuUI initialized</color>");
			m_SpatialHintModuleUI = this.InstantiateUI(m_SpatialHintModuleUI.gameObject).GetComponent<SpatialHintModuleUI>();
			this.ConnectInterfaces(m_SpatialHintModuleUI);
		}

		/// <summary>
		/// Visually pulse the spatial-scroll arrows; the arrows shown when performing a spatiatil scroll
		/// </summary>
		public void PulseScrollArrows()
		{
			m_SpatialHintModuleUI.PulseScrollArrows();
		}

		public void SetState(SpatialHintStateFlags newState)
		{
			state = newState;
		}

		public void SetPosition(Vector3 newPosition)
		{
			spatialHintContentContainer.position = newPosition;
		}

		public void SetRotation(Quaternion newRotation)
		{
			m_SpatialHintModuleUI.transform.rotation = newRotation;
		}

		public void SetRotationTarget(Vector3 target)
		{
			spatialHintScrollVisualsRotation = target;
		}

		public void LookAt(Vector3 position)
		{
			var orig = spatialHintContentContainer.rotation;
			spatialHintContentContainer.LookAt(position);
			//this.SetSpatialHintLookAT(value.Value);
			//spatialHintContentContainer.LookAt(value.Value);
			//Debug.LogError(value.Value.ToString("F4"));
			//m_SpatialScrollOrientation = Quaternion.Euler(value.Value); // Quaternion.FromToRotation(m_HintContentContainer.forward, value.Value); // Quaternion.Euler(value.Value); Quaternion.RotateTowards(m_HintContentContainerInitialRotation, Quaternion.Euler(value.Value), 180f);
			//m_SpatialHintModuleUI.rotation (orig);
			spatialHintContentContainer.rotation = orig;
		}

		public void SetDragThresholdTriggerPosition (Vector3 position)
		{
			m_SpatialHintModuleUI.scrollVisualsDragThresholdTriggerPosition = position;
		}

		public void SetSpatialHintControlObject(Transform controlObject)
		{
			controllingRayOrigin = controlObject;
		}
	}
}
#endif
