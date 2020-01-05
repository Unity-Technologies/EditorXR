using UnityEngine;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class AnnotationInput : ActionMapInput {
		public AnnotationInput (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @draw { get { return (ButtonInputControl)this[0]; } }
		public AxisInputControl @changeBrushSize { get { return (AxisInputControl)this[1]; } }
		public AxisInputControl @vertical { get { return (AxisInputControl)this[2]; } }
		public ButtonInputControl @stickButton { get { return (ButtonInputControl)this[3]; } }
	}
}
