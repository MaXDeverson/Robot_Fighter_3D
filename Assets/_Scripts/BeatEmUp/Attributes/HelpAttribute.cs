using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

//----
//This attribute adds a helpbox to the Unity property inspector
//----
[AttributeUsage(AttributeTargets.Field, Inherited = true)]
public class HelpAttribute : PropertyAttribute {

	public readonly string text;
	#if UNITY_EDITOR
	public readonly MessageType type;
	#endif

	public HelpAttribute(string text
		#if UNITY_EDITOR
		, MessageType type = MessageType.Info
		#endif
	)
	{
		this.text = text;
		#if UNITY_EDITOR
		this.type = type;
		#endif
	}
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(HelpAttribute))]
public class HelpDrawer : PropertyDrawer {

	const int paddingHeight = 8; // Used for top and bottom padding between the text and the HelpBox border.
	const int marginHeight = 2; // Used to add some margin between the the HelpBox and the property.
	float baseHeight = 0; //  Global field to store the original (base) property height.
	float addedHeight = 0; // Custom added height for drawing text area which has the MultilineAttribute.
	HelpAttribute helpAttribute { get { return (HelpAttribute)attribute; } } // A wrapper which returns the PropertyDrawer.attribute field as a HelpAttribute.

	RangeAttribute rangeAttribute {
		get {
			var attributes = fieldInfo.GetCustomAttributes(typeof(RangeAttribute), true);
			return attributes != null && attributes.Length > 0 ? (RangeAttribute)attributes[0] : null;
		}
	}

	MultilineAttribute multilineAttribute {
		get {
			var attributes = fieldInfo.GetCustomAttributes(typeof(MultilineAttribute), true);
			return attributes != null && attributes.Length > 0 ? (MultilineAttribute)attributes[0] : null;
		}
	}

	public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)	{

		baseHeight = base.GetPropertyHeight(prop, label);
		float minHeight = paddingHeight * 5;
		var content = new GUIContent(helpAttribute.text);
		var style = GUI.skin.GetStyle("helpbox");
		var height = style.CalcHeight(content, EditorGUIUtility.currentViewWidth);
		height += marginHeight * 2; // add tiny padding here to make sure the text is not overflowing the HelpBox from the top and bottom.

		// Since we draw a custom text area with the label above if our property contains the
		// MultilineAttribute, we need to add some extra height to compensate. This is stored in a
		// seperate global field so we can use it again later.
		if (multilineAttribute != null && prop.propertyType == SerializedPropertyType.String) {
			addedHeight = 48f;
		}

		// If the calculated HelpBox is less than our minimum height we use this to calculate the returned height instead.
		return height > minHeight ? height + baseHeight + addedHeight : minHeight + baseHeight + addedHeight;
	}
		
	public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label) {
		
		var multiline = multilineAttribute;

		EditorGUI.BeginProperty(position, label, prop);

		// Copy the position out so we can calculate the position of our HelpBox without affecting the original position.
		var helpPos = position;

		helpPos.height -= baseHeight + marginHeight;


		if (multiline != null) {
			helpPos.height -= addedHeight;
		}

		// Renders the HelpBox in the Unity inspector UI.
		EditorGUI.HelpBox(helpPos, helpAttribute.text, helpAttribute.type);

		position.y += helpPos.height + marginHeight;
		position.height = baseHeight;


		// If we have a RangeAttribute on our field, we need to handle the PropertyDrawer differently to keep the same style as Unity's default.
		var range = rangeAttribute;

		if (range != null) {
			if (prop.propertyType == SerializedPropertyType.Float) {
				EditorGUI.Slider(position, prop, range.min, range.max, label);
			}
			else if (prop.propertyType == SerializedPropertyType.Integer) {
				EditorGUI.IntSlider(position, prop, (int)range.min, (int)range.max, label);

			} else {
				EditorGUI.PropertyField(position, prop, label);
			}
		}
		else if (multiline != null) {
			
			// Here's where we handle the PropertyDrawer differently if we have a MultiLineAttribute, to try and keep some kind of multiline text area. This is not identical to Unity's default but is better than nothing...
			if (prop.propertyType == SerializedPropertyType.String) {
				var style = GUI.skin.label;
				var size = style.CalcHeight(label, EditorGUIUtility.currentViewWidth);

				EditorGUI.LabelField(position, label);

				position.y += size;
				position.height += addedHeight - size;
				prop.stringValue = EditorGUI.TextArea(position, prop.stringValue);
			}
			else {
				EditorGUI.PropertyField(position, prop, label);
			}
		}
		else
		{
			// If we get to here it means we're drawing the default property field below the HelpBox. More custom
			// and built in PropertyDrawers could be implemented to enable HelpBox but it could easily make for
			// hefty else/if block which would need refactoring!
			EditorGUI.PropertyField(position, prop, label);
		}
		EditorGUI.EndProperty();
	}
}
#endif