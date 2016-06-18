using System.Collections.Generic;
using UnityEngine;

namespace VRLineRendererInternals
{

/// <summary>
/// The mesh chain handles all the translation between Unity's mesh class,
///  what the line renderers want to do, and what the billboard-pipe based shaders expect
/// If you need more custom/optimized access to this kind of mesh information, feel
///	free to hook into this structure directly.
/// </summary>
public class MeshChain
{
	[System.Flags]
	public enum MeshRefreshFlag
	{
		None = 0,
		Positions = 1,
		Colors = 2,
		Sizes = 4,
		All = 7
	}

	// Stored mesh data we use to update our runtime mesh
	public int reservedElements	{ get { return m_ReservedElements; } }
	protected int m_ReservedElements = 0;

	protected Vector3[] m_Verts = null;
	protected Color32[] m_Colors = null;
	protected List<Vector4> m_ShapeData = null;			// xy: UV coordinates for GPU expansion zw: Size of this vertex, size of the neighbor
	protected List<Vector3> m_NeighborPoints = null;	// Location of the next point this pipe connects to, or itself if it is a billboard

	public bool worldSpaceData	{ get { return m_WorldSpaceData; } set { m_WorldSpaceData = value; } }
	protected bool m_WorldSpaceData = false;

	// Update flags to prevent unncessary mesh data generation
	protected MeshRefreshFlag m_DataThatNeedsUpdate = MeshRefreshFlag.All;
	
	// Cached runtime data
	protected Mesh m_Mesh = null;
	protected Transform m_OwnerTransform = null;


	/// <summary>
	/// Creates or re-creates the mesh with all the data needed for billboard-pipe based line rendering
	/// </summary>
	/// <param name="owner">The gameobject that will own the created mesh</param>
	/// <param name="dynamic">Whether this mesh is going to updated frequently or not</param>
	/// <param name="totalElements">How many total billboards and pipes are needed for this renderer</param>
	public void GenerateMesh(GameObject owner, bool dynamic, int totalElements)
	{
		// Precache neccessary data
		// The mesh, vertex and triangle counts
		if (m_Mesh == null)
		{
			m_Mesh = new Mesh();
		}
		if (dynamic == true)
		{
			m_Mesh.MarkDynamic();
		}
		owner.GetComponent<MeshFilter>().mesh = m_Mesh;
		m_OwnerTransform = owner.transform;

		m_ReservedElements = totalElements;

		var vertCount = 4 * m_ReservedElements;
		var triCount = 6 * m_ReservedElements;

		m_Verts = new Vector3[vertCount];
		m_Colors = new Color32[vertCount];
		m_ShapeData = new List<Vector4>(vertCount);
		m_NeighborPoints = new List<Vector3>(vertCount);

		var triangles = new int[triCount];

		var defaultWhite = new Color32(255, 255, 255, 255);

		var uvSet1 = new Vector4(0, 0, 1, 1);
		var uvSet2 = new Vector4(1, 0, 1, 1);
		var uvSet3 = new Vector4(1, 1, 1, 1);
		var uvSet4 = new Vector4(0, 1, 1, 1);

		// Set up the basic data for all of our geometry
		var pointCounter = 0;
		while (pointCounter < m_ReservedElements)
		{
			// Get where in the various indices we need to write
			var vertOffset = pointCounter * 4;
			var triOffset = pointCounter * 6;

			// Store default color
			m_Colors[vertOffset] = defaultWhite;
			m_Colors[vertOffset + 1] = defaultWhite;
			m_Colors[vertOffset + 2] = defaultWhite;
			m_Colors[vertOffset + 3] = defaultWhite;

			// Write traditional billboard coordinates
			// We use the UV coordinates to determine direction each
			// individual vertex will expand in, in screen space
			// Last two coordinates are size expansion
			m_ShapeData.Add(uvSet1);
			m_ShapeData.Add(uvSet2);
			m_ShapeData.Add(uvSet3);
			m_ShapeData.Add(uvSet4);

			// Zero out neighbor points
			m_NeighborPoints.Add(Vector3.zero);
			m_NeighborPoints.Add(Vector3.zero);
			m_NeighborPoints.Add(Vector3.zero);
			m_NeighborPoints.Add(Vector3.zero);

			// And a proper index buffer for this element
			triangles[triOffset] = vertOffset;
			triangles[triOffset + 1] = vertOffset + 1;
			triangles[triOffset + 2] = vertOffset + 2;
			triangles[triOffset + 3] = vertOffset;
			triangles[triOffset + 4] = vertOffset + 2;
			triangles[triOffset + 5] = vertOffset + 3;

			pointCounter++;
		}

		// Now set any values we can
		m_Mesh.vertices = m_Verts;
		m_Mesh.SetUVs(0, m_ShapeData);
		m_Mesh.SetUVs(1, m_NeighborPoints);
		m_Mesh.triangles = triangles;
	}

