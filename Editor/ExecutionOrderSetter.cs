using Unity.Labs.Utils;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    /// <summary>
    /// Sets the execution order of EditingContextManager
    /// </summary>
    [InitializeOnLoad]
    public static class ExecutionOrderSetter
    {
        /// <summary>
        /// The execution order value which will be set on the EditingContextManager script
        /// This is intended to be as early as possible so that module Awake happens before all other scripts
        /// It is still possible to use a lower execution order if necessary
        /// </summary>
        public const int EditingContextManagerExecutionOrder = short.MinValue / 2;

        static ExecutionOrderSetter()
        {
            var contextManager = new GameObject().AddComponent<EditingContextManager>();
            var managerMonoScript = MonoScript.FromMonoBehaviour(contextManager);
            if  (managerMonoScript == null)
                return;

            if (MonoImporter.GetExecutionOrder(managerMonoScript) != EditingContextManagerExecutionOrder)
                MonoImporter.SetExecutionOrder(managerMonoScript, EditingContextManagerExecutionOrder);

            UnityObjectUtils.Destroy(contextManager.gameObject);
        }
    }
}
