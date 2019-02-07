using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [InitializeOnLoad]
    class BuildCallbacks : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {

        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("pre");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            Debug.Log("post");
        }
    }
}
