#if UNITY_EDITOR && UNITY_EDITORVR
using System;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	sealed class SelectionTool : MonoBehaviour, ITool, IUsesRayOrigin, IUsesRaycastResults, ICustomActionMap,
		ISetHighlight, ISelectObject, ISetManipulatorsVisible, IIsHoveringOverUI
	{
		GameObject m_HoverGameObject;
		GameObject m_PressedObject;

		public ActionMap actionMap { get { return m_ActionMap; } }
		[SerializeField]
		ActionMap m_ActionMap;

		public Func<Transform, GameObject> getFirstGameObject { private get; set; }
		public Transform rayOrigin { private get; set; }
		public Action<GameObject, Transform, bool> setHighlight { private get; set; }

		public Func<Transform, bool> isRayActive;
		public event Action<GameObject, Transform> hovered;

		public GetSelectionCandidateDelegate getSelectionCandidate { private get; set; }
		public SelectObjectDelegate selectObject { private get; set; }

		public Action<ISetManipulatorsVisible, bool> setManipulatorsVisible { private get; set; }

		public Func<Transform, bool> isHoveringOverUI { private get; set; }

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			if (rayOrigin == null)
				return;

			if (isHoveringOverUI(rayOrigin))
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

			var hoveredObject = getFirstGameObject(rayOrigin);

			if (hovered != null)
				hovered(hoveredObject, rayOrigin);

			var selectionCandidate = getSelectionCandidate(hoveredObject, true);

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
					setHighlight(hoveredObject, rayOrigin, true);
			}

			m_HoverGameObject = hoveredObject;

			setManipulatorsVisible(this, !selectionInput.multiSelect.isHeld);

			// Capture object on press
			if (selectionInput.select.wasJustPressed)
			{
				m_PressedObject = hoveredObject;
				consumeControl(selectionInput.select);
			}

			// Select button on release
			if (selectionInput.select.wasJustReleased)
			{
				if (m_PressedObject == hoveredObject)
				{
					selectObject(m_PressedObject, rayOrigin, selectionInput.multiSelect.isHeld, true);

					if (m_PressedObject != null)
						setHighlight(m_PressedObject, rayOrigin, false);

					if (selectionInput.multiSelect.isHeld)
						consumeControl(selectionInput.multiSelect);
				}

				if (m_PressedObject != null)
					consumeControl(selectionInput.select);

				m_PressedObject = null;
			}
		}

		void DeactivateHover()
		{
			if (m_HoverGameObject != null)
				setHighlight(m_HoverGameObject, rayOrigin, false);
			m_HoverGameObject = null;
		}

		void OnDisable()
		{
			if (m_HoverGameObject != null)
			{
				setHighlight(m_HoverGameObject, rayOrigin, false);
				m_HoverGameObject = null;
			}
		}
	}
}
#endif
