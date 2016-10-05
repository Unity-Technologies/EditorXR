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

		public bool multiSelect { get { return m_MultiSelect; } set { m_MultiSelect = value; } }
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

		[SerializeField]
		GameObject m_TemplatePrefabMulti;

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
				UpdateToggles();
				UpdateLabel();
			}
		}
		[SerializeField]
		int[] m_Values = new int[0];

		Toggle[] m_Toggles;

		public event Action<int, int[]> onValueChanged;

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

			var template = m_MultiSelect ? m_TemplatePrefabMulti : m_TemplatePrefab;

			if (template)
			{
				var size = template.GetComponent<RectTransform>().rect.size;
				m_OptionPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y * m_Options.Length);

				var listTransform = m_OptionList.transform;

				// Clear existing options
				var children = listTransform.Cast<Transform>().ToList();
				foreach (Transform child in children)
					U.Object.Destroy(child.gameObject);

				m_Toggles = new Toggle[m_Options.Length];

				for (int i = 0; i < m_Options.Length; i++)
				{
					var optionObject = Instantiate(template, listTransform.position, listTransform.rotation, listTransform) as GameObject;
					var optionText = optionObject.GetComponentInChildren<Text>();
					if (optionText)
						optionText.text = m_Options[i];

					var toggle = optionObject.GetComponentInChildren<Toggle>();
					if (toggle)
						toggle.isOn = values.Contains(i);

					m_Toggles[i] = toggle;

					var button = optionObject.GetComponentInChildren<Button>();
					var index = i;
					button.onClick.AddListener(() =>
					{
						if (toggle)
							toggle.isOn = !toggle.isOn;
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

		public void LabelOverride(string text)
		{
			m_Label.text = text;
		}

		void OptionClicked(int val)
		{
			if (m_MultiSelect)
			{
				var list = new List<int>(values);
				if (list.Contains(val))
					list.Remove(val);
				else
					list.Add(val);
				m_Values =  list.ToArray();
			}
			else
				m_Value = val;

			UpdateLabel();

			ClosePanel();

			if(onValueChanged != null)
				onValueChanged(val, m_MultiSelect ? m_Values : new [] {m_Value});
		}

		void UpdateToggles()
		{
			for(int i = 0; i < m_Toggles.Length; i++)
			{
				var toggle = m_Toggles[i];
				if (toggle)
					toggle.isOn = m_Values.Contains(i);
			}
		}

		void UpdateLabel()
		{
			if (m_MultiSelect)
			{
				var labelText = string.Empty;
				if (values.Length > 0)
				{
					foreach (var v in values)
						labelText += m_Options[v] + ", ";
					m_Label.text = labelText.Substring(0, labelText.Length - 2);
				}
				else
					m_Label.text = "Nothing";
			}
			else
			{
				if(m_Value >= 0 && m_Value < m_Options.Length)
					m_Label.text = m_Options[m_Value];
			}
		}
	}
}