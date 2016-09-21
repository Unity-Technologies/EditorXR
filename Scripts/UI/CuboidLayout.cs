using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VR.Utilities;

public class CuboidLayout : UIBehaviour
{
	private static readonly Vector2 kCuboidPivot = new Vector2(0.5f, 0.5f);
	private const float kLayerHeight = 0.004f;
	private const float kExtraSpace = 0.00055f; // To avoid Z-fighting

	[SerializeField]
	private int m_Depth; // For stacking

	[SerializeField]
	private RectTransform[] m_TargetTransforms;

	[SerializeField]
	private GameObject m_CubePrefab;

	private Transform[] m_CubeTransforms;

	protected override void Start()
	{
		m_CubeTransforms = new Transform[m_TargetTransforms.Length];
		for (int i = 0; i < m_CubeTransforms.Length; i++)
		{
			var cube = Instantiate(m_CubePrefab).transform;
			cube.transform.SetParent(m_TargetTransforms[i], false);
			m_CubeTransforms[i] = cube;
		}
		UpdateCubes();
	}

	protected override void OnRectTransformDimensionsChange()
	{
		UpdateCubes();
	}

	private void UpdateCubes()
	{
		for (int i = 0; i < m_CubeTransforms.Length; i++)
		{
			var rect = m_TargetTransforms[i].rect;
			// Scale pivot by rect size to get correct xy local position
			var pivotOffset =  Vector2.Scale(rect.size, kCuboidPivot - m_TargetTransforms[i].pivot);
			
			//Offset by number of layers + 0.5 to account for pivot in center
			var zOffset = kLayerHeight * (m_Depth + 0.5f) + kExtraSpace;

			m_CubeTransforms[i].localPosition = new Vector3(pivotOffset.x, pivotOffset.y, zOffset);
			m_CubeTransforms[i].localScale = new Vector3(rect.width, rect.height, kLayerHeight);
		}
	}
}