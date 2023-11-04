using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mirror;
using Objects.Lighting;
using ScriptableObjects;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;

namespace Items.Tool
{
	public class AdvancedLightReplacer : NetworkBehaviour, ICheckedInteractable<HandActivate>, ICheckedInteractable<HandApply>, IHoverTooltip
	{
		private bool lightTuner = false;
		[SyncVar, SerializeField] private Color currentColor;
		[SerializeField] private ItemStorage storage;

		private void Awake()
		{
			if (storage == null) storage = GetComponent<ItemStorage>();
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (lightTuner && interaction.IsAltClick)
			{
				LightTunerWindowOpen(interaction.PerformerPlayerScript.netIdentity.connectionToClient);
				return;
			}
			lightTuner = !lightTuner;
			var text = lightTuner ? "will tune lights now." : "will replace lights now.";
			Chat.AddExamineMsg(interaction.Performer, $"this {gameObject.ExpensiveName()} {text}");
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (gameObject.PickupableOrNull()?.ItemSlot == null) return false;
			return interaction.TargetObject != null && DefaultWillInteract.Default(interaction, side);
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.TargetObject, CommonTraits.Instance.LightBulb) ||
			    Validations.HasItemTrait(interaction.TargetObject, CommonTraits.Instance.LightTube))
			{
				storage.ServerTryAdd(interaction.TargetObject);
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You watch as the tool automatically pulls out a mechanical arm that slots in the {interaction.TargetObject.ExpensiveName()}",
					$"{interaction.PerformerPlayerScript.visibleName} slots in the {interaction.TargetObject.ExpensiveName()} using the {gameObject.ExpensiveName()}.");
				return;
			}
			if (interaction.TargetObject.TryGetComponent<LightSource>(out var source) == false) return;
			if (lightTuner)
			{
				SetLightColors(source);
			}
			else
			{
				var target = storage.GetTopOccupiedIndexedSlot();
				if (target == null)
				{
					source.TryReplaceBulb(interaction);
					return;
				}
				if (target.ItemAttributes.GetTraits().Contains(source.TraitRequired) == false) return;
				source.TryReplaceBulb(interaction);
				AddLightToFixture(target, source, interaction);
				Chat.AddExamineMsg(interaction.Performer, "You replace the light-bulb with another one.");
			}
		}

		private void AddLightToFixture(ItemSlot target, LightSource source, HandApply interaction)
		{
			target.ItemStorage.ServerTryRemove(target.ItemObject, false, interaction.PerformerPlayerScript.AssumedWorldPos);
			source.TryAddBulb(target.ItemObject);
		}

		[TargetRpc]
		private void LightTunerWindowOpen(NetworkConnection target)
		{
			UIManager.Instance.GlobalColorPicker.CurrentColor = currentColor;
			UIManager.Instance.GlobalColorPicker.EnablePicker(SetColorToTuneWapper);
		}

		[Command(requiresAuthority = false)]
		private void SetColorToTune(Color newColor, NetworkConnectionToClient sender = null)
		{
			if (sender == null) return;
			if (Validations.CanApply(PlayerList.Instance.Get(sender).Script, this.gameObject, NetworkSide.Server, false, ReachRange.Standard) == false) return;
			if (gameObject.PickupableOrNull().ItemSlot == null) return;
			if (gameObject.PickupableOrNull().ItemSlot.Player == null) return;
			currentColor = newColor;
		}

		private void SetColorToTuneWapper(Color newColor)
		{
			SetColorToTune(newColor);
		}

		private void SetLightColors(LightSource source)
		{
			source.SetAnimation(source.CurrentOnColor, currentColor);
			source.CurrentOnColor = currentColor;
		}

		public string HoverTip()
		{
			StringBuilder statusText = new StringBuilder();
			statusText.AppendLine($"Lighter Tuner On: {lightTuner}");
			statusText.AppendLine($"Slots used: {storage.GetOccupiedSlots().Count} out of {storage.ItemStorageStructure.IndexedSlots}");
			return statusText.ToString();
		}

		public string CustomTitle() => null;

		public Sprite CustomIcon() => null;

		public List<Sprite> IconIndicators() => null;

		public List<TextColor> InteractionsStrings()
		{
			List<TextColor> interactions = new List<TextColor>();
			interactions.Add(new TextColor()
			{
				Text = $"Alt+Click or Alt + {KeybindManager.Instance.userKeybinds[KeyAction.HandActivate].PrimaryCombo} to change the tuner settings.",
				Color = Color.green,
			});
			interactions.Add(new TextColor()
			{
				Text = $"{KeybindManager.Instance.userKeybinds[KeyAction.HandActivate].PrimaryCombo} or click on it while in your hand to change its mode.",
				Color = Color.green,
			});
			interactions.Add(new TextColor()
			{
				Text = "Click on a nearby light fixture to interact with it.",
				Color = Color.green,
			});
			interactions.Add(new TextColor()
			{
				Text = "Click on a nearby light bulbs to load them in.",
				Color = Color.green,
			});
			return interactions;
		}
	}
}