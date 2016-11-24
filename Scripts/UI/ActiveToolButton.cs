using System;
using System.Text;
using UnityEngine.VR.Tools;
using UnityEngine.VR.UI;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Menus
{
	public class ActiveToolButton : MonoBehaviour, ISelectTool
	{
		readonly Vector3 m_OriginalLocalPosition = new Vector3(0f, 0f, -0.035f);

		public event Action<Transform> selected = delegate { };
		public Transform menuOrigin { private get; set; }
		public Transform activeToolRayOrigin { get; set; }
		public Func<Transform, Type, bool> selectTool { private get; set; }

		public Type toolType
		{
			get
			{
				return m_ToolType;
			}

			set
			{
				if (m_ToolType == value)
					return;

				m_VRButton.gameObject.SetActive(true);

				m_ToolType = value;
				if (m_ToolType != null)
				{
					m_VRButton.SetContent(GetTypeAbbreviation(m_ToolType));
					SetButtonGradients(true);
					m_VRButton.visible = true;
				}
			}
		}
		private Type m_ToolType;

		[SerializeField]
		private VRButton m_VRButton;

		private void Start()
		{
			transform.localPosition = m_OriginalLocalPosition;
			transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
			transform.localScale = Vector3.one;
			m_VRButton.onClick += OnClick;
			m_VRButton.gameObject.SetActive(false);
		}

		void OnClick()
		{
			SetButtonGradients(selectTool(activeToolRayOrigin, m_ToolType));
		}

		// Create periodic table-style names for types
		string GetTypeAbbreviation(Type type)
		{
			var abbreviation = new StringBuilder();
			foreach (var ch in type.Name.ToCharArray())
			{
				if (char.IsUpper(ch))
					abbreviation.Append(abbreviation.Length > 0 ? char.ToLower(ch) : ch);

				if (abbreviation.Length >= 2)
					break;
			}

			return abbreviation.ToString();
		}

		void SetButtonGradients(bool active)
		{
			if (active)
			{
				m_VRButton.normalGradientPair = UnityBrandColorScheme.sessionGradient;
				m_VRButton.highlightGradientPair = UnityBrandColorScheme.grayscaleSessionGradient;
			}
			else
			{
				m_VRButton.normalGradientPair = UnityBrandColorScheme.grayscaleSessionGradient;
				m_VRButton.highlightGradientPair = UnityBrandColorScheme.sessionGradient;
			}
		}
	}
}
