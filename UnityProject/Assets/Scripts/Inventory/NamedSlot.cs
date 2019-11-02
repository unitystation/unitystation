
/// <summary>
/// All possible item slots. You MUST specify the ordinal otherwise it will break everything.
/// </summary>
public enum NamedSlot
{
	//player inventory stuff
	exosuit = 0,
	belt = 1,
	head = 2,
	feet = 3,
	face = 4,
	mask = 5,
	uniform = 6,
	leftHand = 7,
	rightHand = 8,
	eyes = 9,
	back = 10,
	hands = 11,
	ear = 12,
	neck = 13,
	handcuffs = 14,
	id = 15,
	storage01 = 16,
	storage02 = 17,
	suitStorage = 18,

	//alternative for non-nullable null value
	none = 2048,

}
