using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class UIActions : ActionMapInput {
		public UIActions (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @select { get { return (ButtonInputControl)this[0]; } }
		public AxisInputControl @verticalScroll { get { return (AxisInputControl)this[1]; } }
		public AxisInputControl @horizontalScroll { get { return (AxisInputControl)this[2]; } }
	}
}
