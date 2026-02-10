#if UNITY_EDITOR
using UnityEditor;


namespace US13.Core.Threading
{
	[InitializeOnLoad]
	public static class PauseStateChangedEditor
	{
		public static bool IsPaused = false;

		static PauseStateChangedEditor()
		{
			EditorApplication.pauseStateChanged += LogPauseState;
		}

		private static void LogPauseState(PauseState state)
		{
			IsPaused = state == PauseState.Paused;
		}
	}
}
#endif