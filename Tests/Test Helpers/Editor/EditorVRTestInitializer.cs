using NUnit.Framework;
using UnityEditor.Experimental.EditorVR.Core;

namespace UnityEditor.Experimental.EditorVR.Tests.Core
{
	[SetUpFixture]
	public class EditorVRTestInitializer
	{
		[OneTimeSetUp]
		public void SetupBeforeAllTests()
		{
			EditingContextManager.ShowEditorVR();
		}

		[OneTimeTearDown]
		public void CleanupAfterAllTests()
		{
			EditorApplication.delayCall += CloseVRView;
		}

		private void CloseVRView()
		{
			EditorWindow.GetWindow<VRView>("EditorVR", false).Close();
		}
	}
}


