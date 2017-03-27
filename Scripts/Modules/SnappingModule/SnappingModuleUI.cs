using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class SnappingModuleUI : MonoBehaviour
{
	[SerializeField]
	Toggle[] m_OnToggles;
	[SerializeField]
	Toggle[] m_OffToggles;

	//private SnappingModes m_LastFlag;

	//private readonly SnappingModes[] m_SnappingModes = new SnappingModes[]
	//{
	//	SnappingModes.SnapToGround,
	//	SnappingModes.SnapToSurfaceNormal,
	//	SnappingModes.Throw
	//};

	void Start()
	{
		//m_LastFlag = Snapping.currentSnappingMode;

		//for (int i = 0; i < 3; i++)
		//{
		//	bool hasFlag = Snapping.HasFlag(m_SnappingModes[i]);
		//	m_OnToggles[i].isOn = hasFlag;
		//	m_OffToggles[i].isOn = !hasFlag;

		//	m_OnToggles[i].onValueChanged.RemoveAllListeners();
		//	m_OffToggles[i].onValueChanged.RemoveAllListeners();

		//	int index = i;
		//	m_OnToggles[i].onValueChanged.AddListener(b => { OnTogglePressed(index); });
		//	m_OffToggles[i].onValueChanged.AddListener(b => { OnTogglePressed(index); });
		//}
	}

	void Update()
	{
		//if (Snapping.currentSnappingMode != m_LastFlag)
		//{
		//	for (int i = 0; i < 3; i++)
		//	{
		//		bool hasFlag = Snapping.HasFlag(m_SnappingModes[i]);

		//		m_OnToggles[i].isOn = hasFlag;
		//		m_OffToggles[i].isOn = !hasFlag;
		//	}

		//	m_LastFlag = Snapping.currentSnappingMode;
		//}
	}

	public void OnTogglePressed(int index)
	{
		//bool shouldEnable = m_OnToggles[index].isOn;
		//SnappingModes mode = m_SnappingModes[index];

		//if (shouldEnable)
		//	Snapping.currentSnappingMode |= mode;
		//else
		//	Snapping.currentSnappingMode &= ~mode;

		//m_LastFlag = Snapping.currentSnappingMode;
	}

}
