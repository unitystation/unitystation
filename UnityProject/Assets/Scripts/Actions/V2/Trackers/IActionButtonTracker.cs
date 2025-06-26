using SecureStuff;

namespace Actions.V2.Trackers
{
	public interface IActionButtonTracker
	{
		//FIXME: UnityEvent is slow. Create a safe-reflection based approach for this instead.
		public SerializableDictionary<ActionButtonData, SerializedAction> ActionData { get; set; }
		public ActionManager TargetActionManager { get; set; }

		public void WhenHolderIsInRange();
		public void WhenHolderIsOutOfRange();
	}
}