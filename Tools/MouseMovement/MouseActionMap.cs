using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class MouseActionMap : ActionMapInput {
		public MouseActionMap (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @mB0 { get { return (ButtonInputControl)this[0]; } }
		public AxisInputControl @mX { get { return (AxisInputControl)this[1]; } }
		public AxisInputControl @mY { get { return (AxisInputControl)this[2]; } }
	}
}
