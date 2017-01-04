using System;
using UnityEngine.InputNew;

namespace UnityEngine.Experimental.EditorVR.Tools
{
	public class SelectionTool : MonoBehaviour, ITool, IUsesRayOrigin, IUsesRaycastResults, ICustomActionMap, ISetHighlight, ISelectObject
	{
		GameObject m_HoverGameObject;
		GameObject m_PressedObject;

		public ActionMap actionMap { get { return m_ActionMap; } }
		[SerializeField]
		ActionMap m_ActionMap;

		public Func<Transform, GameObject> getFirstGameObject { private get; set; }
		public Transform rayOrigin { private get; set; }
		public Action<GameObject, bool> setHighlight { private get; set; }

		public Func<Transform, bool> isRayActive;
		public event Action<GameObject, Transform> hovered;

		public CanSelectObjectDelegate canSelectObject { private get; set; }
		public Func<GameObject, GameObject> getGroupRoot { private get; set; }
		public SelectObjectDelegate selectObject { private get; set; }

		public void ProcessInput(ActionMapInput input, Action<InputControl> consumeControl)
		{
			if (rayOrigin == null)
				return;

			if (!isRayActive(rayOrigin))
				return;

			var selectionInput = (SelectionInput)input;

			var hoveringObject = getFirstGameObject(rayOrigin);

			if (hovered != null)
				hovered(hoveringObject, rayOrigin);

			var newHoverObject = getGroupRoot(hoveringObject);
			if (newHoverObject)
				hoveringObject = newHoverObject;

			// Can't select this object
			if (!canSelectObject(hoveringObject, true))
				return;

			// Handle changing highlight
			if (newHoverObject != m_HoverGameObject)
			{
				if (m_HoverGameObject != null)
					setHighlight(m_HoverGameObject, false);

				if (newHoverObject != null)
					setHighlight(newHoverObject, true);
			}

			m_HoverGameObject = newHoverObject;

			// Capture object on press
			if (selectionInput.select.wasJustPressed)
			{
				m_PressedObject = hoveringObject;
				consumeControl(selectionInput.select);
			}

			// Select button on release
			if (selectionInput.select.wasJustReleased)
			{
				if (m_PressedObject == hoveringObject)
				{
					selectObject(m_PressedObject, rayOrigin, selectionInput.multiSelect.isHeld, true);

					if (m_PressedObject != null)
						setHighlight(m_PressedObject, false);

					if (selectionInput.multiSelect.isHeld)
						consumeControl(selectionInput.multiSelect);
				}

				if (m_PressedObject != null)
					consumeControl(selectionInput.select);

				m_PressedObject = null;
			}
		}

		void OnDisable()
		{
			if (m_HoverGameObject != null)
			{
				setHighlight(m_HoverGameObject, false);
				m_HoverGameObject = null;
			}
		}
	}
}
