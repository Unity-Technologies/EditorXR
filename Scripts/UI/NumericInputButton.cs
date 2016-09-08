using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Modules;

public class NumericInputButton : MonoBehaviour
{
	[SerializeField]
	private string m_String;

	public Action<string> OnPressAction { private get; set; }

//	protected override void OnPress()
//	{
//		OnPressAction( m_String );
//	}
    [SerializeField]
    private BaseHandle m_BaseHandle;

	[SerializeField]
	private Transform m_MeshTransform;
	private Vector3 m_StartScale;
	private Vector3 m_PressedScale;
	public bool pressed { get; private set; }
	private const float kCooldown = 0.2f;
	private float m_CooldownTimer;
	private bool m_CanPress;

	void Awake()
	{
		m_StartScale = m_MeshTransform.localScale;
		m_PressedScale = new Vector3( 1, .5f, 1 );
		m_CanPress = true;
	}

	void Update()
	{
		if ( !m_CanPress )
		{
			m_CooldownTimer -= Time.unscaledDeltaTime;
			if ( m_CooldownTimer <= 0 )
				m_CanPress = true;
		}
	}

	private void Press()
	{
		m_MeshTransform.localScale = m_PressedScale;
		OnPress();
		pressed = true;
	}

	private void UnPress()
	{
		m_MeshTransform.localScale = m_StartScale;
		pressed = false;
		m_CooldownTimer = kCooldown;
		m_CanPress = false;
	}

	protected virtual void OnPress()
	{
	}
}
