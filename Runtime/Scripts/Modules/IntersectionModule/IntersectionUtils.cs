using Unity.Labs.EditorXR.Extensions;
using Unity.Labs.EditorXR.Modules;
using UnityEngine;
using Unity.Labs.Utils;

namespace Unity.Labs.EditorXR.Utilities
{
    static class IntersectionUtils
    {
        // Local method use only -- created here to reduce garbage collection
        static readonly Vector3[] k_TriangleVertices = new Vector3[3];
        static readonly Collider[] k_Colliders = new Collider[64];
        public static Mesh BakedMesh { private get; set; }

        /// <summary>
        /// Test whether an object collides with the tester
        /// </summary>
        /// <param name="collisionTester">A mesh collider located at the origin used to test the object in it's local space</param>
        /// <param name="obj">The object to test collision on</param>
        /// <param name="tester">The tester object</param>
        /// <param name="collisionPoint">The point of collision between the tester and the surface of the object</param>
        /// <returns>The result of whether the tester is in intersection with or located within the object</returns>
        public static bool TestObject(MeshCollider collisionTester, Renderer obj, IntersectionTester tester,
            out Vector3 collisionPoint)
        {
            var transform = obj.transform;

            SetupCollisionTester(collisionTester, transform);

            // Try a simple test with specific rays located at vertices
            for (var j = 0; j < tester.rays.Length; j++)
            {
                var ray = tester.rays[j];

                //Transform rays to world space, then to object's local space
                var testerTransform = tester.transform;
                var objectTransform = obj.transform;
                ray.origin = objectTransform.InverseTransformPoint(testerTransform.TransformPoint(ray.origin));
                ray.direction = objectTransform.InverseTransformDirection(testerTransform.TransformDirection(ray.direction));

                RaycastHit hit;
                if (TestRay(collisionTester, transform, ray, out hit))
                {
                    collisionPoint = hit.point;
                    return true;
                }
            }

            // Try a more robust version with all edges
            return TestEdges(collisionTester, transform, tester, out collisionPoint);
        }