	/// <summary>
	/// Updates any mesh vertex data that is marked as dirty
	/// </summary>
	public void RefreshMesh()
	{
		if ((m_DataThatNeedsUpdate & MeshRefreshFlag.Positions) != 0)
		{
			m_Mesh.vertices = m_Verts;
			m_Mesh.SetUVs(1, m_NeighborPoints);
		}
		if ((m_DataThatNeedsUpdate & MeshRefreshFlag.Colors) != 0)
		{
			m_Mesh.colors32 = m_Colors;
		}
		if ((m_DataThatNeedsUpdate & MeshRefreshFlag.Sizes) != 0)
		{
			m_Mesh.SetUVs(0, m_ShapeData);
		}
		m_DataThatNeedsUpdate = MeshRefreshFlag.None;

		m_Mesh.RecalculateBounds();
		if (m_WorldSpaceData == true)
		{
			var newBounds = m_Mesh.bounds;
			newBounds.center = m_OwnerTransform.InverseTransformPoint(newBounds.center);
			m_Mesh.bounds = newBounds;
		}
	}

	/// <summary>
	/// Used by external classes to alert the mesh chain that they have modified its data
	/// </summary>
	/// <param name="dataThatNeedsUpdate">Which type of data (position, color, size) has been changed</param>
	public void SetMeshDataDirty(MeshRefreshFlag dataThatNeedsUpdate)
	{
		m_DataThatNeedsUpdate |= dataThatNeedsUpdate;
	}

	/// <summary>
	/// Sets the position of a specific point in the chain
	/// </summary>
	/// <param name="elementIndex">Which control point to update</param>
	/// <param name="position">The updated position of the control point</param>
	public void SetElementPosition(int elementIndex, ref Vector3 position)
	{
		var offset = elementIndex * 4;
		m_Verts[offset] = position;
		m_Verts[offset + 1] = position;
		m_Verts[offset + 2 ] = position;
		m_Verts[offset + 3] = position;

		m_NeighborPoints[offset] = position;
		m_NeighborPoints[offset + 1] = position;
		m_NeighborPoints[offset + 2] = position;
		m_NeighborPoints[offset + 3] = position;
	}

	/// <summary>
	/// Sets the endpoints of a pipe in the chain - The pipe equivalent of SetElementPosition
	/// </summary>
	/// <param name="elementIndex">Which control pipe to update</param>
	/// <param name="startPoint">The position of the previous control point being connected to</param>
	/// <param name="endPoint">The position of the next control point being connected to</param>
	public void SetElementPipe(int elementIndex, ref Vector3 startPoint, ref Vector3 endPoint)
	{
		var offset = elementIndex * 4;
		m_Verts[offset] = startPoint;
		m_Verts[offset + 1] = startPoint;
		m_Verts[offset + 2] = endPoint;
		m_Verts[offset + 3] = endPoint;

		m_NeighborPoints[offset] = endPoint;
		m_NeighborPoints[offset + 1] = endPoint;
		m_NeighborPoints[offset + 2] = startPoint;
		m_NeighborPoints[offset + 3] = startPoint;
	}


