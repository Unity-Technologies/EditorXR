using UnityEngine;
using System.Collections;
using UnityEngine.VR.Tools;
using System;

public class TranslateManipulator : MonoBehaviour, IManipulator
{
	[SerializeField]
	private BaseDraggable m_GrabHandle;

	[SerializeField]
	private BaseDraggable m_TranslateHandleX;

	[SerializeField]
	private BaseDraggable m_TranslateHandleY;

	[SerializeField]
	private BaseDraggable m_TranslateHandleZ;

	[SerializeField]
	private BaseDraggable m_TranslateHandleXY;

	[SerializeField]
	private BaseDraggable m_TranslateHandleXZ;

	[SerializeField]
	private BaseDraggable m_TranslateHandleYZ;

	[SerializeField]
	private BaseDraggable m_RotateHandleY;

	public Action<Vector3> translate { private get; set; }
	public Action<Quaternion> rotate { private get; set; }

	void OnEnable()
	{
		m_GrabHandle.onDrag += TranslateHandleOnDrag;

		m_TranslateHandleX.onDrag += TranslateHandleOnDrag;
		m_TranslateHandleY.onDrag += TranslateHandleOnDrag;
		m_TranslateHandleZ.onDrag += TranslateHandleOnDrag;

		m_TranslateHandleXY.onDrag += TranslateHandleOnDrag;
		m_TranslateHandleXZ.onDrag += TranslateHandleOnDrag;
		m_TranslateHandleYZ.onDrag += TranslateHandleOnDrag;

		m_RotateHandleY.onDrag += RotateHandleOnDrag;
	}

	void OnDisable()
	{
		m_GrabHandle.onDrag -= TranslateHandleOnDrag;

		m_TranslateHandleX.onDrag -= TranslateHandleOnDrag;
		m_TranslateHandleY.onDrag -= TranslateHandleOnDrag;
		m_TranslateHandleZ.onDrag -= TranslateHandleOnDrag;

		m_TranslateHandleXY.onDrag += TranslateHandleOnDrag;
		m_TranslateHandleXZ.onDrag -= TranslateHandleOnDrag;
		m_TranslateHandleYZ.onDrag -= TranslateHandleOnDrag;
	}

	private void TranslateHandleOnDrag(Vector3 delta)
	{
		translate(delta);
	}

	private void RotateHandleOnDrag(Vector3 delta)
	{
		rotate(Quaternion.AngleAxis(delta.y, m_RotateHandleY.transform.up));
	}

}
