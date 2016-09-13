namespace UnityEngine.VR.Actions
{
	[ActionItem("SaveScene", "ActionIcons/SaveSceneIcon", "Scene")]
	public class SaveScene : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;
		public Sprite icon { get; set; }

		public int indexPosition { get; set; }
		public string sectionName { get; set; }

		public bool Execute()
		{
			Debug.LogError("Execute Action should save a scene here");

			return true;
		}
	}
}