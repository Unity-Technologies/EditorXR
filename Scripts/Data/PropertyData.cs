using UnityEditor;
using UnityEngine.VR.Utilities;

public class PropertyData : InspectorData
{
	public SerializedProperty property { get; private set; }

	public override int instanceID
	{
		get { return property.GetHashCode(); }
	}

	public PropertyData(string template, SerializedObject serializedObject, InspectorData[] children, SerializedProperty property, bool defaultToExpanded = false)
		: base(template, serializedObject, children, defaultToExpanded)
	{
		this.property = property;
	}

	public void SetChildren(InspectorData[] children)
	{
		InspectorNumberItem arraySizeItem = null;
		if (this.children != null)
		{
			foreach (var child in this.children)
			{
				if (child.item)
				{
					var childNumberItem = child.item as InspectorNumberItem;
					if (childNumberItem && childNumberItem.propertyType == SerializedPropertyType.ArraySize)
						arraySizeItem = childNumberItem;
					else
						U.Object.Destroy(child.item.gameObject);
				}
			}
		}

		// Re-use InspectorNumberItem for array Size in case we are dragging the value
		if (arraySizeItem)
		{
			foreach (var child in children)
			{
				var propChild = child as PropertyData;
				if (propChild != null && propChild.property.propertyType == SerializedPropertyType.ArraySize)
				{
					propChild.item = arraySizeItem;
					arraySizeItem.data = propChild;
				}
			}
		}

		this.children = children;
	}
}