using System.Linq;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEngine;
using UnityEngine.InputNew;

[RequireComponent(typeof(ProxyHelper))]
public class ProxyAnimator : MonoBehaviour, ICustomActionMap
{
	[SerializeField]
	ActionMap m_ProxyActionMap;

	ProxyHelper.ButtonObject[] m_Buttons;
	InputControl[] m_Controls;
	Vector3[] m_InitialPositions;
	Vector3[] m_InitialRotations;

	public ActionMap actionMap { get { return m_ProxyActionMap; } }

	void Start()
	{
		m_Buttons = GetComponent<ProxyHelper>().buttons;
	}

	public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
	{
		var length = m_Buttons.Length;
		if (m_Controls == null)
		{
			m_Controls = new InputControl[length];
			m_InitialPositions = new Vector3[length];
			m_InitialRotations = new Vector3[length];

			for (var i = 0; i < input.controlCount; i++)
			{
				var control = input[i];
				for (var j = 0; j < length; j++)
				{
					var button = m_Buttons[j];
					if (control.data.componentControlIndices.Contains((int)button.control))
					{
						m_Controls[j] = control;
						break;
					}
				}
			}

			for (var i = 0; i < length; i++)
			{
				var buttonTransform = m_Buttons[i].transform;
				m_InitialPositions[i] = buttonTransform.localPosition;
				m_InitialRotations[i] = buttonTransform.localRotation.eulerAngles;
			}
		}

		for (var i = 0; i < length; i++)
		{
			var button = m_Buttons[i];
			var control = m_Controls[i];
			//Assume control values are [-1, 1]
			var min = button.min;
			var offset = min + (control.rawValue + 1) * (button.max - min);

			var buttonTransform = button.transform;
			var localPosition = m_InitialPositions[i];
			var translateAxes = button.translateAxes;
			if ((translateAxes | AxisFlags.X) != 0)
				localPosition.x += offset;

			if ((translateAxes | AxisFlags.Y) != 0)
				localPosition.y += offset;

			if ((translateAxes | AxisFlags.Z) != 0)
				localPosition.z += offset;

			var localRotation = m_InitialRotations[i];
			var rotateAxes = button.rotateAxes;
			if ((rotateAxes | AxisFlags.X) != 0)
				localRotation.x += offset;

			if ((rotateAxes | AxisFlags.Y) != 0)
				localRotation.y += offset;

			if ((rotateAxes | AxisFlags.Z) != 0)
				localRotation.z += offset;

			buttonTransform.localPosition = localPosition;
			buttonTransform.localRotation = Quaternion.Euler(localRotation);
		}
	}
}
