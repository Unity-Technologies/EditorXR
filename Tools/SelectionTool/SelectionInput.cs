using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
namespace UnityEngine.InputNew
{
	public class SelectionInput : ActionMapInput {
		public SelectionInput (ActionMap actionMap) : base (actionMap) { }
		
		public ButtonInputControl @multiSelect { get { return (ButtonInputControl)this[0]; } }
		public ButtonInputControl @select { get { return (ButtonInputControl)this[1]; } }
		public ButtonInputControl @multiSelectAlt { get { return (ButtonInputControl)this[2]; } }
	}
}
