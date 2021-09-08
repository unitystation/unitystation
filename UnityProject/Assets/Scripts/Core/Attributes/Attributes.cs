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
}
