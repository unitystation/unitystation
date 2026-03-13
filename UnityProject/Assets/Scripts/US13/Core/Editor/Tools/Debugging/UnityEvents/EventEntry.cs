namespace US13.Core.Editor.Tools.Debugging.UnityEvents
{
	public class EventEntry
	{
		public string PrefabPath;
		public string GameObjectPath;
		public string ComponentName;
		public string FieldName;
		public int    ListenerCount;
		public int    BrokenListenerCount;
		public bool   HasListeners => ListenerCount > 0;
		public bool   IsBroken     => BrokenListenerCount > 0;

		public string DisplayPath;
		public string SearchKey;
	}
}