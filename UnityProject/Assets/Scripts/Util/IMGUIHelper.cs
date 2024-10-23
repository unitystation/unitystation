using System;
using System.Collections;
using System.Reflection;
using ImGuiNET;

namespace Util
{
	public static class IMGUIHelper
	{
		public static void DrawField(FieldInfo field, object target)
		{
			var fieldType = field.FieldType;
			var value = field.GetValue(target);

			if (fieldType == typeof(int))
			{
				var intValue = (int)value;
				if (ImGui.InputInt(field.Name, ref intValue)) field.SetValue(target, intValue);
			}
			else if (fieldType.IsEnum)
			{
				// Get the names and values of the enum
				var enumValues = Enum.GetValues(fieldType);
				var enumNames = Enum.GetNames(fieldType);
				var currentEnumIndex = Array.IndexOf(enumValues, value);

				if (ImGui.Combo(field.Name, ref currentEnumIndex, enumNames, enumNames.Length))
					// Set the field to the newly selected enum value
					field.SetValue(target, enumValues.GetValue(currentEnumIndex));
			}
			else if (typeof(IList).IsAssignableFrom(fieldType) || fieldType.IsArray)
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

						if (element != null)
							DrawObjectField(element, i.ToString()); // Draw list element based on type
						else
							ImGui.Text("null");

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
						list.Add(newElement);
					}

					// Close the collapsible TreeNode
					ImGui.TreePop();
				}
			}
			else if (fieldType == typeof(float))
			{
				var floatValue = (float)value;
				if (ImGui.InputFloat(field.Name, ref floatValue)) field.SetValue(target, floatValue);
			}
			else if (fieldType == typeof(bool))
			{
				var boolValue = (bool)value;
				if (ImGui.Checkbox(field.Name, ref boolValue)) field.SetValue(target, boolValue);
			}
			else if (fieldType == typeof(string))
			{
				var strValue = (string)value ?? string.Empty;
				if (ImGui.InputText(field.Name, ref strValue, 100)) field.SetValue(target, strValue);
			}
			else
			{
				ImGui.Text($"{field.Name}: {value?.ToString() ?? "null"}");
			}
		}

		public static void DrawObjectField(object obj, string label)
		{
			if (obj == null)
			{
				ImGui.Text($"{label}: null");
				return;
			}

			var type = obj.GetType();
			var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (var field in fields) DrawField(field, obj); // Recursively draw fields of the object
		}

		public static object CreateDefaultInstance(Type type)
		{
			if (type == typeof(int)) return 0;
			if (type == typeof(float)) return 0.0f;
			if (type == typeof(bool)) return false;
			if (type == typeof(string)) return string.Empty;

			// For other types, use Activator to create a new instance (for classes or structs)
			return Activator.CreateInstance(type);
		}

		public static void DrawObjectFields(object target)
		{
			if (target == null)
			{
				ImGui.Text("Error: Attempted to render object that is null.");
				return;
			}

			var type = target.GetType();
			var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (var field in fields)
			{
				DrawField(field, target);
			}
		}
	}
}