#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Analytics;
using UnityEditor;

namespace Unity.Labs.EditorXR.Editor
{
    abstract class EditorXREvent
    {
        protected const int k_DefaultMaxEventsPerHour = 1000;
        protected const int k_DefaultMaxElementCount = 1000;

        /// <summary>
        /// The top-level name for an event determines which database table it goes into in the CDP backend.
        /// All events which we want grouped into a table must share the same top-level name.
        /// </summary>
        public readonly string TopLevelName;

        /// <summary>The actual event name</summary>
        public string Name;

        public int MaxEventsPerHour { get; private set; }
        public int MaxElementCount { get; private set; }

        internal EditorXREvent(string topLevelName, string name, int maxPerHour = k_DefaultMaxEventsPerHour,
            int maxElementCount = k_DefaultMaxElementCount)
        {
            TopLevelName = topLevelName;
            Name = name;
            MaxEventsPerHour = maxPerHour;
            MaxElementCount = maxElementCount;
        }
    }

    class EditorXREvent<T> : EditorXREvent where T : EditorEventArgs
    {
        internal void Send(T value)
        {
            // Analytics events will always refuse to send if analytics are disabled or the editor is for sure quitting
            if (EditorXRAnalytics.Disabled || EditorXRAnalytics.Quitting)
                return;

            value.name = Name;
            var result = EditorAnalytics.SendEventWithLimit(TopLevelName, value);
            if (result != AnalyticsResult.Ok)
            {
#if DEBUG_EXR_EDITOR_ANALYTICS
                Debug.LogWarning($"Sending event {Name} : {value} Failed with status {result}");
#endif
            }
#if DEBUG_EXR_EDITOR_ANALYTICS
            Debug.Log($"Sending event {Name} : {value} Success with status {result}");
#endif
        }

        internal EditorXREvent(string topLevelName, string name,
            int maxPerHour = k_DefaultMaxEventsPerHour, int maxElementCount = k_DefaultMaxElementCount)
            : base(topLevelName, name, maxPerHour, maxElementCount)
        {
        }
    }

    [InitializeOnLoad]
    static class EditorXRAnalytics
    {
        const string k_ExrVendorKey = "unity.labs.editorxr";

        internal static bool Quitting { get; private set; }
        internal static bool Disabled { get; private set; }

        static EditorXRAnalytics()
        {
            var result = RegisterEvent(EditorXREvents.ToolSelected);
            // if the user has analytics disabled, respect that and make sure that no code actually tries to send events
            if (result == AnalyticsResult.AnalyticsDisabled)
            {
                Disabled = true;
                return;
            }

            EditorApplication.quitting += SetQuitting;

            // this just means we've already previously registered this event for this client, and can stop.
            // remove this if you want to iterate on analytics without restarting the Editor.
            if (result == AnalyticsResult.TooManyRequests)
                return;

            RegisterEvent(EditorXREvents.WorkspaceState);
            RegisterEvent(EditorXREvents.StartStop);
        }

        // we set the Quitting variable so that we don't record window close events when the editor quits
        static void SetQuitting()
        {
            Quitting = true;
        }

        static AnalyticsResult RegisterEvent(EditorXREvent me)
        {
            return EditorAnalytics.RegisterEventWithLimit(me.TopLevelName, me.MaxEventsPerHour, me.MaxElementCount,
                k_ExrVendorKey);
        }
    }
}
#endif
