using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	public interface ICustomHighlight
	{
		bool OnHighlight(GameObject go, Material material);
	}
}
