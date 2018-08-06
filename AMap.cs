using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class AMap : ActionMapInput {
		public AMap (ActionMap actionMap) : base (actionMap) { }
		
		public AxisInputControl @deltaX { get { return (AxisInputControl)this[0]; } }
		public AxisInputControl @deltaY { get { return (AxisInputControl)this[1]; } }
		public ButtonInputControl @mouse0 { get { return (ButtonInputControl)this[2]; } }
		public ButtonInputControl @mouse1 { get { return (ButtonInputControl)this[3]; } }
		public ButtonInputControl @mouse2 { get { return (ButtonInputControl)this[4]; } }
	}
}
