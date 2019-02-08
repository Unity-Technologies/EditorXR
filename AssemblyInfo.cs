using System.Runtime.CompilerServices;

// This is necessary to allow cross-communication between assemblies for classes in the UnityEditor.Experimental.EditorVR
// namespace; The plan is that all code will eventually be within a single assembly.

[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]
[assembly: InternalsVisibleTo("EXR-Editor")]
[assembly: InternalsVisibleTo("EXR-Tests")]
