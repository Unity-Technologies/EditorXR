using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class MouseInput : ActionMapInput {
		public MouseInput (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @button0 { get { return (ButtonInputControl)this[0]; } }
		public AxisInputControl @positionX { get { return (AxisInputControl)this[1]; } }
		public AxisInputControl @positionY { get { return (AxisInputControl)this[2]; } }
	}
}
