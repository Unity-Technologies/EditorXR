using System.Runtime.CompilerServices;

// This is necessary to allow cross-communication between assemblies for classes in the UnityEditor.Experimental.EditorVR
// namespace; The plan is that all code will eventually be within a single assembly.
[assembly: InternalsVisibleTo("Unity.Labs.EditorXR.Editor")]
[assembly: InternalsVisibleTo("Unity.Labs.EditorXR.EditorTests")]
