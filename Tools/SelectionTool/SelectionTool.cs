#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	sealed class SelectionTool : MonoBehaviour, ITool, IUsesRayOrigin, IUsesRaycastResults, ICustomActionMap,
		ISetHighlight, ISelectObject, ISetManipulatorsVisible, IIsHoveringOverUI, IUsesDirectSelection, ILinkedObject,
		ICanGrabObject, IUsesNode, IIsRayActive, IIsMainMenuVisible, IIsInMiniWorld
	{
		[SerializeField]
		ActionMap m_ActionMap;

		GameObject m_PressedObject;

		SelectionInput m_SelectionInput;

		readonly Dictionary<Transform, GameObject> m_HoverGameObjects = new Dictionary<Transform, GameObject>();

		readonly Dictionary<Transform, GameObject> m_SelectionHoverGameObjects = new Dictionary<Transform, GameObject>();

		public ActionMap actionMap { get { return m_ActionMap; } }

		public Transform rayOrigin { private get; set; }
		public Node? node { private get; set; }

		public event Action<GameObject, Transform> hovered;

		public List<ILinkedObject> linkedObjects { get; set; }

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			m_SelectionInput = (SelectionInput)input;

			var multiSelect = false;
			foreach (var linkedObject in linkedObjects)
			{
				var selectionTool = (SelectionTool)linkedObject;
				var toolInput = selectionTool.m_SelectionInput;
				if (toolInput != null && toolInput.multiSelect.isHeld)
					multiSelect = true;
			}

			if (this.IsSharedUpdater(this))
			{
				this.SetManipulatorsVisible(this, !multiSelect);

				var directSelection = this.GetDirectSelection();

				m_SelectionHoverGameObjects.Clear();
				foreach (var linkedObject in linkedObjects)
				{
					var selectionTool = (SelectionTool)linkedObject;
					var selectionRayOrigin = selectionTool.rayOrigin;

					if (!selectionTool.IsRayActive())
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
					var directSelectionData = kvp.Value;
					var directHoveredObject = directSelectionData.gameObject;

					var directSelectionCandidate = this.GetSelectionCandidate(directHoveredObject, true);

					// Can't select this object (it might be locked or static)
					if (directHoveredObject && !directSelectionCandidate)
						continue;

					if (directSelectionCandidate)
						directHoveredObject = directSelectionCandidate;

					if (!this.CanGrabObject(directHoveredObject, directRayOrigin))
						continue;

					var grabbingNode = directSelectionData.node;
					var selectionTool = linkedObjects.Cast<SelectionTool>().FirstOrDefault(linkedObject => linkedObject.node == grabbingNode);
					if (selectionTool == null)
						continue;

					if (!selectionTool.IsDirectActive())
						continue;

					// Only overwrite an existing selection if it does not contain the hovered object
					// In the case of multi-select, only add, do not remove
					if (selectionTool.m_SelectionInput.select.wasJustPressed && !Selection.objects.Contains(directHoveredObject))
						this.SelectObject(directHoveredObject, directRayOrigin, multiSelect);

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

			// Capture object on press
			if (m_SelectionInput.select.wasJustPressed)
				m_PressedObject = hoveredObject;

			// Select button on release
			if (m_SelectionInput.select.wasJustReleased)
			{
				if (m_PressedObject == hoveredObject)
				{
					this.SelectObject(m_PressedObject, rayOrigin, multiSelect, true);

					if (m_PressedObject != null)
						this.SetHighlight(m_PressedObject, false, rayOrigin);
				}

				if (m_PressedObject)
					consumeControl(m_SelectionInput.select);

				m_PressedObject = null;
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

			if (!this.IsRayActive(rayOrigin))
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
	}
}
#endif
