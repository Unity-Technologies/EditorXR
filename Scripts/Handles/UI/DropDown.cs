using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.UI
{
	public class DropDown : BaseHandle
	{
		public string[] options
		{
			get { return m_Options; }
			set
			{
				m_Options = value;
				SetupOptions();
			}
		}
		[SerializeField]
		string[] m_Options;

		[SerializeField]
		bool m_MultiSelect;

		[SerializeField]
		Text m_Label;

		[SerializeField]
		RectTransform m_OptionPanel;

		[SerializeField]
		LayoutGroup m_OptionList;

		[SerializeField]
		GameObject m_TemplatePrefab;

		public int value
		{
			get { return m_Value; }
			set
			{
				m_Value = value;
				UpdateLabel();
			}
		}
		[SerializeField]
		int m_Value;

		public int[] values
		{
			get { return m_Values; }
			set
			{
				m_Values = value;
				UpdateLabel();
			}
		}
		[SerializeField]
		int[] m_Values = new int[0];

		public event Action<int[]> onValueChanged;

		void Awake()
		{
			SetupOptions();
		}

		void OnEnable()
		{
			m_OptionPanel.gameObject.SetActive(false);
		}

		void SetupOptions()
		{
			if (m_Options.Length > 0)
			{
				UpdateLabel();
			}

			if (m_TemplatePrefab)
			{
				var size = m_TemplatePrefab.GetComponent<RectTransform>().rect.size;
				m_OptionPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y * m_Options.Length);

				var listTransform = m_OptionList.transform;

				// Clear existing options
				var children = listTransform.Cast<Transform>().ToList();
				foreach (Transform child in children)
					U.Object.Destroy(child.gameObject);

				for (int i = 0; i < m_Options.Length; i++)
				{
					var optionObject = Instantiate(m_TemplatePrefab, listTransform.position, listTransform.rotation, listTransform) as GameObject;
					var optionText = optionObject.GetComponentInChildren<Text>();
					if (optionText)
						optionText.text = m_Options[i];

					var optionHandle = optionObject.GetComponentInChildren<Button>();
					var index = i;
					optionHandle.onClick.AddListener(() =>
					{
						OptionClicked(index);
					});
				}
			}
		}

		protected override void OnHandleDragEnded(HandleEventData eventData)
		{
			m_OptionPanel.gameObject.SetActive(true);
		}

		public void ClosePanel()
		{
			m_OptionPanel.gameObject.SetActive(false);
		}

		void OptionClicked(int val)
		{
			if (m_MultiSelect)
				m_Values = new List<int>(values) { val }.ToArray();
			else
				m_Value = val;

			UpdateLabel();

			ClosePanel();

			if(onValueChanged != null)
				onValueChanged(m_MultiSelect ? m_Values : new [] {m_Value});
		}

		void UpdateLabel()
		{
			if (m_MultiSelect)
			{
				var labelText = string.Empty;
				foreach (var v in values)
					labelText += m_Options[v] + ", ";
				m_Label.text = labelText.Substring(labelText.Length - 3);
			}
			else
			{
				m_Label.text = m_Options[m_Value];
			}
		}
	}
}