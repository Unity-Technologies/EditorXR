using UnityEngine;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class RadialMenuInput : ActionMapInput {
		public RadialMenuInput (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @selectItem { get { return (ButtonInputControl)this[0]; } }
		public AxisInputControl @navigateX { get { return (AxisInputControl)this[1]; } }
		public AxisInputControl @navigateY { get { return (AxisInputControl)this[2]; } }
		public Vector2InputControl @navigate { get { return (Vector2InputControl)this[3]; } }
	}
}
