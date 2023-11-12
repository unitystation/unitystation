using System;
using UnityEngine;

namespace Core.Editor.Attributes
{
	/// <summary>
	/// Hides the inspector field when not in scene-editing mode.
	/// </summary>
	public class SceneModeOnlyAttribute : PropertyAttribute { }

	public class SelectImplementationAttribute : PropertyAttribute
	{
		public Type FieldType;

		public SelectImplementationAttribute(Type fieldType)
		{
			FieldType = fieldType;
		}
	}

}
