using System;
using UnityEngine.VR.UI;

namespace UnityEngine.VR.Menus
{
	public class ActiveToolButton : MonoBehaviour
	{
		[SerializeField]
		private VRButton m_VRButton;

		public event Action<Transform> selected = delegate { };
	}
}
