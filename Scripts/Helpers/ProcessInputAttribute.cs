
using System;

namespace UnityEditor.Experimental.EditorVR
{
    [AttributeUsage(AttributeTargets.Class)]
    sealed class ProcessInputAttribute : Attribute
    {
        public int order;

        public ProcessInputAttribute(int order)
        {
            this.order = order;
        }
    }
}

