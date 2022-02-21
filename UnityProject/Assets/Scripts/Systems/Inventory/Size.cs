/// <summary>
/// Sizes:
/// Tiny - pen, coin, pills. Anything you'd easily lose in a couch.
/// Small - Pocket-sized items. You could hold a couple in one hand, but ten would be a hassle without a bag. Apple, phone, drinking glass etc.
/// Medium - default size. Fairly bulky but stuff you could carry in one hand and stuff into a backpack. Most tools would fit this size.
/// Large - particularly long or bulky items that would need a specialised bag to carry them. A shovel, a snowboard etc or wall mounts, kitchen appliance.
/// Huge - Think, like, a fridge. Absolute unit. You aren't stuffing this into anything less than a shipping crate or plasma generator.
/// Massive - Particle accelerator piece, takes up the entire tile.
/// Humongous - Multi-block/Sprite stretches across multiple tiles structures such as the gateway
/// </summary>
public enum Size
{
	//w_class
	None = 0,
	Tiny = 1,
	Small = 2,
	Medium = 3, //Normal
	Large = 4, //Bulky
	Huge = 5,
	Massive = 6,
	Humongous = 7,
}