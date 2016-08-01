using UnityEngine;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class JoystickLocomotion : ActionMapInput {
		public JoystickLocomotion (ActionMap actionMap) : base (actionMap) { }
		
		public AxisInputControl @moveForward { get { return (AxisInputControl)this[0]; } }
		public AxisInputControl @moveRight { get { return (AxisInputControl)this[1]; } }
		public AxisInputControl @yaw { get { return (AxisInputControl)this[2]; } }
	}
}
