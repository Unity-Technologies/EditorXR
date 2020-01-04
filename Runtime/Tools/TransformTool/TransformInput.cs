using UnityEngine;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class TransformInput : ActionMapInput {
		public TransformInput (ActionMap actionMap) : base (actionMap) { }

		public ButtonInputControl @select { get { return (ButtonInputControl)this[0]; } }
		public ButtonInputControl @cancel { get { return (ButtonInputControl)this[1]; } }
		public AxisInputControl @suppressVertical { get { return (AxisInputControl)this[2]; } }
	}
}
