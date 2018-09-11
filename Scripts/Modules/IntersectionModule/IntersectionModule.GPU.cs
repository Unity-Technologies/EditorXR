#if UNITY_EDITOR

#define CPU_DEBUG

using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed partial class IntersectionModule
    {
        const string k_IntersectionKernelName = "Intersect";
        const string k_RayBufferName = "rays";
        const string k_VertexBufferName = "vertices";
        const string k_IndexBufferName = "indices";
        const string k_OutputBufferName = "output";

        const float k_OneThird = 1 / 3f;

        [SerializeField]
        ComputeShader m_IntersectionShader;

#if !CPU_DEBUG
        int m_IntersectionKernelIndex;

        ComputeBuffer m_RayBuffer;
        ComputeBuffer m_VertexBuffer;
        ComputeBuffer m_IndexBuffer;
        ComputeBuffer m_OutputBuffer;

        readonly Ray[] m_ManagedRayBuffer = new Ray[1];
        readonly float[] m_OutputArray = new float[1];

        int m_VectorSize;
#endif

        void SetupGPUIntersection()
        {
#if !CPU_DEBUG
            m_IntersectionKernelIndex = m_IntersectionShader.FindKernel(k_IntersectionKernelName);

            // Marshal.SizeOf<> doesn't exist on OSX?
            m_VectorSize = 12;// Marshal.SizeOf<Vector3>();

            m_RayBuffer = new ComputeBuffer(1, m_VectorSize * 2);
            m_OutputBuffer = new ComputeBuffer(1, sizeof(float));

            m_IntersectionShader.SetBuffer(m_IntersectionKernelIndex, k_RayBufferName, m_RayBuffer);
            m_IntersectionShader.SetBuffer(m_IntersectionKernelIndex, k_OutputBufferName, m_OutputBuffer);
#endif
        }

        static bool TestObjectGPU(Renderer obj, IntersectionTester tester)
        {
            var meshFilter = obj.GetComponent<MeshFilter>();
            if (!meshFilter)
                return false;

#if CPU_DEBUG
            return TestIntersection(tester.transform, meshFilter);
#else
            if (m_VertexBuffer != null)
                m_VertexBuffer.Dispose();

            if (m_IndexBuffer != null)
                m_IndexBuffer.Dispose();

            var mesh = meshFilter.sharedMesh;
            m_VertexBuffer = new ComputeBuffer(mesh.vertexCount, m_VectorSize);
            var indexCount = (int)mesh.GetIndexCount(0);
            m_IndexBuffer = new ComputeBuffer(indexCount, sizeof(int));

            m_IntersectionShader.SetBuffer(m_IntersectionKernelIndex, k_VertexBufferName, m_VertexBuffer);
            m_IntersectionShader.SetBuffer(m_IntersectionKernelIndex, k_IndexBufferName, m_IndexBuffer);

            m_VertexBuffer.SetData(mesh.vertices);
            m_IndexBuffer.SetData(mesh.GetIndices(0));

            var rayOrigin = tester.transform;
            var modelTransform = meshFilter.transform;
            var ray = new Ray(
                modelTransform.InverseTransformPoint(rayOrigin.position),
                modelTransform.InverseTransformDirection(rayOrigin.forward));

            m_OutputArray[0] = 0;
            m_OutputBuffer.SetData(m_OutputArray);
            m_ManagedRayBuffer[0] = ray;
            m_RayBuffer.SetData(m_ManagedRayBuffer);

            m_IntersectionShader.Dispatch(m_IntersectionKernelIndex, (int)(indexCount * k_OneThird), 1, 1);

            m_OutputBuffer.GetData(m_OutputArray);
            return m_OutputArray[0] > 0;
#endif
        }

#if CPU_DEBUG
        static bool TestIntersection(Transform rayTransform, MeshFilter meshFilter)
        {
            var ray = new Ray(rayTransform.position, rayTransform.forward);
            var mesh = meshFilter.sharedMesh;
            var vertices = mesh.vertices;
            var triCount = mesh.GetIndexCount(0) * k_OneThird;
            var indices = mesh.GetIndices(0);

            var modelTransform = meshFilter.transform;
            var rayVector = modelTransform.InverseTransformDirection(ray.direction);
            var rayOrigin = modelTransform.InverseTransformPoint(ray.origin);

            Debug.DrawRay(rayOrigin, rayVector * 50, Color.blue);

            for (var i = 0; i < triCount; i++)
            {
                var triIndex = i * 3;
                var vertex0 = vertices[indices[triIndex]];
                var vertex1 = vertices[indices[triIndex + 1]];
                var vertex2 = vertices[indices[triIndex + 2]];

                Debug.DrawLine(vertex0, vertex1, Color.red);
                Debug.DrawLine(vertex1, vertex2, Color.red);
                Debug.DrawLine(vertex2, vertex0, Color.red);
                var edge1 = vertex1 - vertex0;
                var edge2 = vertex2 - vertex0;
                var h = Vector3.Cross(rayVector, edge2);
                var a = Vector3.Dot(edge1, h);
                if (Mathf.Abs(a) < Mathf.Epsilon)
                    continue;

                var f = 1 / a;
                var s = rayOrigin - vertex0;
                var u = f * Vector3.Dot(s, h);
                if (u < 0.0 || u > 1.0)
                    continue;
                var q = Vector3.Cross(s, edge1);
                var v = f * Vector3.Dot(rayVector, q);
                if (v < 0.0 || u + v > 1.0)
                    continue;

                Debug.DrawLine(vertex0, vertex1, Color.green);
                Debug.DrawLine(vertex1, vertex2, Color.green);
                Debug.DrawLine(vertex2, vertex0, Color.green);

                // At this stage we can compute t to find out where the intersection point is on the line.
                float t = f * Vector3.Dot(edge2, q);
                if (t > Mathf.Epsilon) // ray intersection
                {
                    //outIntersectionPoint = rayOrigin + rayVector * t;
                    return true;
                }
            }

            return false;
        }
#endif

        void TearDownGPUIntersection()
        {
#if !CPU_DEBUG
            if (m_RayBuffer != null)
                m_RayBuffer.Release();

            if (m_OutputBuffer != null)
                m_OutputBuffer.Release();

            if (m_VertexBuffer != null)
                m_VertexBuffer.Dispose();

            if (m_IndexBuffer != null)
                m_IndexBuffer.Dispose();
#endif
        }
    }
}
#endif
