using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class RadialMenuInput : ActionMapInput {
		public RadialMenuInput (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @selectItem { get { return (ButtonInputControl)this[0]; } }
		public Vector2InputControl @navigateMenu { get { return (Vector2InputControl)this[1]; } }
		public AxisInputControl @navigateMenuX { get { return (AxisInputControl)this[2]; } }
		public AxisInputControl @navigateMenuY { get { return (AxisInputControl)this[3]; } }
	}
}
