using UnityEngine.UI;

// if you create a custom class in a file that doesn't match the class name,
// after the tests run Unity will complain that the compiler that imported 
// "TestImage" isn't available anymore, so they go in their own files
namespace UnityEditor.Experimental.EditorVR.Tests.TestHelpers
{
    public class TestImage : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh) { }
    }
}

