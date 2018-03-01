#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    using FeedbackDictionary = Dictionary<int, ProxyFeedbackRequest>;

    [ProcessInput(1)]
    [RequireComponent(typeof(ProxyNode))]
    class ProxyAnimator : MonoBehaviour, ICustomActionMap, IUsesNode, IRequestFeedback
    {
        public class TransformInfo
        {
            public Vector3 initialPosition;
            public Vector3 initialRotation;
            public Vector3 positionOffset;
            public Vector3 rotationOffset;

            public void Apply(Transform transform)
            {
                transform.localPosition = initialPosition + positionOffset;
                transform.localRotation = Quaternion.Euler(initialRotation + rotationOffset);
            }
        }

        [SerializeField]
        ActionMap m_ProxyActionMap;

        Affordance[] m_Affordances;
        AffordanceDefinition[] m_AffordanceDefinitions;
        InputControl[] m_Controls;

        readonly Dictionary<Transform, TransformInfo> m_TransformInfos = new Dictionary<Transform, TransformInfo>();

        readonly FeedbackDictionary m_FeedbackRequests = new FeedbackDictionary();

        public ActionMap actionMap { get { return m_ProxyActionMap; } }
        public bool ignoreActionMapInputLocking { get { return true; } }

        public Node node { private get; set; }

        internal event Action<Affordance[], AffordanceDefinition[], Dictionary<Transform, TransformInfo>, ActionMapInput> postAnimate;

        public void Setup(AffordanceDefinition[] affordanceDefinitions, Affordance[] affordances)
        {
            m_Affordances = affordances;
            m_AffordanceDefinitions = affordanceDefinitions;
        }

        void OnDestroy()
        {
            this.ClearFeedbackRequests();
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            var length = m_Affordances.Length;
            if (m_Controls == null)
            {
                m_Controls = new InputControl[length];

                var bindings = input.actionMap.controlSchemes[0].bindings;
                for (var i = 0; i < input.controlCount; i++)
                {
                    var control = input[i];
                    var binding = bindings[i];
                    for (var j = 0; j < length; j++)
                    {
                        var affordance = m_Affordances[j];
                        foreach (var index in binding.sources)
                        {
                            if (index.controlIndex == (int)affordance.control)
                            {
                                m_Controls[j] = control;
                                break;
                            }
                        }
                    }
                }

                for (var i = 0; i < m_Affordances.Length; i++)
                {
                    var affordance = m_Affordances[i];
                    var transforms = affordance.transforms;
                    foreach (var transform in transforms)
                    {
                        TransformInfo info;
                        if (!m_TransformInfos.TryGetValue(transform, out info))
                        {
                            info = new TransformInfo();
                            m_TransformInfos[transform] = info;
                        }

                        info.initialPosition = transform.localPosition;
                        info.initialRotation = transform.localRotation.eulerAngles;
                    }

                    if (m_Controls[i] == null)
                        Debug.LogError(string.Format("Affordance defined for missing control {0}", affordance.control));
                }
            }

            foreach (var kvp in m_TransformInfos)
            {
                var transformInfo = kvp.Value;
                transformInfo.positionOffset = Vector3.zero;
                transformInfo.rotationOffset = Vector3.zero;
            }

            for (var i = 0; i < length; i++)
            {
                var affordance = m_Affordances[i];
                var inputControl = m_Controls[i];
                var controlIndex = affordance.control;
                foreach (var affordanceDefinition in m_AffordanceDefinitions)
                {
                    if (affordanceDefinition.control == controlIndex)
                    {
                        var definitions = affordanceDefinition.animationDefinitions;

                        // Animate any values defined in the ProxyAffordanceMap's Affordance Definition
                        //Assume control values are [-1, 1]
                        if (definitions != null)
                        {
                            var transforms = affordance.transforms;
                            for (var j = 0; j < transforms.Length; j++)
                            {
                                var transform = transforms[j];
                                var definition = definitions[j];
                                var info = m_TransformInfos[transform];
                                var handednessScalar = node == Node.RightHand && definition.reverseForRightHand ? -1 : 1;
                                var min = definition.min * handednessScalar;
                                var max = definition.max * handednessScalar;
                                var offset = min + (inputControl.rawValue + 1) * (max - min) * 0.5f;

                                info.positionOffset += definition.translateAxes.GetAxis() * offset;
                                info.rotationOffset += definition.rotateAxes.GetAxis() * offset;
                            }
                        }

                        break;
                    }
                }

                if (Mathf.Approximately(inputControl.rawValue, 0))
                    HideFeedback(controlIndex);
                else
                    ShowFeedback(controlIndex);
            }

            foreach (var kvp in m_TransformInfos)
            {
                kvp.Value.Apply(kvp.Key);
            }

            if (postAnimate != null)
                postAnimate(m_Affordances, m_AffordanceDefinitions, m_TransformInfos, input);
        }

        void ShowFeedback(VRInputDevice.VRControl control)
        {
            var key = (int)control;
            if (m_FeedbackRequests.ContainsKey(key))
                return;

            var request = (ProxyFeedbackRequest)this.GetFeedbackRequestObject(typeof(ProxyFeedbackRequest));
            request.control = control;
            request.node = node;
            request.duration = -1;
            m_FeedbackRequests[key] = request;
            this.AddFeedbackRequest(request);
        }

        void HideFeedback(VRInputDevice.VRControl control)
        {
            var key = (int)control;
            ProxyFeedbackRequest request;
            if (m_FeedbackRequests.TryGetValue(key, out request))
            {
                m_FeedbackRequests.Remove(key);
                this.RemoveFeedbackRequest(request);
            }
        }
    }
}
#endif
