
using System.Collections.Generic;

/// <summary>
/// Only used temporarily for cloth killer PR, for migrating to ItemAttributesV2.
///
/// Important to note that anything with Server prefix is valid to call on server side only.
/// </summary>
public interface IItemAttributes
{
	/// <summary>
	/// Get name of the item
	/// </summary>
	string ItemName { get; }

	/// <summary>
	/// Change the item name, valid server side only
	/// </summary>
	/// <param name="newName"></param>
	void ServerSetItemName(string newName);

	/// <summary>
	/// Change the item description, valid server side only
	/// </summary>
	/// <param name="desc"></param>
	void ServerSetItemDescription(string desc);


	/// <summary>
	/// Get current size.
	/// </summary>
	ItemSize Size { get; }
	/// <summary>
	/// Change the size, valid server side only.
	/// </summary>
	/// <param name="newSize"></param>
	void ServerSetSize(ItemSize newSize);

	float ServerHitDamage { get; set;  }
	DamageType ServerDamageType { get; set; }
	float ServerThrowSpeed { get; set; }
	float ServerThrowRange { get; set; }
	float ServerThrowDamage { get; set; }
	string ServerHitSound { get; set; }

	/// <summary>
	/// Possible verbs used when attacking via melee.
	/// </summary>
	IEnumerable<string> ServerAttackVerbs { get; set; }

	bool IsEVACapable { get; }
	bool CanConnectToTank { get; }

	/// <summary>
	/// Cached sprite data handler of this object
	/// </summary>
	SpriteDataHandler SpriteDataHandler { get; }

	/// <summary>
	/// Adds the trait dynamically
	/// NOTE: Not synced between client / server
	/// </summary>
	/// <param name="itemTrait"></param>
	void AddTrait(ItemTrait itemTrait);

	/// <summary>
	/// Removes the trait dynamically
	/// NOTE: Not synced between client / server
	/// </summary>
	/// <param name="itemTrait"></param>
	void RemoveTrait(ItemTrait itemTrait);

	/// <summary>
	/// Does it have the given trait?
	/// NOTE: Dynamically added / removed traits are not synced between client / server
	/// </summary>
	/// <param name="itemTrait"></param>
	/// <returns></returns>
	bool HasTrait(ItemTrait itemTrait);

	/// <summary>
	/// All traits currently on the item.
	/// NOTE: Dynamically added / removed traits are not synced between client / server
	/// </summary>
	/// <returns></returns>
	IEnumerable<ItemTrait> GetTraits();
}
