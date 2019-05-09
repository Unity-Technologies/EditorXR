using System.Collections;
using System.Collections.Generic;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
    sealed class VacuumTool : MonoBehaviour, ITool, ICustomActionMap, IUsesRayOrigin, IUsesViewerScale,
        IRequestFeedback, IUsesNode
    {
#pragma warning disable 649
        [SerializeField]
        ActionMap m_ActionMap;
#pragma warning restore 649

        float m_LastClickTime;
        readonly Dictionary<Transform, Coroutine> m_VacuumingCoroutines = new Dictionary<Transform, Coroutine>();

        readonly BindingDictionary m_Controls = new BindingDictionary();
        readonly List<ProxyFeedbackRequest> m_Feedback = new List<ProxyFeedbackRequest>();

        public ActionMap actionMap { get { return m_ActionMap; } }
        public bool ignoreActionMapInputLocking { get { return false; } }

        public List<IVacuumable> vacuumables { private get; set; }

        public Transform rayOrigin { get; set; }

        public Vector3 defaultOffset { private get; set; }
        public Quaternion defaultTilt { private get; set; }
        public Node node { private get; set; }

        void Start()
        {
            InputUtils.GetBindingDictionaryFromActionMap(m_ActionMap, m_Controls);
        }

        void OnDestroy()
        {
            this.ClearFeedbackRequests();
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            var hit = false;
            foreach (var vacuumable in vacuumables)
            {
                var vacuumableTransform = vacuumable.transform;
                var ray = new Ray(rayOrigin.position, rayOrigin.forward);
                ray.origin = vacuumableTransform.InverseTransformPoint(ray.origin);
                ray.direction = vacuumableTransform.InverseTransformDirection(ray.direction);
                if (vacuumable.vacuumBounds.IntersectRay(ray))
                {
                    hit = true;
                    var vacuumInput = (VacuumInput)input;
                    if (vacuumInput.vacuum.wasJustPressed)
                    {
                        var realTime = Time.realtimeSinceStartup;
                        if (UIUtils.IsDoubleClick(realTime - m_LastClickTime))
                        {
                            consumeControl(vacuumInput.vacuum);

                            Coroutine coroutine;
                            if (m_VacuumingCoroutines.TryGetValue(vacuumableTransform, out coroutine))
                                StopCoroutine(coroutine);

                            m_VacuumingCoroutines[vacuumableTransform] = StartCoroutine(VacuumToViewer(vacuumable));
                        }

                        m_LastClickTime = realTime;
                    }

                    if (m_Feedback.Count == 0)
                    {
                        foreach (var kvp in m_Controls)
                        {
                            foreach (var id in kvp.Value)
                            {
                                var request = (ProxyFeedbackRequest)this.GetFeedbackRequestObject(typeof(ProxyFeedbackRequest));
                                request.control = id;
                                request.node = node;
                                request.tooltipText = "Double-tap to summon workspace";
                                m_Feedback.Add(request);
                                this.AddFeedbackRequest(request);
                            }
                        }
                    }

                    break;
                }
            }

            if (!hit)
            {
                foreach (var request in m_Feedback)
                {
                    this.RemoveFeedbackRequest(request);
                }
                m_Feedback.Clear();
            }
        }

        IEnumerator VacuumToViewer(IVacuumable vacuumable)
        {
            var vacuumTransform = vacuumable.transform;
            var startPosition = vacuumTransform.position;
            var startRotation = vacuumTransform.rotation;

            var offset = defaultOffset;
            offset.z += vacuumable.vacuumBounds.extents.z;
            offset *= this.GetViewerScale();

            var camera = CameraUtils.GetMainCamera().transform;
            var destPosition = camera.position + camera.rotation.ConstrainYaw() * offset;
            var destRotation = Quaternion.LookRotation(camera.forward) * defaultTilt;

            var currentValue = 0f;
            var currentVelocity = 0f;
            var currentDuration = 0f;
            const float kTargetValue = 1f;
            const float kTargetDuration = 0.5f;
            while (currentDuration < kTargetDuration)
            {
                currentDuration += Time.deltaTime;
                currentValue = MathUtilsExt.SmoothDamp(currentValue, kTargetValue, ref currentVelocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
                vacuumTransform.position = Vector3.Lerp(startPosition, destPosition, currentValue);
                vacuumTransform.rotation = Quaternion.Lerp(startRotation, destRotation, currentValue);
                yield return null;
            }

            m_VacuumingCoroutines.Remove(vacuumTransform);
        }
    }
}
