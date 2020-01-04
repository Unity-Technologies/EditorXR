using System.Collections;
using Unity.Labs.EditorXR.Utilities;
using UnityEngine;

namespace Unity.Labs.EditorXR.UI
{
    sealed class KeyboardMallet : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        Transform m_StemOrigin;

        [SerializeField]
        float m_StemLength = 0.06f;

        [SerializeField]
        float m_StemWidth = 0.003125f;

        [SerializeField]
        Transform m_Bulb;

        [SerializeField]
        float m_BulbRadius;

        [SerializeField]
        Collider m_BulbCollider;
#pragma warning restore 649

        KeyboardButton m_CurrentButton;

        Coroutine m_ShowHideCoroutine;

        bool m_Open;

        public bool visible
        {
            get { return m_Visible; }
            set
            {
                m_Visible = value;
                gameObject.SetActive(true);
                if (m_ShowHideCoroutine != null)
                    StopCoroutine(m_ShowHideCoroutine);

                m_ShowHideCoroutine = StartCoroutine(value
                    ? ShowMallet()
                    : HideMallet());
            }
        }

        bool m_Visible;

        /// <summary>
        /// Invoked by the editor to update the mallet components' transform data.
        /// </summary>
        public void UpdateMalletDimensions()
        {
            m_StemOrigin.localScale = new Vector3(m_StemWidth, m_StemLength, m_StemWidth);

            m_Bulb.transform.localPosition = new Vector3(0f, 0f, m_StemLength * 2f);
            m_Bulb.transform.localScale = Vector3.one * m_BulbRadius * 2f;
        }

        /// <summary>
        /// Check for colliders that are keyboard keys.
        /// </summary>
        public void CheckForKeyCollision()
        {
            if (!m_Open)
                return;

            if (m_CurrentButton != null)
                m_CurrentButton.OnTriggerStay(m_BulbCollider);

            var shortestDistance = Mathf.Infinity;
            KeyboardButton hitKey = null;
            Collider[] hitColliders = Physics.OverlapSphere(m_Bulb.position, m_BulbRadius);
            foreach (var col in hitColliders)
            {
                var key = col.GetComponentInParent<KeyboardButton>();
                if (key != null)
                {
                    var newDist = Vector3.Distance(m_Bulb.position, key.transform.position);
                    if (newDist < shortestDistance)
                        hitKey = key;
                }
            }

            if (m_CurrentButton != hitKey)
            {
                if (m_CurrentButton != null)
                    m_CurrentButton.OnTriggerExit(m_BulbCollider);

                m_CurrentButton = hitKey;

                if (m_CurrentButton != null)
                    m_CurrentButton.OnTriggerEnter(m_BulbCollider);
            }
        }

        IEnumerator HideMallet()
        {
            m_Open = false;

            var stemScale = m_StemOrigin.localScale;
            var startLength = m_StemOrigin.localScale.y;
            var currentLength = m_StemOrigin.localScale.y; // cache current length for smooth animation to target value without snapping
            var bulbStartScale = m_Bulb.localScale;

            var smoothVelocity = 0f;
            var currentDuration = 0f;
            const float kTargetDuration = 0.3f;
            while (currentDuration < kTargetDuration)
            {
                currentLength = MathUtilsExt.SmoothDamp(currentLength, 0f, ref smoothVelocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
                m_StemOrigin.localScale = new Vector3(stemScale.x, currentLength, stemScale.z);
                m_Bulb.transform.localPosition = new Vector3(0f, 0f, currentLength * 2f);
                var alpha = currentLength / startLength;
                m_Bulb.transform.localScale = bulbStartScale * alpha;
                currentDuration += Time.deltaTime;
                yield return null;
            }

            m_Bulb.transform.localPosition = Vector3.zero;
            m_Bulb.transform.localScale = Vector3.zero;

            m_ShowHideCoroutine = null;
        }

        IEnumerator ShowMallet()
        {
            var stemScale = m_StemOrigin.localScale;
            var currentLength = m_StemOrigin.localScale.y;
            var targetBulbScale = Vector3.one * m_BulbRadius * 2f;

            var smoothVelocity = 0f;
            const float kTargetDuration = 0.3f;
            var currentDuration = 0f;
            while (currentDuration < kTargetDuration)
            {
                currentLength = MathUtilsExt.SmoothDamp(currentLength, m_StemLength, ref smoothVelocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
                m_StemOrigin.localScale = new Vector3(stemScale.x, currentLength, stemScale.z);
                m_Bulb.transform.localPosition = new Vector3(0f, 0f, currentLength * 2f);
                var alpha = currentLength / m_StemLength;
                m_Bulb.transform.localScale = targetBulbScale * alpha;
                currentDuration += Time.deltaTime;
                yield return null;
            }

            m_Bulb.transform.localPosition = new Vector3(0f, 0f, m_StemLength * 2f);
            m_Bulb.transform.localScale = targetBulbScale;

            m_Open = true;
            m_ShowHideCoroutine = null;
        }
    }
}
