
/// <summary>
/// Defines each of the possible progress actions.
/// </summary>
public sealed class ProgressAction
{
	/// <summary>
	/// Includes construction / deconstruction. Anything that does something of this nature should
	/// use this (like mining).
	/// </summary>
	public static readonly ProgressAction Construction = new ProgressAction(allowMultiple: true);
	public static readonly ProgressAction Restrain = new ProgressAction();
	public static readonly ProgressAction Uncuff = new ProgressAction();
	public static readonly ProgressAction SelfHeal = new ProgressAction();
	public static readonly ProgressAction CPR = new ProgressAction();
	public static readonly ProgressAction Mop = new ProgressAction(false, allowMultiple: true);

	/// <summary>
	/// When this is true, and 2 players do the same ProgressAction on the same tile, when one player
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

	/// <summary>
	/// Whether player is allowed to perform this action simultaneously on multiple different tiles.
	/// </summary>
	public readonly bool AllowMultiple;


	private ProgressAction(bool interruptsOverlapping = true, bool allowTurning = true, bool allowMultiple = false)
	{
		InterruptsOverlapping = interruptsOverlapping;
		AllowTurning = allowTurning;
		AllowMultiple = allowMultiple;
	}
}
