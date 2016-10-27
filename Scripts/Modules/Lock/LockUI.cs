using UnityEngine;
using UnityEngine.UI;

public class LockUI : MonoBehaviour
{

	[SerializeField]
	Image m_LockImage;

	[SerializeField]
	Sprite m_LockIcon;

	[SerializeField]
	Sprite m_UnlockIcon;

	LockModule m_LockModule;

	void Start()
	{
		m_LockModule = GetComponentInParent<LockModule>();
		HandleSprite();
	}

	public void OnLockButtonPressed()
	{
		if (m_LockModule)
		{
			m_LockModule.ToggleLocked();
			HandleSprite();
		}
	}

	private void HandleSprite()
	{
		if (m_LockModule)
		{
			var active = UnityEditor.Selection.activeGameObject;
			bool isLocked = m_LockModule.IsLocked(active);
			m_LockImage.sprite = isLocked ? m_UnlockIcon : m_LockIcon;
		}
	}

}
