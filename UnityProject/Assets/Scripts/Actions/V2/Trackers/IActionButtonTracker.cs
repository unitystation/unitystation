using SecureStuff;

namespace Actions.V2.Trackers
{
	public interface IActionButtonTracker
	{
		public SerializableDictionary<ActionButtonData, SerializedAction> ActionData { get; set; }
		public ActionManager TargetActionManager { get; set; }

		public void WhenHolderIsInRange();
		public void WhenHolderIsOutOfRange();
	}
}
