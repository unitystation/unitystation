
/// <summary>
/// Defines the different kinds of standard progress actions. This influences whether
/// the action can be performed based on when other actions of the same type are also
/// being performed in the same tile and/or by the same performer.
/// </summary>
public enum StandardProgressActionType
{
	Construction = 0,
	Restrain = 1,
	Uncuff = 2,
	SelfHeal = 3,
	CPR = 4,
	Disrobe = 5,
	Mop = 6,
	Escape = 7,
	Unbuckle = 8,
	ItemTransfer = 9,
	Craft = 10
}
