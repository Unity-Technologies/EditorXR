namespace UnityEngine.VR.Actions
{
	[ActionItem("Undo", "ActionIcons/UndoIcon", "DefaultActions", 2)]
	public class Undo : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;
		public Sprite icon { get; set; }

		public int indexPosition { get; set; }
		public string sectionName { get; set; }

		public bool Execute()
		{
			Debug.LogError("Execute Action should undo here");
			UnityEditor.Undo.PerformUndo();
			return true;
		}
	}
}