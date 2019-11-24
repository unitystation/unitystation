/// <summary>
/// Only used temporarily for cloth killer PR, for migrating to ClothingV2.
///
/// Important to note that anything with Server prefix is valid to call on server side only.
/// </summary>
public interface IClothing
{
	/// <summary>
	/// The current sprite data used for displaying this clothing's sprites
	/// </summary>
	SpriteData SpriteInfo { get; }

	//TODO: Refactor this, quite confusing
	/// <summary>
	/// The index of the sprites that should currently be used for rendering this, an index into SpriteData.List
	/// </summary>
	int SpriteInfoState { get; }

	/// <summary>
	/// Change the ClothingItem this clothing is linked to, so that the corresponding sprite renderer
	/// on the player object is updated appropriately.
	/// </summary>
	/// <param name="clothingItem"></param>
	void LinkClothingItem(ClothingItem clothingItem);
}
