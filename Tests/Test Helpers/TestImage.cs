using UnityEngine.UI;

// this class exists to allow testing of the overload for 
// MaterialUtils.GetMaterialClone that takes a Graphic-derived class

namespace UnityEditor.Experimental.EditorVR.Tests.TestHelpers
{
    public class TestImage : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh) {}
    }
}
