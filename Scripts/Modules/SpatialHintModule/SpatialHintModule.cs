#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class SpatialHintModule : MonoBehaviour, IConnectInterfaces, IInstantiateUI
	{
		[SerializeField]
		SpatialHintModuleUI m_SpatialHintModuleUI;

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
		public bool spatialHintPreScrollVisualsVisible
		{
			get { return m_SpatialHintModuleUI.preScrollVisualsVisible; }
			set { m_SpatialHintModuleUI.preScrollVisualsVisible = value; }
		}

		/// <summary>
		/// Description
		/// </summary>
		public bool spatialHintPrimaryArrowsVisible
		{
			get { return m_SpatialHintModuleUI.primaryArrowsVisible; }
			set { m_SpatialHintModuleUI.primaryArrowsVisible = value; }
		}

		/// <summary>
		/// Description
		/// </summary>
		public bool spatialHintSecondaryArrowsVisible
		{
			get { return m_SpatialHintModuleUI.secondaryArrowsVisible; }
			set { m_SpatialHintModuleUI.secondaryArrowsVisible = value; }
		}

		/// <summary>
		/// Description
		/// </summary>
		public Vector3 spatialHintScrollVisualsRotation
		{
			get { return m_SpatialHintModuleUI.scrollVisualsRotation; }
			set { m_SpatialHintModuleUI.scrollVisualsRotation = value; }
		}

		/// <summary>
		/// Description
		/// </summary>
		public Vector3 spatialHintScrollVisualsDragThresholdTriggerPosition
		{
			get { return m_SpatialHintModuleUI.scrollVisualsDragThresholdTriggerPosition; }
			set { m_SpatialHintModuleUI.scrollVisualsDragThresholdTriggerPosition = value; }
		}

		/// <summary>
		/// Description
		/// </summary>
		public Transform spatialHintContentContainer { get { return m_SpatialHintModuleUI.contentContainer; } }

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
			Debug.LogError("<color=green>Pulse scroll arrows called !!!!!!!!!! <-------------------------------------------------</color>");
			m_SpatialHintModuleUI.PulseScrollArrows();
		}
	}
}
#endif
