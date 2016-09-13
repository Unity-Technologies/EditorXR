namespace UnityEngine.VR.Actions
{
	[ActionItem("Redo", "ActionIcons/RedoIcon", "DefaultActions", 1)]
	public class Redo : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;
		public Sprite icon { get; set; }

		public int indexPosition { get; set; }
		public string sectionName { get; set; }

		public bool Execute()
		{
			Debug.LogError("Execute Action should redo here");
			UnityEditor.Undo.PerformRedo();
			return true;
		}
	}
}