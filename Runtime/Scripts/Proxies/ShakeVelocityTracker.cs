using System;
using UnityEngine;

namespace  Unity.Labs.EditorXR.Helpers
{
    /// <summary>
    /// Helper class that tracks an object's shaking velocity over a period of time
    /// This is calculated by distance traveled vs. total range of motion
    /// </summary>
    [Serializable]
    public class ShakeVelocityTracker
    {
        [SerializeField]
        [Tooltip("Time period to measure shake over")]
        float m_Period = .125f;

        [SerializeField]
        [Tooltip("Number of discrete steps to store the distance samples in")]
        int m_Steps = 4;

        [SerializeField]
        [Tooltip("A weight to apply to the newest velocity sample for prediction")]
        float m_NewChunkWeight = 2.0f;

        int m_ChunkIndex;
        float m_ChunkPeriod = 1.0f;
        float[] m_TimeChunks = new float[0];
        float[] m_DistanceChunks = new float[0];
        Vector3[] m_Offsets = new Vector3[0];

        Vector3 m_LastPosition;

        bool m_Initialized;

        /// <summary>
        /// How powerful of a shake an object is experiencing this frame
        /// </summary>
        public float shakeStrength { get; private set; }

        /// <summary>
        /// The direction this object is shaking in this frame
        /// </summary>
        public Vector3 shakeAxis { get; private set; }

        /// <summary>
        /// Initializes the shake tracker's history storage and stabilizes it at its current position
        /// </summary>
        /// <param name="startPoint"></param>
        public void Initialize(Vector3 startPoint)
        {
            // Since this is not a monobehaviour we don't have OnValidate - which means we need to do input validation here
            m_Period = Mathf.Max(m_Period, 0.01f);
            m_Steps = Mathf.Max(m_Steps, 1);

            m_ChunkPeriod = m_Period / m_Steps;

            m_TimeChunks = new float[m_Steps];
            m_DistanceChunks = new float[m_Steps];
            m_Offsets = new Vector3[m_Steps];

            ForceChangePosition(startPoint);

            m_Initialized = true;
        }

        /// <summary>
        /// Based on new position data, calculates how much shake this object is currently experiencing
        /// </summary>
        /// <param name="newPosition">The new position of this shaken object</param>
        /// <param name="timeSlice">How much time has passed to get to this new position</param>
        public void Update(Vector3 newPosition, float timeSlice)
        {
            if (!m_Initialized)
                Initialize(newPosition);

            // Update the stored time and distance values
            if (m_TimeChunks[m_ChunkIndex] > m_ChunkPeriod)
            {
                m_ChunkIndex = (m_ChunkIndex + 1) % m_Steps;
                m_DistanceChunks[m_ChunkIndex] = 0;
                m_Offsets[m_ChunkIndex] = Vector3.zero;
                m_TimeChunks[m_ChunkIndex] = 0;
            }
            m_TimeChunks[m_ChunkIndex] += timeSlice;

            // Update positions and distance value
            var currentOffset = newPosition - m_LastPosition;
            shakeAxis = currentOffset.normalized;
            var newDistance = currentOffset.magnitude;

            m_LastPosition = newPosition;
            m_DistanceChunks[m_ChunkIndex] += newDistance;
            m_Offsets[m_ChunkIndex] += currentOffset;

            // Update the average velocity
            var totalTime = 0.0f;
            var totalDistance = 0.0f;
            var minOffset = Vector3.zero;
            var maxOffset = Vector3.zero;
            var activeOffset = Vector3.zero;

            var chunkCounter = 0;
            while (chunkCounter < m_Steps)
            {
                if (chunkCounter == m_ChunkIndex)
                {
                    totalTime += m_TimeChunks[chunkCounter] * m_NewChunkWeight;
                    totalDistance += m_DistanceChunks[chunkCounter] * m_NewChunkWeight;
                    activeOffset += m_Offsets[chunkCounter] * m_NewChunkWeight;
                }
                else
                {
                    totalTime += m_TimeChunks[chunkCounter];
                    totalDistance += m_DistanceChunks[chunkCounter];
                    activeOffset += m_Offsets[chunkCounter];
                }
                minOffset = Vector3.Min(minOffset, activeOffset);
                maxOffset = Vector3.Max(maxOffset, activeOffset);

                chunkCounter++;
            }
            var motionBounds = (maxOffset - minOffset).magnitude;

            if (totalTime > 0.0f)
            {
                shakeStrength = Mathf.Max((totalDistance - motionBounds), 0.0f) / totalTime;
            }
        }

        /// <summary>
        /// Clear the buffer and set a new position
        /// </summary>
        /// <param name="newPosition">The new position</param>
        public void ForceChangePosition(Vector3 newPosition)
        {
            m_LastPosition = newPosition;
            ClearChunks();
        }

        void ClearChunks()
        {
            m_ChunkIndex = 0;
            Array.Clear(m_TimeChunks, 0, m_TimeChunks.Length);
            Array.Clear(m_DistanceChunks, 0, m_DistanceChunks.Length);
            Array.Clear(m_Offsets, 0, m_Offsets.Length);
        }
    }
}
