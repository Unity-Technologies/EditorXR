using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class MainMenuInput : ActionMapInput {
		public MainMenuInput (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @show { get { return (ButtonInputControl)this[0]; } }
		public AxisInputControl @rotate { get { return (AxisInputControl)this[1]; } }
	}
}
