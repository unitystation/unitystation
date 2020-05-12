using Chemistry.Components;
using UnityEngine;

/// <summary>
/// The component used on containers/reagentcontainers that can determine if they can be crafted into a simplebot and what type of bot
/// </summary>
public class ContainerConstruct : MonoBehaviour, ICheckedInteractable<HandApply>
{
	[Tooltip("Check this if the assembly should use a certain amount of materials/stackable items")]
	public bool stackableMaterials;// A simple bool that will have the script consume a set ammount of a stackable item

	[Tooltip("If stackableMaterials is checked type in how many of that material it should use")]
	public int materialCost;// How much of the stack of items should be consumed if stackableMaterials is true

	[Tooltip("Check this if the 'container' uses a reagentcontainer instead of an itemstorage component")]
	public bool reagentContainer;// A simple bool to switch to using reagentcontainer instead of itemstorage

	[Tooltip("Put the item(s) in that should be used to spawn assembly, Note: A default simplebot should have 2 elements for both borg arms")]
	public GameObject[] craftItem; // The items that can be used to spawn the assembly

	[Tooltip("Put what assembly should spawn once item is given")]
	public GameObject assembly; // The assembly that should spawn

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		var hand = interaction.HandObject != null ? interaction.HandObject : null;
		if (hand == null)
		{
			return false;
		}

		// Checks to see if reagentContainer is marked true
		if (reagentContainer != true)
		{
			// If not marked true (false) then the script will try and access ItemStorage
			var storage = gameObject.GetComponent<ItemStorage>()?.GetItemSlots();
			foreach (var slot in storage)
			{
				if (slot.Item != null)
				{
					return false;
				}
			}
		}
		else
		{
			// If marked true then it will try and access the ReagentContainer
			var storage = gameObject.GetComponent<ReagentContainer>()?.IsEmpty;
			if (storage != true)
			{
				return false;
			}
		}
		// Checks if one of the items in the list is in active hand
		foreach (var neededObject in craftItem)
		{
			if (hand.GetComponent<ItemAttributesV2>()?.InitialName == neededObject.GetComponent<ItemAttributesV2>().InitialName)
			{   // This will activate if the component has been set to use a stackable item
				if (stackableMaterials)
				{
					if (hand.GetComponent<Stackable>().Amount >= materialCost)
					{
						return true;
					}
					else return false;
				}
				else return true;
			}
		}
		return false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		// Checks to see if stackableMaterials is not true (false) if so just despawn the item in hand
		if (stackableMaterials != true)
		{
			Inventory.ServerDespawn(interaction.HandObject);
		}
		else
		{   // If stackableMaterials IS true then consume the amount set by material cost
			interaction.HandObject.GetComponent<Stackable>().ServerConsume(materialCost);
		}
		// Spawns assembly and despawns self
		Spawn.ServerPrefab(assembly, gameObject.RegisterTile().WorldPosition, transform.parent, count: 1);
		Despawn.ServerSingle(gameObject);
	}
}