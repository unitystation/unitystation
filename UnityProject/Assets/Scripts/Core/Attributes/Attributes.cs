using System;
using UnityEngine;

namespace Core.Editor.Attributes
{
	/// <summary>
	/// Hides the inspector field when not in prefab-editing mode.
	/// </summary>
	public class PrefabModeOnlyAttribute : PropertyAttribute { }

	/// <summary>
	/// Hides the inspector field when not in scene-editing mode.
	/// </summary>
	public class SceneModeOnlyAttribute : PropertyAttribute { }

	/// <summary>
	/// Used for tests to detect whether the field is null on a prefab when it's not allowed to be
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class NotNullAttribute : Attribute { }
}
