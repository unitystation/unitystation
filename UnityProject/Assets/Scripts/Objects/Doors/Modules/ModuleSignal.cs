namespace Doors.Modules
{
	/// <summary>
	/// These are used by modules when signaling to the master controller what to do when looping through modules.
	///
	/// Continue: continue executing through modules.
	/// Break: prevent any further execution, including door masters own methods.
	/// SkipRemaining: skip the remaining modules, but continue with the door masters methods.
	/// ContinueWithoutDoorStateChange: continue with module interactions, but the door wont change states from here on out.
	/// ContinueRegardlessOfOtherModulesStates: Allows doors to be openable despite other module signal states.
	/// </summary>
	public enum ModuleSignal
	{
		Continue,
		Break,
		SkipRemaining,
		ContinueWithoutDoorStateChange,
		ContinueRegardlessOfOtherModulesStates,
	}


	public enum DoorProcessingStates
	{
		SoftwarePrevented,
		SoftwareHacked,
		PhysicallyPrevented,
		PowerPrevented,
		Welded,
		PreventSilently, // Blocks opening the door and prevents processing messages.  Used for when the interaction is something other than trying to open the door (eg repairing with welder)
	}
}