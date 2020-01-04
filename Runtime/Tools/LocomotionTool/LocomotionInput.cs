using UnityEngine;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class LocomotionInput : ActionMapInput {
		public LocomotionInput (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @blink { get { return (ButtonInputControl)this[0]; } }
		public ButtonInputControl @crawl { get { return (ButtonInputControl)this[1]; } }
		public ButtonInputControl @scaleReset { get { return (ButtonInputControl)this[2]; } }
		public AxisInputControl @speed { get { return (AxisInputControl)this[3]; } }
		public ButtonInputControl @reverse { get { return (ButtonInputControl)this[4]; } }
		public ButtonInputControl @forward { get { return (ButtonInputControl)this[5]; } }
		public ButtonInputControl @worldReset { get { return (ButtonInputControl)this[6]; } }
		public ButtonInputControl @rotate { get { return (ButtonInputControl)this[7]; } }
		public AxisInputControl @horizontal { get { return (AxisInputControl)this[8]; } }
		public AxisInputControl @vertical { get { return (AxisInputControl)this[9]; } }
	}
}
