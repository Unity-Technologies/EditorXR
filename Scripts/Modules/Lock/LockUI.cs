using UnityEngine;
using UnityEngine.UI;

public class LockUI : MonoBehaviour
{

	[SerializeField]
	Button m_LockButton;

	LockModule m_LockModule;

	void Start()
	{
		m_LockModule = GetComponentInParent<LockModule>();
		HandleColoring();
	}

	void Update()
	{
		HandleColoring();
	}

	public void OnLockButtonPressed()
	{
		if (m_LockModule)
		{
			bool isLocked = m_LockModule.GetLocked(UnityEditor.Selection.activeGameObject);
			if (!isLocked)
				m_LockModule.SetLocked();
			else
				m_LockModule.SetUnLocked();
		}
	}

	private void HandleColoring()
	{
		if (m_LockModule)
		{
			var active = UnityEditor.Selection.activeGameObject;
			if (active)
			{
				bool isLocked = m_LockModule.GetLocked(active);
				m_LockButton.image.color = isLocked ? Color.red : Color.green;
			}
			else
				m_LockButton.image.color = Color.white;
		}
	}

}
