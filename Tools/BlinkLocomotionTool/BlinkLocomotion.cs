using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class BlinkLocomotion : ActionMapInput {
		public BlinkLocomotion (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @blink { get { return (ButtonInputControl)this[0]; } }
		public AxisInputControl @yaw { get { return (AxisInputControl)this[1]; } }
		public AxisInputControl @forward { get { return (AxisInputControl)this[2]; } }
	}
}
