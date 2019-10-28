#if !(UNITY_2018_4_OR_NEWER || UNITY_2019_1_OR_NEWER)
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [InitializeOnLoad]
    class VersionCheck
    {
        const string k_ShowCustomEditorWarning = "EditorVR.ShowCustomEditorWarning";

        static VersionCheck()
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
}
#endif
