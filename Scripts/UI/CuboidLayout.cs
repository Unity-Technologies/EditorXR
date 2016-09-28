using UnityEngine;
using UnityEngine.EventSystems;

public class CuboidLayout : UIBehaviour
{
	private static readonly Vector2 kCuboidPivot = new Vector2(0.5f, 0.5f);
	private const float kLayerHeight = 0.004f;
	private const float kExtraSpace = 0.00055f; // To avoid Z-fighting

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

	public void SetMaterials(Material backingCubeMaterial)
	{
		foreach (var cube in m_CubeTransforms)
			cube.GetComponent<Renderer>().sharedMaterial = backingCubeMaterial;
	}

	public void UpdateCubes()
	{
		if (m_CubeTransforms == null)
			return;
		for (int i = 0; i < m_CubeTransforms.Length; i++)
		{
			var rectSize = m_TargetTransforms[i].rect.size.Abs();
			// Scale pivot by rect size to get correct xy local position
			var pivotOffset =  Vector2.Scale(rectSize, kCuboidPivot - m_TargetTransforms[i].pivot);

			// Add space for cuboid
			var localPosition = m_TargetTransforms[i].localPosition;
			m_TargetTransforms[i].localPosition = new Vector3(localPosition.x, localPosition.y, -kLayerHeight);

			//Offset by 0.5 * height to account for pivot in center
			var zOffset = kLayerHeight * 0.5f + kExtraSpace;
			m_CubeTransforms[i].localPosition = new Vector3(pivotOffset.x, pivotOffset.y, zOffset);
			m_CubeTransforms[i].localScale = new Vector3(rectSize.x, rectSize.y, kLayerHeight);
		}
	}
}