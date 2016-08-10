using UnityEngine;

public class ClipBox : MonoBehaviour {
	public Bounds bounds
	{
		get
		{
			return new Bounds(transform.position, Vector3.Scale(transform.localScale, m_LocalBoundsSize));
		}
		set
		{
			transform.position = value.center;
			m_LocalBoundsSize = Vector3.Scale(Inverse(transform.localScale), value.size);
		}
	}
	public Bounds localBounds
	{
		get { return new Bounds(Vector3.zero, m_LocalBoundsSize);}
		set { m_LocalBoundsSize = value.size; }
	}
	private Vector3 m_LocalBoundsSize = Vector3.one;

	//TODO: Add this function to U.Math after Spatial Hash merge
	static Vector3 Inverse(Vector3 vec)
	{
		return new Vector3(1 / vec.x, 1 / vec.y, 1 / vec.z);
	}
}