        /// <summary>
        /// Test the edges of the tester's collider against another mesh collider for intersection of being contained within
        /// </summary>
        /// <param name="collisionTester">A mesh collider located at the origin used to test the object in it's local space</param>
        /// <param name="obj">The object to test collision on</param>
        /// <param name="tester">The tester object</param>
        /// <returns>The result of whether the point/ray is intersection with or located within the object</returns>
        public static bool TestEdges(MeshCollider collisionTester, Transform obj, IntersectionTester tester, out Vector3 collisionPoint)
        {
            var boundsMagnitude = collisionTester.bounds.size.magnitude;

            var triangles = tester.triangles;
            var vertices = tester.vertices;

            var testerTransform = tester.transform;
            for (var i = 0; i < triangles.Length; i += 3)
            {
                k_TriangleVertices[0] = vertices[triangles[i]];
                k_TriangleVertices[1] = vertices[triangles[i + 1]];
                k_TriangleVertices[2] = vertices[triangles[i + 2]];

                for (var j = 0; j < 3; j++)
                {
                    RaycastHit hitInfo;

                    var start = obj.InverseTransformPoint(testerTransform.TransformPoint(k_TriangleVertices[j]));
                    var end = obj.InverseTransformPoint(testerTransform.TransformPoint(k_TriangleVertices[(j + 1) % 3]));
                    var edge = end - start;
                    var maxDistance = Mathf.Max(edge.magnitude, boundsMagnitude);
                    var direction = edge.normalized;

                    // Handle degenerate triangles
                    if (Mathf.Approximately(direction.magnitude, 0f))
                        continue;

                    // Shoot a ray from outside the object (due to face normals) in the direction of the ray to see if it is inside
                    var forwardRay = new Ray(start, direction);
                    forwardRay.origin = forwardRay.GetPoint(-maxDistance);

                    Vector3 forwardHit;

                    if (collisionTester.Raycast(forwardRay, out hitInfo, maxDistance * 2f))
                        forwardHit = hitInfo.point;
                    else
                        continue;

                    // Shoot a ray in the other direction, too, from outside the object (due to face normals)
                    Vector3 behindHit;
                    var behindRay = new Ray(end, -direction);
                    behindRay.origin = behindRay.GetPoint(-maxDistance);
                    if (collisionTester.Raycast(behindRay, out hitInfo, maxDistance * 2f))
                        behindHit = hitInfo.point;
                    else
                        continue;

                    // Check whether the triangle edge is contained or intersects with the object
                    var A = forwardHit;
                    var B = behindHit;
                    var C = start;
                    var D = end;
                    var a = OnSegment(C, A, D);
                    var b = OnSegment(C, B, D);
                    var c = OnSegment(A, C, B);
                    var d = OnSegment(A, D, B);
                    if (a || b || c || d)
                    {
                        if (!a && !b && c && d) // Tester is fully contained
                        {
                            collisionPoint = testerTransform.position;
                        }
                        else
                        {
                            var testerPosition = testerTransform.position;
                            forwardHit = obj.TransformPoint(forwardHit);
                            behindHit = obj.TransformPoint(behindHit);
                            if (Vector3.Distance(testerPosition, forwardHit) > Vector3.Distance(testerPosition, behindHit))
                                collisionPoint = behindHit;
                            else
                                collisionPoint = forwardHit;
                        }
                        return true;
                    }
                }
            }

            collisionPoint = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Returns whether C lies on segment AB
        /// </summary>
        public static bool OnSegment(Vector3 A, Vector3 C, Vector3 B)
        {
            return Mathf.Approximately(Vector3.Distance(A, C) + Vector3.Distance(C, B), Vector3.Distance(A, B));
        }

        /// <summary>
        /// Tests a "ray" against a collider; Really we are testing whether a point is located within or is intersecting with a collider
        /// </summary>
        /// <param name="collisionTester">A mesh collider located at the origin used to test the object in it's local space</param>
        /// <param name="obj">The object to test collision on</param>
        /// <param name="ray">A ray positioned at a vertex of the tester's collider</param>
        /// <returns>The result of whether the point/ray is intersection with or located within the object</returns>
        public static bool TestRay(MeshCollider collisionTester, Transform obj, Ray ray)
        {
            ray.origin = obj.InverseTransformPoint(ray.origin);
            ray.direction = obj.InverseTransformDirection(ray.direction);

            var boundsSize = collisionTester.bounds.size.magnitude;
            var maxDistance = boundsSize * 2f;

            // Shoot a ray from outside the object (due to face normals) in the direction of the ray to see if it is inside
            var forwardRay = new Ray(ray.origin, ray.direction);
            forwardRay.origin = forwardRay.GetPoint(-boundsSize);

            Vector3 forwardHit;
            RaycastHit hitInfo;
            if (collisionTester.Raycast(forwardRay, out hitInfo, maxDistance))
                forwardHit = hitInfo.point;
            else
                return false;

            // Shoot a ray in the other direction, too, from outside the object (due to face normals)
            Vector3 behindHit;
            var behindRay = new Ray(ray.origin, -ray.direction);
            behindRay.origin = behindRay.GetPoint(-boundsSize);
            if (collisionTester.Raycast(behindRay, out hitInfo, maxDistance))
                behindHit = hitInfo.point;
            else
                return false;

            // Check whether the point (i.e. ray origin) is contained within the object
            var collisionLine = forwardHit - behindHit;
            var projection = Vector3.Dot(collisionLine, ray.origin - behindHit);
            return projection >= 0f && projection <= collisionLine.sqrMagnitude;
        }

        /// <summary>
        /// Tests a ray against a collider
        /// </summary>
        /// <param name="collisionTester">A mesh collider located at the origin used to test the object in it's local space</param>
        /// <param name="obj">The object to test collision on</param>
        /// <param name="ray">A ray positioned at a vertex of the tester's collider</param>
        /// <param name="hit">Info about the raycast hit</param>
        /// <param name="maxDistance">Maximum distance at which a hit can occur</param>
        /// <returns>The result of whether the ray intersects with the object</returns>
        public static bool TestRay(MeshCollider collisionTester, Transform obj, Ray ray, out RaycastHit hit,
            float maxDistance = Mathf.Infinity)
        {
            ray.origin = obj.InverseTransformPoint(ray.origin);
            ray.direction = obj.InverseTransformVector(ray.direction);
            maxDistance = obj.InverseTransformVector(ray.direction * maxDistance).magnitude;

            return collisionTester.Raycast(ray, out hit, maxDistance);
        }

        /// <summary>
        /// Tests a box against a collider
        /// </summary>
        /// <param name="collisionTester">A mesh collider located at the origin used to test the object in it's local space</param>
        /// <param name="obj">The object to test collision on</param>
        /// <param name="center">The center of the box</param>
        /// <param name="halfExtents">Half the size of the box in each dimension</param>
        /// <param name="orientation">The rotation of the box</param>
        /// <returns>The result of whether the box intersects with the object</returns>
        public static bool TestBox(MeshCollider collisionTester, Transform obj, Vector3 center, Vector3 halfExtents, Quaternion orientation)
        {
            center = obj.InverseTransformPoint(center);
            halfExtents = Vector3.Scale(halfExtents, obj.lossyScale.Inverse());
            orientation = Quaternion.Inverse(obj.rotation) * orientation;

            foreach (var intersection in Physics.OverlapBox(center, halfExtents, orientation))
            {
                if (intersection.gameObject == collisionTester.gameObject)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Tests a sphere against a collider
        /// </summary>
        /// <param name="collisionTester">A mesh collider located at the origin used to test the object in it's local space</param>
        /// <param name="obj">The object to test collision on</param>
        /// <param name="center">The center of the sphere</param>
        /// <param name="radius">The radius of the sphere</param>
        /// <returns>The result of whether the sphere intersects with the object</returns>
        public static bool TestSphere(MeshCollider collisionTester, Transform obj, Vector3 center, float radius)
        {
            if (obj.lossyScale == Vector3.zero)
                return false;

            //Because our sphere check cannot be non-uniformly scaled, transform the test object instead
            var testerTransform = collisionTester.transform;
            testerTransform.position = obj.position;
            testerTransform.rotation = obj.rotation;

            //Negative scales cause mesh read errors
            var objScale = obj.lossyScale;
            UndoInverseScale(ref objScale.x, ref center, obj);
            UndoInverseScale(ref objScale.y, ref center, obj);
            UndoInverseScale(ref objScale.z, ref center, obj);

            //Zero scales cause mesh read errors
            PadZeroScale(ref objScale.x);
            PadZeroScale(ref objScale.y);
            PadZeroScale(ref objScale.z);

            testerTransform.localScale = objScale;

            // HACK: Signal to the physics system that the collider has moved
            collisionTester.enabled = false;
            collisionTester.enabled = true;

            var count = Physics.OverlapSphereNonAlloc(center, radius, k_Colliders);

            testerTransform.position = Vector3.zero;
            testerTransform.localScale = Vector3.one;
            testerTransform.rotation = Quaternion.identity;

            for (var i = 0; i < count; i++)
            {
                var intersection = k_Colliders[i];

                if (intersection.gameObject == collisionTester.gameObject)
                    return true;
            }

            return false;
        }

        static void UndoInverseScale(ref float scale, ref Vector3 center, Transform obj)
        {
            if (scale < 0)
            {
                scale *= -1;
                var offset = center - obj.position;
                offset = Quaternion.AngleAxis(180, obj.up) * offset;
                center = obj.position + offset;
            }
        }

        static void PadZeroScale(ref float scale)
        {
            const float epsilon = 1e-5f;
            if (Mathf.Approximately(scale, 0))
                scale = epsilon;
        }

        public static void SetupCollisionTester(MeshCollider collisionTester, Transform obj)
        {
            var mf = obj.GetComponent<MeshFilter>();
            if (mf)
            {
                var mesh = mf.sharedMesh;

#if !UNITY_EDITOR
                // Player builds throw errors for non-readable meshes
                if (!mesh.isReadable)
                    return;
#endif

                // Non-triangle meshes cause physics error
                if (mesh.GetTopology(0) != MeshTopology.Triangles)
                    return;

                if (collisionTester.sharedMesh == mesh)
                    return;

                collisionTester.sharedMesh = mf.sharedMesh;
                collisionTester.transform.localScale = Vector3.one;

                return;
            }

            var smr = obj.GetComponent<SkinnedMeshRenderer>();
            if (smr)
            {
                smr.BakeMesh(BakedMesh);
                collisionTester.sharedMesh = BakedMesh;
                collisionTester.transform.localScale = obj.transform.lossyScale.Inverse();
            }
        }
    }
}
