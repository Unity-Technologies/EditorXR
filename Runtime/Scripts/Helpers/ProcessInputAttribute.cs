using System;

namespace Unity.Labs.EditorXR
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
