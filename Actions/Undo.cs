namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Undo", ActionMenuItemAttribute.kDefaultActionSectionName, 2)]
	public class Undo : MonoBehaviour, IAction
	{
		public Sprite icon { get { return m_Icon; } }
		[SerializeField]
		private Sprite m_Icon;

		public bool ExecuteAction()
		{
			UnityEditor.Undo.PerformUndo();
			return true;
		}
	}
}