using UnityEngine.InputNew;

namespace UnityEngine.VR.Tools
{
	public interface ICustomActionMaps
	{
		ActionMap[] actionMaps { get; }
		ActionMapInput[] actionMapInputs { set; get; }
	}
}