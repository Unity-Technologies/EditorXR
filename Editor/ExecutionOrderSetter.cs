using Unity.Labs.Utils;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    /// <summary>
    /// Runtime hooks for EditingContextManager.  One of these must be in any scene which depends on modules for it to function properly
    /// </summary>
    [InitializeOnLoad]
    public static class ExecutionOrderSetter
    {
        // For some reason, we can't set an execution order as low as int.MinValue
        public const int EditingContextManagerExecutionOrder = short.MinValue / 2;

        static ExecutionOrderSetter()
        {
            var contextManager = new GameObject().AddComponent<EditingContextManager>();
            var managerMonoScript = MonoScript.FromMonoBehaviour(contextManager);
            if (MonoImporter.GetExecutionOrder(managerMonoScript) != EditingContextManagerExecutionOrder)
                MonoImporter.SetExecutionOrder(managerMonoScript, EditingContextManagerExecutionOrder);

            UnityObjectUtils.Destroy(contextManager.gameObject);
        }
    }
}
