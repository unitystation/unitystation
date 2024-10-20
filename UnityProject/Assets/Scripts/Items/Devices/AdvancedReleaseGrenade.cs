using System.Collections;
using Chemistry;
using Chemistry.Components;
using UnityEngine;

namespace Items.Weapons
{
	public class AdvancedReleaseGrenade : ChemicalGrenade
	{
		private int mixAmount = 1;
		private const int MAX_LOOP_COUNT = 30;

		public override void MixReagents()
		{
			if (isServer)
			{
				StartCoroutine(SmartRelease());
			}
		}

		IEnumerator SmartRelease()
		{
			int loopCount = 0;
			while (ReagentContainer1.ReagentMixTotal + ReagentContainer2.ReagentMixTotal > 0)
			{
				var worldPos = objectPhysics.registerTile.WorldPosition;

				BlastData blastData = new BlastData();

				ReagentMix mixA = ReagentContainer1.CurrentReagentMix.Take(mixAmount);
				ReagentMix mixB = ReagentContainer1.CurrentReagentMix.Take(mixAmount);

				float internalEnergy = mixA.InternalEnergy + mixB.InternalEnergy;

				ReagentContainer1.ReagentsChanged(true);
				ReagentContainer1.OnReagentMixChanged?.Invoke();
				ReagentContainer2.ReagentsChanged(true);
				ReagentContainer2.OnReagentMixChanged?.Invoke();

				mixedReagentContainer.CurrentReagentMix.InternalEnergy = internalEnergy;
				mixedReagentContainer.CurrentReagentMix.Temperature += TemperatureChange;
				mixedReagentContainer.ReagentsChanged(false, true); //We mix the the two containers, but cache the effects of the mixed container.

				blastData.ReagentMix = mixedReagentContainer.CurrentReagentMix.CloneWithCache();
				ExplosiveBase.ExplosionEvent.Invoke(worldPos, blastData);

				mixedReagentContainer.CurrentReagentMix.ApplyEffectCache(mixedReagentContainer);//We now apply the cache
				mixedReagentContainer.OnReagentMixChanged?.Invoke();
				spriteHandler.SetCatalogueIndexSprite(LOCKED_SPRITE);
				spriteHandler.SetSpriteVariant(EMPTY_VARIANT);
				mixedReagentContainer.Spill(objectPhysics.OfficialPosition.CutToInt(), DETONATE_SPILL_AMOUNT);

				loopCount++;
				if (loopCount >= MAX_LOOP_COUNT) break;

				yield return WaitFor.Seconds(1f);
			}
		}

		public override bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetSlot.Item.OrNull()?.gameObject != gameObject) return false;
			if (IsFullContainers && interaction.UsedObject != null)
			{
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) == false) return false;
				return true;
			}
			else if (ScrewedClosed == false)
			{
				if (interaction.UsedObject != null)
				{
					if (Validations.HasComponent<ReagentContainer>(interaction.UsedObject) == false) return false;
				}

				return true;
			}

			return false;
		}

		public override void ServerPerformInteraction(InventoryApply interaction)
		{
			if (IsFullContainers && Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				ScrewdriverInteraction(interaction);
				return;
			}
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Multitool))
			{
				MultitoolInteraction(interaction);
				return;
			}

			if (ScrewedClosed == true) return;

			BeakerTransferInteraction(interaction);


			UpdateSprite(UNLOCKED_SPRITE);
		}

		private void ScrewdriverInteraction(InventoryApply interaction)
		{
			ScrewedClosed = !ScrewedClosed;

			if (ScrewedClosed) UpdateSprite(LOCKED_SPRITE);
			else UpdateSprite(UNLOCKED_SPRITE);


			var StateText = ScrewedClosed ? "Closed" : "Open";
			Chat.AddActionMsgToChat(interaction, $" you screw the {gameObject.ExpensiveName()} {StateText}",
				$" {interaction.Performer.ExpensiveName()} screws the {gameObject.ExpensiveName()} {StateText}");
		}

		private void MultitoolInteraction(InventoryApply interaction)
		{
			if (mixAmount < 5) mixAmount++;
			else if (mixAmount < 10) mixAmount += 5;
			else mixAmount = 1;

			Chat.AddActionMsgToChat(interaction, $"you adjust the mix amount of the {gameObject.ExpensiveName()} to {mixAmount}u",
				$" {interaction.Performer.ExpensiveName()} adjusts the mix amout of the {gameObject.ExpensiveName()} to {mixAmount}u");
		}

		private void BeakerTransferInteraction(InventoryApply interaction)
		{
			if (interaction.UsedObject != null)
			{
				ItemSlot targetSlot = null;

				if (containerStorage.GetIndexedItemSlot(0).Item == null) targetSlot = containerStorage.GetIndexedItemSlot(0);
				else if (containerStorage.GetIndexedItemSlot(1).Item == null) targetSlot = containerStorage.GetIndexedItemSlot(1);

				if (targetSlot != null) Inventory.ServerTransfer(interaction.FromSlot, targetSlot);
			}
			else
			{
				ItemSlot fromSlot = null;

				if (containerStorage.GetIndexedItemSlot(1).Item != null) fromSlot = containerStorage.GetIndexedItemSlot(1);
				else if (containerStorage.GetIndexedItemSlot(0).Item != null) fromSlot = containerStorage.GetIndexedItemSlot(0);

				if (fromSlot != null) Inventory.ServerTransfer(fromSlot, interaction.FromSlot);
			}
		}

	}
}