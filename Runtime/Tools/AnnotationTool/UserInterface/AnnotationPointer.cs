using System.Collections.Generic;
using UnityEngine;

namespace Unity.EditorXR.Tools
{
    class AnnotationPointer : MonoBehaviour
    {
        const int k_Sides = 8;
        const float k_BottomRadius = 0.01f;
        const float k_YScale = 2.5f;

#pragma warning disable 649
        [SerializeField]
        Material m_ConeMaterial;
#pragma warning restore 649

        Material m_ConeMaterialInstance;

        Mesh m_CustomPointerMesh;

        float m_Size;

        public void Resize(float size)
        {
            if (size == m_Size)
                return;

            m_Size = size;

            size /= k_YScale;
            var vertices = m_CustomPointerMesh.vertices;
            for (var i = k_Sides; i < k_Sides * 2; i++)
            {
                var angle = (i / (float)k_Sides) * Mathf.PI * 2f;
                var sin = Mathf.Sin(angle);
                var xPos = sin * 0.001f;
                var yPos = sin * size;

                var point = new Vector3(xPos, yPos, AnnotationTool.TipDistance);
                vertices[i] = point;
            }
            m_CustomPointerMesh.vertices = vertices;
        }

        void Awake()
        {
            m_CustomPointerMesh = new Mesh();
            m_CustomPointerMesh.vertices = GeneratePointerVertices();
            m_CustomPointerMesh.triangles = GeneratePointerTriangles();

            gameObject.AddComponent<MeshFilter>().sharedMesh = m_CustomPointerMesh;

            m_ConeMaterialInstance = Instantiate(m_ConeMaterial);
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = m_ConeMaterialInstance;

            transform.localScale = new Vector3(1, k_YScale, 1);
        }

        static Vector3[] GeneratePointerVertices()
        {
            var points = new List<Vector3>();

            for (var capIndex = 0; capIndex < 2; capIndex++)
            {
                var radius = capIndex == 0 ? k_BottomRadius : Mathf.Lerp(AnnotationTool.MaxBrushSize, AnnotationTool.MinBrushSize, capIndex);

                for (var i = 0; i < k_Sides; i++)
                {
                    var angle = (i / (float)k_Sides) * Mathf.PI * 2f;
                    var xPos = Mathf.Cos(angle) * radius;
                    var yPos = Mathf.Sin(angle) * radius;

                    var point = new Vector3(xPos, yPos, capIndex * AnnotationTool.TipDistance);
                    points.Add(point);
                }
            }

            points.Add(Vector3.zero);
            points.Add(Vector3.forward * AnnotationTool.TipDistance);

            return points.ToArray();
        }

        static int[] GeneratePointerTriangles()
        {
            var triangles = new List<int>();

            GeneratePointerSideTriangles(triangles);
            GeneratePointerCapsTriangles(triangles);

            return triangles.ToArray();
        }

        static void GeneratePointerSideTriangles(List<int> triangles)
        {
            for (var i = 1; i < k_Sides; i++)
            {
                var lowerLeft = i - 1;
                var lowerRight = i;
                var upperLeft = i + k_Sides - 1;
                var upperRight = i + k_Sides;

                var sideTriangles = AnnotationTool.VerticesToPolygon(upperRight, upperLeft, lowerRight, lowerLeft);
                triangles.AddRange(sideTriangles);
            }

            // Finish the side with a polygon that loops around from the end to the start vertices.
            var finishTriangles = AnnotationTool.VerticesToPolygon(k_Sides, k_Sides * 2 - 1, 0, k_Sides - 1);
            triangles.AddRange(finishTriangles);
        }

        static void GeneratePointerCapsTriangles(List<int> triangles)
        {
            // Generate the bottom circle cap.
            const int upperLeft = k_Sides * 2 + 1;
            for (var i = 1; i < k_Sides; i++)
            {
                var lowerLeft = i - 1;
                var lowerRight = i;

                triangles.Add(upperLeft);
                triangles.Add(lowerRight);
                triangles.Add(lowerLeft);
            }

            // Close the bottom circle cap with a start-end loop triangle.
            triangles.Add(k_Sides * 2);
            triangles.Add(0);
            triangles.Add(k_Sides - 1);

            // Generate the top circle cap.
            for (var i = k_Sides + 1; i < k_Sides * 2; i++)
            {
                var lowerLeft = i - 1;
                var lowerRight = i;

                triangles.Add(lowerLeft);
                triangles.Add(lowerRight);
                triangles.Add(upperLeft);
            }

            // Close the top circle cap with a start-end loop triangle.
            triangles.Add(k_Sides * 2 - 1);
            triangles.Add(k_Sides);
            triangles.Add(k_Sides * 2 + 1);
        }

        public void SetColor(Color color)
        {
            m_ConeMaterialInstance.SetColor("_EmissionColor", color);
        }
    }
}
