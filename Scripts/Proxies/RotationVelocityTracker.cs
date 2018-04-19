#if UNITY_EDITOR
using System;
using UnityEngine;

namespace  UnityEditor.Experimental.EditorVR.Helpers
{
    /// <summary>
    /// Helper class that tracks an object's rotation velocity over a period of time
    /// </summary>
    //[Serializable]
    public class RotationVelocityTracker
    {
        [SerializeField]
        [Tooltip("Time period to measure rotation over")]
        float m_Period = .125f;

        [SerializeField]
        [Tooltip("Number of discrete steps to store the rotation samples in")]
        int m_Steps = 4;

        [SerializeField]
        [Tooltip("A weight to apply to the newest velocity sample for prediction")]
        float m_NewChunkWeight = 2.0f;

        int m_ChunkIndex;
        float m_ChunkPeriod = 1.0f;
        float[] m_TimeChunks = new float[0];
        float[] m_RotationDeltaChunks = new float[0];

        Quaternion m_PreviousRotation;

        bool m_Initialized;

        /// <summary>
        /// How a rotation the tracked object is experiencing this frame
        /// </summary>
        public float rotationStrength { get; private set; }

        /// <summary>
        /// Initializes the tracker's history storage and stabilizes it at its current rotation
        /// </summary>
        /// <param name="startRotation"></param>
        public void Initialize(Quaternion startRotation)
        {
            // Since this is not a monobehaviour we don't have OnValidate - which means we need to do input validation here
            m_Period = Mathf.Max(m_Period, 0.01f);
            m_Steps = Mathf.Max(m_Steps, 1);

            m_ChunkPeriod = m_Period / m_Steps;

            m_TimeChunks = new float[m_Steps];
            m_RotationDeltaChunks = new float[m_Steps];

            ForceChangeRotation(startRotation);

            m_Initialized = true;
        }

        /// <summary>
        /// Based on new position data, calculates how much shake this object is currently experiencing
        /// </summary>
        /// <param name="newRotation">The new position of this shaken object</param>
        /// <param name="timeSlice">How much time has passed to get to this new position</param>
        public void Update(Quaternion newRotation, float timeSlice)
        {
            if (!m_Initialized)
                Initialize(newRotation);

            // Update the stored time and distance values
            if (m_TimeChunks[m_ChunkIndex] > m_ChunkPeriod)
            {
                m_ChunkIndex = (m_ChunkIndex + 1) % m_Steps;
                m_RotationDeltaChunks[m_ChunkIndex] = 0;
                m_TimeChunks[m_ChunkIndex] = 0;
            }
            m_TimeChunks[m_ChunkIndex] += timeSlice;

            // Update positions and distance value
            //var currentOffset = newRotation * m_PreviousRotation; //newRotation * Quaternion.Inverse(m_PreviousRotation);
            var newRotationDelta = Quaternion.Angle(newRotation, m_PreviousRotation);

            m_PreviousRotation = newRotation;
            m_RotationDeltaChunks[m_ChunkIndex] += newRotationDelta;

            // Update the average velocity
            var totalTime = 0.0f;
            var totalRotationDelta = 0.0f;
            var minOffset = Vector3.zero;
            var maxOffset = Vector3.zero;

            var chunkCounter = 0;
            while (chunkCounter < m_Steps)
            {
                if (chunkCounter == m_ChunkIndex)
                {
                    totalTime += m_TimeChunks[chunkCounter] * m_NewChunkWeight;
                    totalRotationDelta += m_RotationDeltaChunks[chunkCounter] * m_NewChunkWeight;
                }
                else
                {
                    totalTime += m_TimeChunks[chunkCounter];
                    totalRotationDelta += m_RotationDeltaChunks[chunkCounter];
                }

                chunkCounter++;
            }
            var motionBounds = (maxOffset - minOffset).magnitude;

            if (totalTime > 0.0f)
            {
                rotationStrength = totalRotationDelta / totalTime;
            }
        }

        /// <summary>
        /// Clear the buffer and set a new position
        /// </summary>
        /// <param name="newRotation">The new position</param>
        public void ForceChangeRotation(Quaternion newRotation)
        {
            m_PreviousRotation = newRotation;
            ClearChunks();
        }

        void ClearChunks()
        {
            m_ChunkIndex = 0;
            Array.Clear(m_TimeChunks, 0, m_TimeChunks.Length);
            Array.Clear(m_RotationDeltaChunks, 0, m_RotationDeltaChunks.Length);
        }
    }
}
#endif
