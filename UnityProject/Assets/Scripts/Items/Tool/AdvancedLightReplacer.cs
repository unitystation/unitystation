using System.Collections.Generic;
using Mirror;
using Objects.Lighting;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;

namespace Items.Tool
{
	public class AdvancedLightReplacer : NetworkBehaviour, ICheckedInteractable<HandActivate>, ICheckedInteractable<HandApply>, IHoverTooltip
	{
		private bool lightTuner = false;
		[SyncVar, SerializeField] private Color currentColor;

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (lightTuner && interaction.IsAltClick)
			{
				LightTunerWindowOpen();
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
			if (interaction.TargetObject.TryGetComponent<LightSource>(out var source) == false) return;
			if (lightTuner)
			{
				SetLightColors(source);
			}
			else
			{
				source.TryReplaceBulb(interaction);
			}
		}

		private void LightTunerWindowOpen()
		{
			UIManager.Instance.GlobalColorPicker.EnablePicker(SetColorToTune);
		}

		private void SetColorToTune(Color newColor)
		{
			currentColor = newColor;
		}

		private void SetLightColors(LightSource source)
		{
			source.ONColour = currentColor;
		}

		public string HoverTip()
		{
			return $"Lighter Tuner On: {lightTuner}";
		}

		public string CustomTitle() { return null; }

		public Sprite CustomIcon() { return null; }

		public List<Sprite> IconIndicators() { return null; }

		public List<TextColor> InteractionsStrings()
		{
			List<TextColor> interactions = new List<TextColor>();
			interactions.Add(new TextColor()
			{
				Text = "Alt+Click to change the tuner settings.",
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
			return interactions;
		}
	}
}