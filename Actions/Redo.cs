namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Redo", ActionMenuItemAttribute.kDefaultActionSectionName, 1)]
	public class Redo : MonoBehaviour, IAction
	{
		public Sprite icon { get { return m_Icon; } }
		[SerializeField]
		private Sprite m_Icon;

		public bool ExecuteAction()
		{
			UnityEditor.Undo.PerformRedo();
			return true;
		}
	}
}