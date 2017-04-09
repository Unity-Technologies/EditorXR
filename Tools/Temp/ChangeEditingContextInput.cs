using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class ChangeEditingContextInput : ActionMapInput {
		public ChangeEditingContextInput (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @push { get { return (ButtonInputControl)this[0]; } }
		public ButtonInputControl @pop { get { return (ButtonInputControl)this[1]; } }
		public AxisInputControl @change { get { return (AxisInputControl)this[2]; } }
		public ButtonInputControl @set { get { return (ButtonInputControl)this[3]; } }
	}
}
