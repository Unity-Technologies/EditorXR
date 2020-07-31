#if UNITY_EDITOR
using System;
// ReSharper disable InconsistentNaming

namespace Unity.EditorXR
{
    [Serializable]
    abstract class EditorEventArgs
    {
        public string name;

        public override string ToString() { return name; }
    }

    [Serializable]
    class ExrStartStopArgs : EditorEventArgs
    {
        public bool active;
        public bool play_mode;

        public ExrStartStopArgs(bool active, bool playMode)
        {
            this.active = active;
            play_mode = playMode;
        }

        public override string ToString() { return $"{name}, {active}, play mode: {play_mode}"; }
    }

    [Serializable]
    class UiComponentArgs : EditorEventArgs
    {
        public string label;
        public bool active;

        public UiComponentArgs(string label, bool active)
        {
            this.label = label;
            this.active = active;
        }

        public override string ToString() { return $"{name}, {label}, {active}"; }
    }

    [Serializable]
    class SelectToolArgs : EditorEventArgs
    {
        public string label;

        public override string ToString() { return $"{name}, {label}"; }
    }

    static class EditorXRAnalyticsEvents
    {
        const string k_TopLevelName = "editorxr";

        public static EditorXREvent<SelectToolArgs> ToolSelected =
            new EditorXREvent<SelectToolArgs>(k_TopLevelName, "toolUsed");

        public static EditorXREvent<ExrStartStopArgs> StartStop =
            new EditorXREvent<ExrStartStopArgs>(k_TopLevelName, "startStop");

        public static EditorXREvent<UiComponentArgs> WorkspaceState =
            new EditorXREvent<UiComponentArgs>(k_TopLevelName, "workspaceState");
    }
}
#endif
