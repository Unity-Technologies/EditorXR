using UnityEditor;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Actions
{
	[ActionItem("Clone", "ActionIcons/CloneIcon", "DefaultActions", 3)]
	public class Clone : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;

		public Sprite icon { get; set; }
		public int indexPosition { get; set; }
		public string sectionName { get; set; }

		public bool Execute()
		{
			Debug.LogError("Execute Action should clone content here");

			float range = 4f;
			var selection = UnityEditor.Selection.GetTransforms(SelectionMode.Editable);
			foreach (var trans in selection)
			{
				Debug.LogError("Cloning selected object : " + trans.name);
				var clone = U.Object.Instantiate(trans.gameObject) as GameObject;
				var cloneTransform = clone.transform;
				cloneTransform.SetParent(null); // remove from the EditorVR hierarchy
				Vector3 cloneOffset = new Vector3(trans.position.x + Random.Range(-range, range), trans.position.y + Random.Range(-range, range), trans.position.z + Random.Range(-range, range)) + (Vector3.one * 0.5f);
				cloneTransform.position = trans.position + cloneOffset;
			}

			return true;
		}
	}
}