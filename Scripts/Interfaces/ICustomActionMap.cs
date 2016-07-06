using UnityEngine.InputNew;

namespace UnityEngine.VR.Tools
{
	public interface ICustomActionMap
	{
		ActionMap ActionMap
		{
			get;
		}

		ActionMapInput ActionMapInput
		{
			set;
			get;
		}
	}
}