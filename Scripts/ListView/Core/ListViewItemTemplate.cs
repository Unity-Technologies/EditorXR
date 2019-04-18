using UnityEngine;
using System.Collections.Generic;

namespace Unity.Labs.ListView
{
    public sealed class ListViewItemTemplate<TItem>
    {
        public readonly GameObject prefab;
        public readonly Queue<TItem> pool = new Queue<TItem>();

        public ListViewItemTemplate(GameObject prefab)
        {
            if (prefab == null)
                Debug.LogError("Template prefab cannot be null");
            this.prefab = prefab;
        }
    }
}
