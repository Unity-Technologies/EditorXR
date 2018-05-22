
using UnityEngine;
using System.Collections.Generic;

namespace ListView
{
    public class ListViewItemTemplate<TItem>
    {
        public readonly GameObject prefab;
        public readonly List<TItem> pool = new List<TItem>();

        public ListViewItemTemplate(GameObject prefab)
        {
            if (prefab == null)
                Debug.LogError("Template prefab cannot be null");
            this.prefab = prefab;
        }
    }
}

