using UnityEditor;
using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Actions
{
	[ActionItemAttribute("Clone", "ActionIcons/CloneIcon")]
	public class Clone : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;

		public Sprite icon { get; set; }

		public bool Execute()
		{
			Debug.LogError("Execute Action should clone content here");
			var selection = UnityEditor.Selection.GetTransforms(SelectionMode.Editable);
			foreach (var trans in selection)
			{
				Debug.LogError("Cloning selected object : " + trans.name);
				var clone = GameObject.Instantiate(trans.gameObject) as GameObject;
				var cloneTransform = clone.transform;
				Vector3 cloneOffset = new Vector3(trans.position.x + Random.Range(-1f, 1f), trans.position.y + Random.Range(-1f, 1f), trans.position.z + Random.Range(-1f, 1f)) * 0.25f;
				cloneTransform.position = trans.position + cloneOffset;
			}

			return true;
		}
	}
}