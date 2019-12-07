using UnityEngine;

namespace Unity.Labs.EditorXR.Helpers
{
    /// <summary>
    /// Calculates a 'smooth' values of a float based on history.
    /// 1d-version of the PhysicsTracker
    /// </summary>
    class SmoothFloat
    {
        /// <summary>
        /// The time period that the input values are averaged over
        /// </summary>
        const float k_Period = 0.125f;
        const float k_HalfPeriod = k_Period * 0.5f;

        // <summary>
        /// The number of discrete steps to store input samples in
        /// </summary>
        const int k_Steps = 4;

        /// <summary>
        /// The time period stored within a single step sample
        /// </summary>
        const float k_SamplePeriod = k_Period / k_Steps;

        /// <summary>
        /// Weight to use for the most recent input sample, when doing prediction
        /// </summary>
        const float k_NewSampleWeight = 2.0f;
        const float k_AdditiveWeight = k_NewSampleWeight - 1.0f;

        /// <summary>
        /// If we are doing prediction, the time period we average over is stretched out
        /// to simulate having more data than we've actually recorded
        /// </summary>
        const float k_PredictedPeriod = k_Period + k_SamplePeriod * k_AdditiveWeight;

        /// <summary>
        /// We need to keep one extra sample in our sample buffer to have a smooth transition
        /// when dropping one sample for another
        /// </summary>
        const int k_SampleLength = k_Steps + 1;

        /// <summary>
        /// Stores one sample of tracked float data
        /// </summary>
        struct Sample
        {
            public float value;     // The actual value of the float at this sample point in time
            public float offset;  // How far this float has moved over the sampling time
            public float time;      // For how long this sample was recorded

            /// <summary>
            /// Helper function used to combine all the tracked samples up
            /// </summary>
            /// <param name="other">A sample to combine with</param>
            /// <param name="scalar">How much to scale the other sample's values</param>
            public void Accumulate(ref Sample other, float scalar)
            {
                offset += other.offset * scalar;
                time += other.time * scalar;

                // We want the oldest speed for acceleration integration
                // We do a lerp so that as the oldest sample fades out, we smooth switch to the next sample
                value = Mathf.Lerp(value, other.value, scalar);
            }
        }

        // We store all the sampled frame data in a circular array for tightest packing
        // We don't need to store the 'end' index in our array, as when we reset we always
        // make sure the frame time in that reset sample is the maximum we need
        int m_CurrentSampleIndex = -1;
        readonly Sample[] m_Samples = new Sample[k_SampleLength];

        // Previous-frame history for integrating velocity
        float m_LastValue;

        // Output data
        public float speed { get; private set; }
        public float predictedValue { get; private set; }

        /// <summary>
        /// Sets the Smoothed Float value to a 'known' linear state
        /// </summary>
        /// <param name="currentValue">The expected value that the Smoothed Float should have and predict to have until new updates</param>
        public void Reset(float currentValue)
        {
            // Reset history
            m_LastValue = currentValue;

            // Set new 'current' values that imply a held/steady float
            speed = 0.0f;
            predictedValue = m_LastValue;

            // Reset the sample array
            m_CurrentSampleIndex = 0;
            m_Samples[0] = new Sample { value = currentValue, offset = 0.0f, time = k_Period };
        }

        /// <summary>
        /// Takes in a new raw value to determine the smoothed value
        /// </summary>
        /// <param name="newValue">The 'raw' up to date value we are tracking</param>
        /// <param name="timeSlice">How much time has passed since the last update</param>
        public void Update(float newValue, float timeSlice)
        {
            // Automatically reset, if we have not done so initially
            if (m_CurrentSampleIndex == -1)
            {
                Reset(newValue);
                return;
            }

            if (timeSlice <= 0.0f)
            {
                return;
            }

            var currentOffset = newValue - m_LastValue;
            m_LastValue = newValue;

            // Add new data to the current sample
            m_Samples[m_CurrentSampleIndex].offset += currentOffset;
            m_Samples[m_CurrentSampleIndex].time += timeSlice;

            // Accumulate and generate our new smooth, predicted float values
            var combinedSample = new Sample();
            var sampleIndex = m_CurrentSampleIndex;

            while (combinedSample.time < k_Period)
            {
                var overTimeScalar = Mathf.Clamp01((k_Period - combinedSample.time) / m_Samples[sampleIndex].time);

                combinedSample.Accumulate(ref m_Samples[sampleIndex], overTimeScalar);
                sampleIndex = (sampleIndex + 1) % k_SampleLength;
            }

            var oldestValue = combinedSample.value;

            // Another accumulation step to weight the most recent values stronger for prediction
            sampleIndex = m_CurrentSampleIndex;
            while (combinedSample.time < k_PredictedPeriod) // combinedSample's time is altered in the Accumulate call below
            {
                var overTimeScalar = Mathf.Clamp01((k_PredictedPeriod - combinedSample.time) / m_Samples[sampleIndex].time);
                combinedSample.Accumulate(ref m_Samples[sampleIndex], overTimeScalar); // adjusts sample's time+offset values
                sampleIndex = (sampleIndex + 1) % k_SampleLength;
            }

            // Our combo sample is ready to be used to generate smooth output
            speed = combinedSample.offset / combinedSample.time;

            predictedValue = oldestValue + speed * k_SamplePeriod;

            // If the current sample is full, clear out the oldest sample and make that the new current sample
            if (m_Samples[m_CurrentSampleIndex].time < k_SamplePeriod)
            {
                return;
            }

            m_Samples[m_CurrentSampleIndex].value = newValue;
            m_CurrentSampleIndex = (m_CurrentSampleIndex - 1 + k_SampleLength) % k_SampleLength;
            m_Samples[m_CurrentSampleIndex] = new Sample();
        }
    }
}
