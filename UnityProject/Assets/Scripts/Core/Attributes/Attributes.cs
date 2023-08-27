using System;
using UnityEngine;

namespace Core.Editor.Attributes
{
	/// <summary>
	/// Hides the inspector field when not in scene-editing mode.
	/// </summary>
	public class SceneModeOnlyAttribute : PropertyAttribute { }

	/// <summary>
	/// Hides the inspector field When not in play mode
	/// </summary>
	public class PlayModeOnlyAttribute : PropertyAttribute { }


	public class SelectImplementationAttribute : PropertyAttribute
	{
		public Type FieldType;

		public SelectImplementationAttribute(Type fieldType)
		{
			FieldType = fieldType;
		}
	}

}
