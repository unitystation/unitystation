
/// <summary>
/// Defines the configuration for a standard progress action, can be safely reused
/// between progress action instances.
/// </summary>
public class StandardProgressActionConfig
{
	/// <summary>
	/// What type of progress action this is for the purposes of determining if it
	/// is allowed to be started.
	/// </summary>
	public readonly StandardProgressActionType StandardProgressActionType;

	/// <summary>
	/// Whether player is allowed to perform this action simultaneously on multiple different tiles.
	/// </summary>
	public readonly bool AllowMultiple;

	/// <summary>
	/// When this is true, and 2 players perform a progress action of the same type on the same tile, when one player
	/// completes the action, the other players progress will be interrupted.
	///
	/// For example, once a rock is mined, it shouldn't be allowed to be mined again, so this
	/// should be set to true for that case (Construction). But for mopping, it's okay to mop the same tile multiple
	/// times so it can be set to false.
	///
	/// This prevents progress action components from having to figure it out themselves if they should
	/// be prematurely interrupted due to other progress actions on the same tile.
	/// </summary>
	public readonly bool InterruptsOverlapping;

	/// <summary>
	/// Whether turning is allowed during the action
	/// </summary>
	public readonly bool AllowTurning;

	public readonly bool AllowMovement;

	public readonly bool AllowDuringCuff;

	/// <summary>
	/// Creates a new progress action config with the indicated settings
	/// </summary>
	/// <param name="standardProgressActionType">What type of progress action this is for the purposes of determining if it
	/// is allowed to be started.</param>
	/// <param name="allowMultiple">Whether player is allowed to perform this action simultaneously on multiple DIFFERENT tiles.</param>
	/// <param name="interruptsOverlapping">When this is true, and 2 players perform a progress action of the same type on the same tile, when one player
	/// completes the action, the other players progress will be interrupted.</param>
	/// <param name="allowTurning">Whether turning is allowed during the action</param>
	public StandardProgressActionConfig(StandardProgressActionType standardProgressActionType,
		bool allowMultiple = false, bool interruptsOverlapping = true, bool allowTurning = true, bool allowMovement = false, bool allowDuringCuff = false)
	{
		StandardProgressActionType = standardProgressActionType;
		AllowMultiple = allowMultiple;
		InterruptsOverlapping = interruptsOverlapping;
		AllowTurning = allowTurning;
		AllowMovement = allowMovement;
		AllowDuringCuff = allowDuringCuff;
	}
}
