using UnityEngine;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class ProxyAnimatorInput : ActionMapInput {
		public ProxyAnimatorInput (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @trigger1 { get { return (ButtonInputControl)this[0]; } }
		public AxisInputControl @trigger2 { get { return (AxisInputControl)this[1]; } }
		public ButtonInputControl @action1 { get { return (ButtonInputControl)this[2]; } }
		public ButtonInputControl @action2 { get { return (ButtonInputControl)this[3]; } }
		public ButtonInputControl @stickButton { get { return (ButtonInputControl)this[4]; } }
		public AxisInputControl @stickX { get { return (AxisInputControl)this[5]; } }
		public AxisInputControl @stickY { get { return (AxisInputControl)this[6]; } }
	}
}
