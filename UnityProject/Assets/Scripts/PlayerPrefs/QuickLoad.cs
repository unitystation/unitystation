using UnityEngine;

namespace Core
{
	public static class QuickLoad
	{
		/// <summary>
		/// If checked, prevents nonessential scenes from loading, in addition to removing the roundstart countdown.
		/// </summary>
		public static bool IsEnabled => PlayerPrefs.GetInt("QuickLoad") == 1;
	}
}
