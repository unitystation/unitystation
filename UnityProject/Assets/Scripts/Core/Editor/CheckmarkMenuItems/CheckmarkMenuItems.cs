using UnityEditor;


namespace Core.Editor
{
	[InitializeOnLoad]
	public static class HideIrrelevantFields
	{
		private const string MenuPathName = "Tools/Inspector/Hide Irrelevant Fields";

		public static bool IsEnabled { get; private set; } = true;

		static HideIrrelevantFields()
		{
			IsEnabled = EditorPrefs.GetBool(MenuPathName, IsEnabled);
		}

		[MenuItem(MenuPathName)]
		private static void ToggleItem()
		{
			IsEnabled = !IsEnabled;

			Menu.SetChecked(MenuPathName, IsEnabled);
			EditorPrefs.SetBool(MenuPathName, IsEnabled);
		}
	}
}
