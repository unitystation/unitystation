using System;
using System.Collections;
using ImGuiNET;
using SecureStuff;

namespace Util
{
	public static class IMGUIHelper
	{
		public static void DrawField(SafeFieldInfo field)
		{
			DrawEnumField(field);
			DrawListField(field);
			if (DrawGenericField(field) == false)
			{
				ImGui.Text($"{field.Name}: {field.Value?.ToString() ?? "null"}");
			}
		}

		public static void DrawEnumField(SafeFieldInfo field)
		{
			if (field.Type.IsEnum)
			{
				// Get the names and values of the enum
				var enumValues = Enum.GetValues(field.Type);
				var enumNames = Enum.GetNames(field.Type);
				var currentEnumIndex = Array.IndexOf(enumValues, field.Value);
				if (ImGui.Combo(field.Name, ref currentEnumIndex, enumNames, enumNames.Length))
					// Set the field to the newly selected enum value
					field.SetValue(enumValues.GetValue(currentEnumIndex));
			}
		}

		public static void DrawListField(SafeFieldInfo field)
		{
			Type fieldType = field.Type;
			object value = field.Value;
			if (typeof(IList).IsAssignableFrom(field.Type) || fieldType.IsArray)
			{
				var list = (IList)value;
				if (list == null)
				{
					ImGui.Text($"{field.Name}: null");
					return;
				}

				// Create a collapsible TreeNode for the list
				if (ImGui.TreeNode($"{field.Name} (Count: {list.Count})"))
				{
					ImGui.Separator();

					// Iterate over the list and draw its elements when expanded
					for (var i = 0; i < list.Count; i++)
					{
						ImGui.Text($"Element {i}:");
						var element = list[i];

						if (element != null) DrawObjectField(element, i.ToString()); // Draw list element based on type
						else ImGui.Text("null");

						// Optionally, allow removing elements
						if (ImGui.Button($"Remove Element {i}"))
						{
							list.RemoveAt(i);
							break; // Safeguard to avoid modifying the list while iterating
						}
					}

					// Optionally, add a button to allow adding new elements
					if (ImGui.Button($"Add Element to {field.Name}"))
					{
						var elementType = fieldType.IsArray
							? fieldType.GetElementType()
							: fieldType.GetGenericArguments()[0];
						var newElement = CreateDefaultInstance(elementType);
						if (newElement != null) list.Add(newElement);
					}

					// Close the collapsible TreeNode
					ImGui.TreePop();
				}
			}
		}

		public static bool DrawGenericField(SafeFieldInfo field)
		{
			Type fieldType = field.Type;
			object value = field.Value;
			if (fieldType == typeof(int))
			{
				var intValue = (int)value;
				if (ImGui.InputInt(field.Name, ref intValue)) field.SetValue(intValue);
				return true;
			}
			else if (fieldType == typeof(float))
			{
				var floatValue = (float)value;
				if (ImGui.InputFloat(field.Name, ref floatValue)) field.SetValue(floatValue);
				return true;
			}
			else if (fieldType == typeof(bool))
			{
				var boolValue = (bool)value;
				if (ImGui.Checkbox(field.Name, ref boolValue)) field.SetValue(boolValue);
				return true;
			}
			else if (fieldType == typeof(string))
			{
				var strValue = (string)value ?? string.Empty;
				if (ImGui.InputText(field.Name, ref strValue, 100)) field.SetValue(strValue);
				return true;
			}
			return false;
		}

		public static void DrawObjectField(object obj, string label)
		{
			if (obj == null)
			{
				ImGui.Text($"{label}: null");
				return;
			}

			var type = obj.GetType();
			var fields = AllowedReflection.GetFieldsFromFieldsGrabbleAttribute(obj);

			foreach (var field in fields) DrawField(field); // Recursively draw fields of the object
		}

		public static object CreateDefaultInstance(Type type)
		{
			if (type == typeof(int)) return 0;
			if (type == typeof(float)) return 0.0f;
			if (type == typeof(bool)) return false;
			if (type == typeof(string)) return string.Empty;

			// (Max): Activator.CreateInstance(type) seems relatively abusable, so we can't let it pass in codescans.
			// as a result, we just return null for now. If you have a type that you want to draw, you'll have to do it manually.
			return null;
		}

		public static void DrawObjectFields(object target)
		{
			if (target == null)
			{
				ImGui.Text("Error: Attempted to render object that is null.");
				return;
			}

			var fields = AllowedReflection.GetFieldsFromFieldsGrabbleAttribute(target);
			foreach (var field in fields)
			{
				DrawField(field);
			}
		}
	}
}