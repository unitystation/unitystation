
/// <summary>
/// Defines the different kinds of progress actions. Actions which share the same
/// type are not allowed to be started on the same tile.
/// </summary>
public enum ProgressActionType
{
	Construction = 0,
	Cuff = 1,
	Uncuff = 2,
	SelfHeal = 3,
	CPR = 4,
	Disrobe = 5,
	Mop = 6
}
