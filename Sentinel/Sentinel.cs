using System;
using System.Diagnostics;
using UnityEditor.Experimental.EditorVR.Sentinel;
using UnityEngine;

[assembly: OptionalDependency("UnityEditor.Experimental.EditorVR.Core.EditorXRRequirementsMet", "ENABLE_EDITORXR")]

namespace UnityEditor.Experimental.EditorVR.Sentinel
{
    [Conditional("UNITY_CCU")]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    sealed class OptionalDependencyAttribute : Attribute
    {
        public string dependentClass;
        public string define;

        public OptionalDependencyAttribute(string dependentClass, string define)
        {
            this.dependentClass = dependentClass;
            this.define = define;
        }
    }

#if UNITY_2018_4_OR_NEWER || UNITY_2019_1_OR_NEWER
    class EditorXRRequirementsMet { }
#else
    class NoEditorVR
    {
        const string k_ShowCustomEditorWarning = "EditorVR.ShowCustomEditorWarning";

        static NoEditorVR()
        {
            if (EditorPrefs.GetBool(k_ShowCustomEditorWarning, true))
            {
                var message = "EditorXR requires Unity 2018.4 or the latest, non-beta version of Unity.";
                var result = EditorUtility.DisplayDialogComplex("Update Unity", message, "Download", "Ignore", "Remind Me Again");
                switch (result)
                {
                    case 0:
                        Application.OpenURL("https://unity3d.com/get-unity/download");
                        break;
                    case 1:
                        EditorPrefs.SetBool(k_ShowCustomEditorWarning, false);
                        break;
                    case 2:
                        Debug.Log("<color=orange>" + message + "</color>");
                        break;
                }
            }
        }
    }
#endif
}
