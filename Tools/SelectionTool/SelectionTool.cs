#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	sealed class SelectionTool : MonoBehaviour, ITool, IUsesRayOrigin, IUsesRaycastResults, ICustomActionMap,
		ISetHighlight, ISelectObject, ISetManipulatorsVisible, IIsHoveringOverUI, IUsesDirectSelection
	{
		GameObject m_HoverGameObject;
		GameObject m_PressedObject;

		public ActionMap actionMap { get { return m_ActionMap; } }
		[SerializeField]
		ActionMap m_ActionMap;

		public Transform rayOrigin { private get; set; }

		public Func<Transform, bool> isRayActive;
		public event Action<GameObject, Transform> hovered;

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			if (rayOrigin == null)
				return;

			if (this.IsHoveringOverUI(rayOrigin))
			{
				DeactivateHover();
				return;
			}

			if (!isRayActive(rayOrigin))
			{
				DeactivateHover();
				return;
			}

			var selectionInput = (SelectionInput)input;

			var hoveredObject = this.GetFirstGameObject(rayOrigin);

			var directSelection = this.GetDirectSelection();
			DirectSelectionData directSelectionData;
			if (directSelection.TryGetValue(rayOrigin, out directSelectionData))
			{
				if (directSelectionData.gameObject)
					hoveredObject = directSelectionData.gameObject;
			}

			if (hovered != null)
				hovered(hoveredObject, rayOrigin);

			var selectionCandidate = this.GetSelectionCandidate(hoveredObject, true);

			// Can't select this object (it might be locked or static)
			if (hoveredObject && !selectionCandidate)
				return;

			if (selectionCandidate)
				hoveredObject = selectionCandidate;

			// Handle changing highlight
			if (hoveredObject != m_HoverGameObject)
			{
				DeactivateHover();

				if (hoveredObject != null)
					this.SetHighlight(hoveredObject, true, rayOrigin);
			}

			m_HoverGameObject = hoveredObject;

			this.SetManipulatorsVisible(this, !selectionInput.multiSelect.isHeld);

			// Capture object on press
			if (selectionInput.select.wasJustPressed)
			{
				m_PressedObject = hoveredObject;

				if (m_PressedObject)
					consumeControl(selectionInput.select);
			}

			// Select button on release
			if (selectionInput.select.wasJustReleased)
			{
				if (m_PressedObject == hoveredObject)
				{
					this.SelectObject(m_PressedObject, rayOrigin, selectionInput.multiSelect.isHeld, true);

					if (m_PressedObject != null)
						this.SetHighlight(m_PressedObject, false, rayOrigin);

					if (selectionInput.multiSelect.isHeld)
						consumeControl(selectionInput.multiSelect);
				}

				if (m_PressedObject)
					consumeControl(selectionInput.select);

				m_PressedObject = null;
			}
		}

		void DeactivateHover()
		{
			if (m_HoverGameObject != null)
				this.SetHighlight(m_HoverGameObject, false, rayOrigin);
			m_HoverGameObject = null;
		}

		void OnDisable()
		{
			if (m_HoverGameObject != null)
			{
				this.SetHighlight(m_HoverGameObject, false, rayOrigin);
				m_HoverGameObject = null;
			}
		}
	}
}
#endif
