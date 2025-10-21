using System.Linq;
using Items;
using Objects.Construction;
using Objects.Machines;
using UnityEngine;

public class RPED : MonoBehaviour, ICheckedInteractable<HandApply>
{

	public ItemStorage ThisItemStorage;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (Validations.HasComponent<Machine>(interaction.TargetObject) == false &&
		    Validations.HasComponent<MachineFrame>(interaction.TargetObject) == false) return false;

		return true;
	}


	public void ServerPerformInteraction(HandApply interaction)
    {
        var target = interaction.TargetObject;

        if (Validations.HasComponent<Machine>(target))
        {
            var machine = target.GetComponent<Machine>();
            if (machine.GetPanelOpen())
            {
	            UpgradeMachineParts(machine, interaction);
            }
            else
            {
	            Chat.AddActionMsgToChat(
		            interaction.Performer,
		            $"You poke the {machine.gameObject.ExpensiveName()} with the {this.gameObject.ExpensiveName()} however the panel isn't open.",
		            $"{interaction.Performer.ExpensiveName()} pokes the {machine.gameObject.ExpensiveName()} with the {this.gameObject.ExpensiveName()} however the panel isn't open."
	            );
            }

        }
        else if (Validations.HasComponent<MachineFrame>(target))
        {
            var frame = target.GetComponent<MachineFrame>();
            UpgradeFrameParts(frame, interaction);
        }
    }

    private void UpgradeMachineParts(Machine machine,HandApply interaction )
    {
        var machineParts = machine.getObjectpartsInFrame;
        var rpedSlots = ThisItemStorage.GetItemSlots();

        foreach (var partRef in machineParts.ToList()) // Clone list because it may change
        {
            var oldTrait = partRef.itemTrait;
            var oldTier = partRef.tier;

            var oldItem = partRef.itemObject;

            // Find a better part inside RPED
            var betterPartSlot = rpedSlots.FirstOrDefault(slot =>
            {
                if (slot.Item == null) return false;
                var attr = slot.Item.GetComponentCustom<ItemAttributesV2>();
                if (attr.HasTrait(oldTrait) == false) return false;

                var stock = slot.Item.GetComponentCustom<StockTier>();
                return stock != null && stock.Tier > oldTier;
            });

            if (betterPartSlot == null)
                continue;

            // Perform the swap
            var newPart = betterPartSlot.Item.gameObject;

            Inventory.ServerSwap(partRef.Slot,betterPartSlot );

            Chat.AddActionMsgToChat(
	            interaction.Performer,
                $"The {this.gameObject.ExpensiveName()} replaces the old {oldItem.ExpensiveName()} part with {newPart.ExpensiveName()}.",
                $"{this.gameObject.ExpensiveName()} replaces the old {oldItem.ExpensiveName()} part with {newPart.ExpensiveName()}."
            );
        }

    }

    private void UpgradeFrameParts(MachineFrame frame, HandApply interaction )
    {
	    if (frame.MachineParts == null) return;
	    var frameParts = frame.MachineParts;
        var rpedSlots = ThisItemStorage.GetItemSlots();

        foreach (var missingPart in frameParts.machineParts)
        {
	        var AlreadyIn = frame.NumberOfPartsForTrait(missingPart.itemTrait);

	        if (AlreadyIn != missingPart.amountOfThisPart)
	        {
		        for (int i = 0; i < (missingPart.amountOfThisPart - AlreadyIn); i++)
		        {
			        //add
			        var PartSlot = rpedSlots.FirstOrDefault(slot =>
			        {
				        if (slot.Item == null) return false;
				        var attr = slot.Item.GetComponentCustom<ItemAttributesV2>();
				        if (attr.HasTrait(missingPart.itemTrait) == false) return false;

				        var stock = slot.Item.GetComponentCustom<StockTier>();
				        return stock != null;
			        });
			        if (PartSlot == null) continue;
			        frame.ItemStorage.ServerTryAdd(PartSlot.ItemObject);
			        Chat.AddActionMsgToChat(
				        interaction.Performer,
				        $"The {this.gameObject.ExpensiveName()}  adds a stock part {missingPart.itemTrait.name} to {frame.gameObject.ExpensiveName()}.",
				        $"{this.gameObject.ExpensiveName()}  adds a stock part {missingPart.itemTrait.name} to {frame.gameObject.ExpensiveName()}."
			        );
		        }
	        }
        }

        frame.CheckPartStage();
    }

}
