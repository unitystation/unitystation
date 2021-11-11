using UnityEditor;
using UnityEngine;

namespace Core.Editor
{
	/// Courtesy of <c>JanWosnitzaSAS</c>
	/// from <see cref="https://answers.unity.com/questions/775869/editor-how-to-add-checkmarks-to-menuitems.html"/>.

	public static class QuickLoad
	{
		private const string MenuName = "Tools/Quick Load";
		private const string SettingName = "QuickLoad";

		/// <summary>
		/// If checked, prevents nonessential scenes from loading, in addition to removing the roundstart countdown.
		/// </summary>
		public static bool IsEnabled
		{
			get => PlayerPrefs.GetInt(SettingName) == 1;
			set => PlayerPrefs.SetInt(SettingName, value ? 1 : 0);
		}

		[MenuItem(MenuName, priority = 1)]
		public static void Toggle()
		{
			IsEnabled = !IsEnabled;
		}

		[MenuItem(MenuName, true, 1)]
		private static bool ToggleValidate()
		{
			Menu.SetChecked(MenuName, IsEnabled);
			return true;
		}
	}

	/// <summary>
	/// If checked, hides prefab-related fields from scene mode, and mapping-related fields from prefab mode.
	/// </summary>
	public static class HideIrrelevantFields
	{
		private const string MenuName = "Tools/Inspector/Hide Irrelevant Fields";
		private const string SettingName = "HideIrrelevantFields";

		public static bool IsEnabled
		{
			get => EditorPrefs.GetBool(SettingName, true);
			set => EditorPrefs.SetBool(SettingName, value);
		}

		[MenuItem(MenuName)]
		private static void Toggle()
		{
			IsEnabled = !IsEnabled;
		}

		[MenuItem(MenuName, true)]
		private static bool ToggleValidate()
		{
			Menu.SetChecked(MenuName, IsEnabled);
			return true;
		}
	}
}
