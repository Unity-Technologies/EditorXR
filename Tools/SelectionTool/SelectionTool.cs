#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	sealed class SelectionTool : MonoBehaviour, ITool, IUsesRayOrigin, IUsesRaycastResults, ICustomActionMap,
		ISetHighlight, ISelectObject, ISetManipulatorsVisible, IIsHoveringOverUI, IUsesDirectSelection, ILinkedObject,
		ICanGrabObject, IGetManipulatorDragState, IUsesNode, IGetRayVisibility, IIsMainMenuVisible, IIsInMiniWorld,
		IRayToNode, IGetDefaultRayColor, ISetDefaultRayColor, ITooltip, ITooltipPlacement, ISetTooltipVisibility,
		IUsesProxyType, IMenuIcon, IGetPointerLength, IRayVisibilitySettings, IUsesViewerScale, ICheckBounds
	{
		const float k_MultiselectHueShift = 0.5f;
		const float k_BLockSelectDragThreshold = 0.01f;
		static readonly Vector3 k_TooltipPosition = new Vector3(0, 0.05f, -0.03f);
		static readonly Quaternion k_TooltipRotation = Quaternion.AngleAxis(90, Vector3.right);

		[SerializeField]
		Sprite m_Icon;

		[SerializeField]
		ActionMap m_ActionMap;

		[SerializeField]
		GameObject m_BlockSelectCube;

		GameObject m_PressedObject;

		SelectionInput m_SelectionInput;

		float m_LastMultiSelectClickTime;
		Color m_NormalRayColor;
		Color m_MultiselectRayColor;
		bool m_MultiSelect;
		bool m_BlockSelect;
		Vector3 m_SelectStartPosition;
		Renderer m_BlockSelectCubeRenderer;

		readonly Dictionary<Transform, GameObject> m_HoverGameObjects = new Dictionary<Transform, GameObject>();
		readonly Dictionary<Transform, GameObject> m_SelectionHoverGameObjects = new Dictionary<Transform, GameObject>();
		readonly List<GameObject> m_BlockSelectHoverGameObjects = new List<GameObject>();

		public ActionMap actionMap { get { return m_ActionMap; } }

		public Transform rayOrigin { private get; set; }
		public Node? node { private get; set; }

		public Sprite icon { get { return m_Icon; } }

		public event Action<GameObject, Transform> hovered;

		public List<ILinkedObject> linkedObjects { get; set; }
		public Type proxyType { get; set; }

		public string tooltipText { get { return m_MultiSelect ? "Multi-Select Enabled" : ""; } }
		public Transform tooltipTarget { get; private set; }
		public Transform tooltipSource { get { return rayOrigin; } }
		public TextAlignment tooltipAlignment { get { return TextAlignment.Center; } }

		void Start()
		{
			m_NormalRayColor = this.GetDefaultRayColor(rayOrigin);
			m_MultiselectRayColor = m_NormalRayColor;
			m_MultiselectRayColor = MaterialUtils.HueShift(m_MultiselectRayColor, k_MultiselectHueShift);

			tooltipTarget = ObjectUtils.CreateEmptyGameObject("SelectionTool Tooltip Target", rayOrigin).transform;
			tooltipTarget.localPosition = k_TooltipPosition;
			tooltipTarget.localRotation = k_TooltipRotation;

			m_BlockSelectCube = ObjectUtils.Instantiate(m_BlockSelectCube, transform);
			m_BlockSelectCube.SetActive(false);
			m_BlockSelectCubeRenderer = m_BlockSelectCube.GetComponent<Renderer>();
		}

		void OnDestroy()
		{
			ObjectUtils.Destroy(m_BlockSelectCube);
		}

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			if (this.GetManipulatorDragState())
				return;

			m_SelectionInput = (SelectionInput)input;

			var multiSelectControl = m_SelectionInput.multiSelect;
			if (proxyType == typeof(ViveProxy))
				multiSelectControl = m_SelectionInput.multiSelectAlt;

			if (multiSelectControl.wasJustPressed)
			{
				var realTime = Time.realtimeSinceStartup;
				if (UIUtils.IsDoubleClick(realTime - m_LastMultiSelectClickTime))
				{
					foreach (var linkedObject in linkedObjects)
					{
						var selectionTool = (SelectionTool)linkedObject;
						selectionTool.m_MultiSelect = !selectionTool.m_MultiSelect;
						this.HideTooltip(selectionTool);
					}

					if (m_MultiSelect)
						this.ShowTooltip(this);

					consumeControl(multiSelectControl);
				}

				m_LastMultiSelectClickTime = realTime;
			}

			this.SetDefaultRayColor(rayOrigin, m_MultiSelect ? m_MultiselectRayColor : m_NormalRayColor);

			if (this.IsSharedUpdater(this))
			{
				this.SetManipulatorsVisible(this, !m_MultiSelect);

				var directSelection = this.GetDirectSelection();

				m_SelectionHoverGameObjects.Clear();
				foreach (var linkedObject in linkedObjects)
				{
					var selectionTool = (SelectionTool)linkedObject;
					if (selectionTool.m_BlockSelect)
						continue;

					if (!selectionTool.IsRayActive())
						continue;

					var selectionRayOrigin = selectionTool.rayOrigin;
					if (!this.IsRayVisible(selectionRayOrigin))
						continue;

					var hover = this.GetFirstGameObject(selectionRayOrigin);

					if (!selectionTool.GetSelectionCandidate(ref hover))
						continue;

					if (hover)
					{
						GameObject lastHover;
						if (m_HoverGameObjects.TryGetValue(selectionRayOrigin, out lastHover) && lastHover != hover)
							this.SetHighlight(lastHover, false, selectionRayOrigin);

						m_SelectionHoverGameObjects[selectionRayOrigin] = hover;
						m_HoverGameObjects[selectionRayOrigin] = hover;
					}
				}

				// Unset highlight old hovers
				var hovers = new Dictionary<Transform, GameObject>(m_HoverGameObjects);
				foreach (var kvp in hovers)
				{
					var directRayOrigin = kvp.Key;
					var hover = kvp.Value;

					if (!directSelection.ContainsKey(directRayOrigin)
						&& !m_SelectionHoverGameObjects.ContainsKey(directRayOrigin))
					{
						this.SetHighlight(hover, false, directRayOrigin);
						m_HoverGameObjects.Remove(directRayOrigin);
					}
				}

				// Find new hovers
				foreach (var kvp in directSelection)
				{
					var directRayOrigin = kvp.Key;
					var directHoveredObject = kvp.Value;

					var directSelectionCandidate = this.GetSelectionCandidate(directHoveredObject, true);

					// Can't select this object (it might be locked or static)
					if (directHoveredObject && !directSelectionCandidate)
						continue;

					if (directSelectionCandidate)
						directHoveredObject = directSelectionCandidate;

					if (!this.CanGrabObject(directHoveredObject, directRayOrigin))
						continue;

					var grabbingNode = this.RequestNodeFromRayOrigin(directRayOrigin);
					var selectionTool = linkedObjects.Cast<SelectionTool>().FirstOrDefault(linkedObject => linkedObject.node == grabbingNode);
					if (selectionTool == null)
						continue;

					if (selectionTool.m_BlockSelect)
						continue;

					if (!selectionTool.IsDirectActive())
					{
						m_HoverGameObjects.Remove(directRayOrigin);
						this.SetHighlight(directHoveredObject, false, directRayOrigin);
						continue;
					}

					// Only overwrite an existing selection if it does not contain the hovered object
					// In the case of multi-select, only add, do not remove
					if (selectionTool.m_SelectionInput.select.wasJustPressed && !Selection.objects.Contains(directHoveredObject))
						this.SelectObject(directHoveredObject, directRayOrigin, m_MultiSelect);

					GameObject lastHover;
					if (m_HoverGameObjects.TryGetValue(directRayOrigin, out lastHover) && lastHover != directHoveredObject)
						this.SetHighlight(lastHover, false, directRayOrigin);

					m_HoverGameObjects[directRayOrigin] = directHoveredObject;
				}

				// Set highlight on new hovers
				foreach (var hover in m_HoverGameObjects)
				{
					this.SetHighlight(hover.Value, true, hover.Key);
				}
			}

			if (!IsRayActive())
				return;

			// Need to call GetFirstGameObject a second time because we do not guarantee shared updater executes first
			var hoveredObject = this.GetFirstGameObject(rayOrigin);

			if (hovered != null)
				hovered(hoveredObject, rayOrigin);

			if (!GetSelectionCandidate(ref hoveredObject))
				return;

			var pointerPosition = this.GetPointerPosition(rayOrigin);

			// Capture object on press
			var select = m_SelectionInput.select;
			if (select.wasJustPressed)
			{
				m_SelectStartPosition = pointerPosition;

				// Ray selection only if ray is visible
				if (this.IsRayVisible(rayOrigin))
					m_PressedObject = hoveredObject;
			}

			if (select.isHeld)
			{
				var startToEnd = pointerPosition - m_SelectStartPosition;
				if (!m_BlockSelect && startToEnd.magnitude > k_BLockSelectDragThreshold * this.GetViewerScale())
				{
					m_BlockSelect = true;
					m_BlockSelectCube.SetActive(true);
					m_PressedObject = null;
					this.AddRayVisibilitySettings(rayOrigin, this, false, true);
				}

				if (m_BlockSelect)
					this.SetManipulatorsVisible(this, false);

				m_BlockSelectCube.transform.localScale = startToEnd;
				m_BlockSelectCube.transform.position = m_SelectStartPosition + startToEnd * 0.5f;

				foreach (var hover in m_BlockSelectHoverGameObjects)
				{
					this.SetHighlight(hover, false, rayOrigin);
				}

				m_BlockSelectHoverGameObjects.Clear();
				this.CheckBounds(m_BlockSelectCubeRenderer.bounds, m_BlockSelectHoverGameObjects);

				foreach (var hover in m_BlockSelectHoverGameObjects)
				{
					this.SetHighlight(hover, true, rayOrigin);
				}
			}

			// Select button on release
			if (select.wasJustReleased)
			{
				if (m_BlockSelect)
				{
					if (!m_MultiSelect)
					{
						this.SetManipulatorsVisible(this, true);
						Selection.activeGameObject = null;
					}

					foreach (var hover in m_BlockSelectHoverGameObjects)
					{
						this.SelectObject(hover, rayOrigin, true);

						if (hover != null)
							this.SetHighlight(hover, false, rayOrigin);
					}

					this.ResetDirectSelectionState();
				}
				else
				{
					if (m_PressedObject == hoveredObject)
					{
						this.SelectObject(m_PressedObject, rayOrigin, m_MultiSelect, true);
						this.ResetDirectSelectionState();

						if (m_PressedObject != null)
							this.SetHighlight(m_PressedObject, false, rayOrigin);
					}

					if (m_PressedObject)
						consumeControl(select);
				}

				this.RemoveRayVisibilitySettings(rayOrigin, this);
				m_BlockSelectCube.SetActive(false);
				m_PressedObject = null;
				m_BlockSelect = false;
			}
		}

		bool GetSelectionCandidate(ref GameObject hoveredObject)
		{
			var selectionCandidate = this.GetSelectionCandidate(hoveredObject, true);

			// Can't select this object (it might be locked or static)
			if (hoveredObject && !selectionCandidate)
				return false;

			if (selectionCandidate)
				hoveredObject = selectionCandidate;

			return true;
		}

		bool IsDirectActive()
		{
			if (rayOrigin == null)
				return false;

			if (!this.IsConeVisible(rayOrigin))
				return false;

			if (this.IsInMiniWorld(rayOrigin))
				return true;

			if (this.IsMainMenuVisible(rayOrigin))
				return false;

			return true;
		}

		bool IsRayActive()
		{
			if (rayOrigin == null)
				return false;

			if (this.IsHoveringOverUI(rayOrigin))
				return false;

			if (this.IsMainMenuVisible(rayOrigin))
				return false;

			if (this.IsInMiniWorld(rayOrigin))
				return false;

			return true;
		}

		void OnDisable()
		{
			foreach (var kvp in m_HoverGameObjects)
			{
				this.SetHighlight(kvp.Value, false, kvp.Key);
			}
			m_HoverGameObjects.Clear();
		}

		public void OnResetDirectSelectionState() { }
	}
}
#endif
