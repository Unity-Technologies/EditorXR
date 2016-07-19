using UnityEngine.InputNew;

namespace UnityEngine.VR.Tools
{
	public interface ICustomActionMap
	{
		ActionMap actionMap
		{
			get;
		}

		ActionMapInput actionMapInput
		{
			set;
			get;
		}
	}
}