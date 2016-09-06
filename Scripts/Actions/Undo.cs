using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Actions
{
	[ActionItemAttribute("Undo", "ActionIcons/UndoIcon")]
	public class Undo : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;

		public Sprite icon { get; set; }

		public bool Execute()
		{
			Debug.LogError("Execute Action should undo here");
			UnityEditor.Undo.PerformUndo();
			return true;
		}
	}
}