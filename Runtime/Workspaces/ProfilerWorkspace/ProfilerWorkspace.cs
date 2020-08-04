#if UNITY_EDITOR
using Unity.EditorXR.Helpers;
using Unity.EditorXR.Utilities;
using UnityEngine;

namespace Unity.EditorXR.Workspaces
{
    [EditorOnlyWorkspace]
    [MainMenuItem("Profiler", "Workspaces", "Analyze your project's performance")]
    [SpatialMenuItem("Profiler", "Workspaces", "Analyze your project's performance")]
    sealed class ProfilerWorkspace : EditorWindowWorkspace
    {
        RectTransform m_CaptureWindowRect;

        bool inView
        {
            get
            {
                var corners = new Vector3[4];
                m_CaptureWindowRect.GetWorldCorners(corners);

                //use a smaller rect than the full viewerCamera to re-enable only when enough of the profiler is in view.
                var camera = CameraUtils.GetMainCamera();
                var minX = camera.pixelRect.width * .25f;
                var minY = camera.pixelRect.height * .25f;
                var maxX = camera.pixelRect.width * .75f;
                var maxY = camera.pixelRect.height * .75f;

                foreach (var vec in corners)
                {
                    var screenPoint = camera.WorldToScreenPoint(vec);
                    if (screenPoint.x > minX && screenPoint.x < maxX && screenPoint.y > minY && screenPoint.y < maxY)
                        return true;
                }
                return false;
            }
        }

        public override void Setup()
        {
            base.Setup();
            UnityEditorInternal.ProfilerDriver.profileEditor = false;

            m_CaptureWindowRect = GetComponentInChildren<EditorWindowCapture>().GetComponent<RectTransform>();
        }

        void Update()
        {
            UnityEditorInternal.ProfilerDriver.profileEditor = inView;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnityEditorInternal.ProfilerDriver.profileEditor = false;
        }
    }
}
#endif
