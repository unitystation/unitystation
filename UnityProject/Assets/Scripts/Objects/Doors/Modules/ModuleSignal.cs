namespace Doors.Modules
{
	/// <summary>
	/// These are used by modules when signaling to the master controller what to do when looping through modules.
	///
	/// Continue: continue executing through modules.
	/// Break: prevent any further execution, including door masters own methods.
	/// SkipRemaining: skip the remaining modules, but continue with the door masters methods.
	/// ContinueWithoutDoorStateChange: continue with module interactions, but the door wont change states from here on out.
	/// </summary>
	public enum ModuleSignal
	{
		Continue,
		Break,
		SkipRemaining,
		ContinueWithoutDoorStateChange,
	}


	public enum DoorProcessingStates
	{
		SoftwarePrevented,
		SoftwareHacked,
	}
}