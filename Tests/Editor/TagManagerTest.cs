using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Experimental.EditorVR.Utilities;

namespace UnityEditor.Experimental.EditorVR.Tests
{
    [InitializeOnLoad]
    public class TagManagerTest
    {
        [Test]
        public void RequiredTags()
        {
            var requiredTags = TagManager.GetRequiredTags();

            SerializedObject so;
            var existingTags = new List<string>();
            var tags = TagManager.GetTagManagerProperty("tags", out so);
            if (tags != null)
            {
                for (var i = 0; i < tags.arraySize; i++)
                {
                    existingTags.Add(tags.GetArrayElementAtIndex(i).stringValue);
                }
            }

            var missingTags = requiredTags.Except(existingTags).ToArray();
            Assert.IsFalse(missingTags.Length > 0, "Missing tags {0}", string.Join(", ", missingTags));
        }

        [Test]
        public void RequiredLayers()
        {
            var requiredLayers = TagManager.GetRequiredLayers();

            SerializedObject so;
            var existingLayers = new List<string>();
            var layers = TagManager.GetTagManagerProperty("layers", out so);
            if (layers != null)
            {
                for (var i = 0; i < layers.arraySize; i++)
                {
                    existingLayers.Add(layers.GetArrayElementAtIndex(i).stringValue);
                }
            }

            var missingLayers = requiredLayers.Except(existingLayers).ToArray();
            Assert.IsFalse(missingLayers.Length > 0, "Missing layers {0}", string.Join(", ", missingLayers));
        }
    }
}
