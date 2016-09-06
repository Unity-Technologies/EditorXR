namespace UnityEngine.VR.Actions
{
	[ActionItemAttribute("Redo", "ActionIcons/RedoIcon")]
	public class Redo : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;

		public Sprite icon { get; set; }

		public bool Execute()
		{
			Debug.LogError("Execute Action should redo here");
			UnityEditor.Undo.PerformRedo();
			return true;
		}
	}
}