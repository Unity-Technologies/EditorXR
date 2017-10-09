using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEngine;
using UnityEngine.InputNew;

[ProcessInput(1)]
[RequireComponent(typeof(ProxyHelper))]
public class ProxyAnimator : MonoBehaviour, ICustomActionMap
{
	public class TransformInfo
	{
		public Vector3 initialPosition;
		public Vector3 initialRotation;
		public Vector3 positionOffset;
		public Vector3 rotationOffset;

		public void ResetOffsets()
		{
			positionOffset = Vector3.zero;
			rotationOffset = Vector3.zero;
		}

		public void ResetPositionOffset()
		{
			positionOffset = Vector3.zero;
		}

		public void ResetRotationOffset()
		{
			rotationOffset = Vector3.zero;
		}

		public void Apply(Transform transform)
		{
			transform.localPosition = initialPosition + positionOffset;
			transform.localRotation = Quaternion.Euler(initialRotation + rotationOffset);
		}
	}

	[SerializeField]
	ActionMap m_ProxyActionMap;

	AffordanceObject[] m_Affordances;
	InputControl[] m_Controls;

	readonly Dictionary<Transform, TransformInfo> m_TransformInfos = new Dictionary<Transform, TransformInfo>();

	public ActionMap actionMap { get { return m_ProxyActionMap; } }
	public bool ignoreLocking { get { return true; } }
	internal event Action<AffordanceObject[], Dictionary<Transform, TransformInfo>, ActionMapInput> postAnimate;

	void Start()
	{
		m_Affordances = GetComponent<ProxyHelper>().affordances;
	}

	public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
	{
		if (m_Affordances == null)
			return;

		var length = m_Affordances.Length;
		if (m_Controls == null)
		{
			m_Controls = new InputControl[length];

			var bindings = input.actionMap.controlSchemes[0].bindings;
			for (var i = 0; i < input.controlCount; i++)
			{
				var control = input[i];
				var binding = bindings[i];
				for (var j = 0; j < length; j++)
				{
					var button = m_Affordances[j];
					foreach (var index in binding.sources)
					{
						if (index.controlIndex == (int)button.control)
						{
							m_Controls[j] = control;
							break;
						}
					}
				}
			}

			foreach (var button in m_Affordances)
			{
				var buttonTransform = button.transform;
				TransformInfo info;
				if (!m_TransformInfos.TryGetValue(buttonTransform, out info))
				{
					info = new TransformInfo();
					m_TransformInfos[buttonTransform] = info;
				}

				info.initialPosition = buttonTransform.localPosition;
				info.initialRotation = buttonTransform.localRotation.eulerAngles;

			}
		}

		foreach (var kvp in m_TransformInfos)
		{
			kvp.Value.ResetOffsets();
		}

		for (var i = 0; i < length; i++)
		{
			var button = m_Affordances[i];
			var control = m_Controls[i];
			var info = m_TransformInfos[button.transform];

			//Assume control values are [-1, 1]
			var min = button.min;
			var offset = min + (control.rawValue + 1) * (button.max - min) * 0.5f;

			var positionOffset = info.positionOffset;
			var translateAxes = button.translateAxes;
			if ((translateAxes & AxisFlags.X) != 0)
				positionOffset.x += offset;

			if ((translateAxes & AxisFlags.Y) != 0)
				positionOffset.y += offset;

			if ((translateAxes & AxisFlags.Z) != 0)
				positionOffset.z += offset;

			info.positionOffset = positionOffset;

			var localRotation = info.rotationOffset;
			var rotateAxes = button.rotateAxes;
			if ((rotateAxes & AxisFlags.X) != 0)
				localRotation.x += offset;

			if ((rotateAxes & AxisFlags.Y) != 0)
				localRotation.y += offset;

			if ((rotateAxes & AxisFlags.Z) != 0)
				localRotation.z += offset;

			info.rotationOffset = localRotation;
		}

		foreach (var kvp in m_TransformInfos)
		{
			kvp.Value.Apply(kvp.Key);
		}

		if (postAnimate != null)
			postAnimate(m_Affordances, m_TransformInfos, input);
	}
}
