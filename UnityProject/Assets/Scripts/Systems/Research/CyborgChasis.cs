using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UI.Systems.Tooltips.HoverTooltips;

public class CyborgChasis : MonoBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<InventoryApply>, IHoverTooltip
{
	[SerializeField] private GameObject bodyWithChasis;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		if (!Validations.IsTarget(gameObject, interaction)) return false;

		return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver);	
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		Spawn.ServerPrefab(bodyWithChasis, SpawnDestination.At(this.gameObject));

		Despawn.ServerSingle(this.gameObject);
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		if (!Validations.IsTarget(gameObject, interaction)) return false;

		return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver);
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		Spawn.ServerPrefab(bodyWithChasis, SpawnDestination.At(interaction.Performer));

		Despawn.ServerSingle(this.gameObject);
	}

	public string HoverTip()
	{
		return null;
	}

	public string CustomTitle()
	{
		return null;
	}

	public Sprite CustomIcon()
	{
		return null;
	}

	public List<Sprite> IconIndicators()
	{
		return null;
	}

	private bool LocalPlayerHasScrewdriver()
	{
		if (PlayerManager.LocalPlayerScript == null) return false;
		if (PlayerManager.LocalPlayerScript.DynamicItemStorage == null) return false;
		foreach (var slot in PlayerManager.LocalPlayerScript.DynamicItemStorage.GetHandSlots())
		{
			if (slot.IsEmpty) continue;
			if (slot.ItemAttributes.GetTraits().Contains(CommonTraits.Instance.Screwdriver)) return true;
		}

		return false;
	}

	public List<TextColor> InteractionsStrings()
	{
		List<TextColor> interactions = new List<TextColor>();
		if (LocalPlayerHasScrewdriver())
		{
			TextColor text = new TextColor
			{
				Text = "Left-Click with Screwdriver: Secure chasis.",
				Color = IntentColors.Help
			};
			interactions.Add(text);
		}
		
		return interactions;
	}
}
