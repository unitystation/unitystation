using Logs;
using UnityEditor;

namespace Core.Editor
{
	/// Courtesy of <c>JanWosnitzaSAS</c>
	/// from <see cref="https://answers.unity.com/questions/775869/editor-how-to-add-checkmarks-to-menuitems.html"/>.

	public static class QuickLoad
	{
		private const string MenuName = "Tools/Quick Load";
		private const string SettingName = "quickLoad";

		/// <summary>
		/// If checked, prevents nonessential scenes from loading, in addition to removing the roundstart countdown.
		/// </summary>
		public static bool IsEnabled
		{
			get => EditorPrefs.GetBool(SettingName, true);
			set => EditorPrefs.SetBool(SettingName, value);
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

		public static class QuickJobSelect
		{
			private const string MenuName = "Tools/Quick Job Select";
			private const string SettingName = "QuickJobSelect";


			/// <summary
			/// If checked, prevents nonessential scenes from loading, in addition to removing the roundstart countdown.
			/// </summary>
			public static bool IsEnabled
			{
				get => EditorPrefs.GetBool(SettingName, true);
				set => EditorPrefs.SetBool(SettingName, value);
			}

			[MenuItem(MenuName, priority = 1)]
			public static void Toggle()
			{
				IsEnabled = !IsEnabled;
				UpdateGameManager(IsEnabled);
			}



			[MenuItem(MenuName, true, 1)]
			private static bool ToggleValidate()
			{
				Menu.SetChecked(MenuName, IsEnabled);
				return true;
			}


			private static void UpdateGameManager(bool isQuickLoad)
			{
				var gameManager = AssetDatabase.LoadAssetAtPath<GameManager>
					("Assets/Prefabs/SceneConstruction/NestedManagers/GameManager.prefab");
				if (gameManager == null)
				{
					Loggy.LogWarning(
						$"{nameof(GameManager)} not found! Cannot set {nameof(GameManager.QuickLoad)} property.");
					return;
				}

				if (gameManager.QuickJoinLoad != isQuickLoad)
				{
					gameManager.QuickJoinLoad = isQuickLoad;
					EditorUtility.SetDirty(gameManager);
					AssetDatabase.SaveAssets();
				}
			}
		}
	}
}