	/// <summary>
	/// Sets the size of the billboard or pipe being rendered
	/// </summary>
	/// <param name="elementIndex">The index of the control point to update</param>
	/// <param name="sizeModification">What the radius or width of the element should be</param>
	public void SetElementSize(int elementIndex, float sizeModification)
	{
		var offset = elementIndex * 4;
		m_ShapeData[offset] = new Vector4(0,0, sizeModification, sizeModification);
		m_ShapeData[offset + 1] = new Vector4(1,0, sizeModification, sizeModification);
		m_ShapeData[offset + 2] = new Vector4(1,1, sizeModification, sizeModification);
		m_ShapeData[offset + 3] = new Vector4(0,1, sizeModification, sizeModification);
	}

	/// <summary>
	/// Sets the size of the pipe being rendered
	/// </summary>
	/// <param name="elementIndex">The index of the pipe control point to update</param>
	/// <param name="sizeModification">What the width of the pipe should be</param>
	public void SetElementSize(int elementIndex, float startSize, float endSize)
	{
		var offset = elementIndex * 4;

		m_ShapeData[offset] = new Vector4(0,0, startSize, endSize);
		m_ShapeData[offset + 1] = new Vector4(1,0, startSize, endSize);
		m_ShapeData[offset + 2] = new Vector4(1,1, endSize, startSize);
		m_ShapeData[offset + 3] = new Vector4(0,1, endSize, startSize);
	}

	/// <summary>
	/// Sets the color of a billboard or pipe in the chain
	/// </summary>
	/// <param name="elementIndex">The index of the element we are coloring</param>
	/// <param name="color">What the color of this element should be</param>
	public void SetElementColor(int elementIndex, ref Color color)
	{
		var offset = elementIndex * 4;
		m_Colors[offset] = color;
		m_Colors[offset + 1] = m_Colors[offset];
		m_Colors[offset + 2] = m_Colors[offset];
		m_Colors[offset + 3] = m_Colors[offset];
	}

	/// <summary>
	/// Sets the color of a billboard or pipe in the chain
	/// </summary>
	/// <param name="elementIndex">The index of the element we are coloring</param>
	/// <param name="color">What the color of this element should be</param>
	public void SetElementColor32(int elementIndex, ref Color32 color)
	{
		var offset = elementIndex * 4;
		m_Colors[offset] = color;
		m_Colors[offset + 1] = color;
		m_Colors[offset + 2] = color;
		m_Colors[offset + 3] = color;
	}

	/// <summary>
	/// Sets the colors of a pipe in the chain
	/// </summary>
	/// <param name="elementIndex">The index of the pipe we are coloring</param>
	/// <param name="startColor">The color of the startpoint of the pipe</param>
	/// <param name="endColor">The color of the endpoint of the pipe</param>
	public void SetElementColor(int elementIndex, ref Color startColor, ref Color endColor)
	{
		var offset = elementIndex * 4;
		m_Colors[offset] = startColor;
		m_Colors[offset + 1] = m_Colors[offset];
		m_Colors[offset + 2] = endColor;
		m_Colors[offset + 3] = m_Colors[offset + 2];
	}

	/// <summary>
	/// Sets the colors of a pipe in the chain
	/// </summary>
	/// <param name="elementIndex">The index of the pipe we are coloring</param>
	/// <param name="startColor">The color of the startpoint of the pipe</param>
	/// <param name="endColor">The color of the endpoint of the pipe</param>
	public void SetElementColor32(int elementIndex, ref Color32 startColor, ref Color32 endColor)
	{
		var offset = elementIndex * 4;
		m_Colors[offset] = startColor;
		m_Colors[offset + 1] = m_Colors[offset];
		m_Colors[offset + 2] = endColor;
		m_Colors[offset + 3] = m_Colors[offset + 2];
	}
}
}