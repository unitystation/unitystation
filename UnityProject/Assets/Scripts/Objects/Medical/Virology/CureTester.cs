using Chemistry;
using HealthV2.Sickness;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using AddressableReferences;
using Chemistry.Components;
using Core.Physics;
using Cysharp.Threading.Tasks;
using Logs;
using Systems.Electricity;

namespace Objects.Medical.Virology
{
	public class CureTester : MonoBehaviour, IAPCPowerable
	{
		private PowerState currentPowerState;

		[SerializeField] private SpriteHandler dishSpriteHandler = null;
		[SerializeField] private SpriteHandler sampleSpriteHandler = null;
		[SerializeField] private SpriteHandler buttonSpriteHandler = null;
		[SerializeField] private ItemStorage itemStorage = null;

		[SerializeField] private UniversalObjectPhysics objectPhysics = null;
		[SerializeField] private AddressableAudioSource machineBeepSound = null;

		public ItemSlot CureItemSlot { get; private set; } = null;
		public ItemSlot DishItemSlot { get; private set; } = null;


		private Reagent _activeSicknessReagent = null;
		private CureManager.Cure ActiveCure => CureManager.InitialisedSicknesses[_activeSicknessReagent];

		private bool _isOnCooldown = false;
		public bool CanExamineSample => _isOnCooldown == false && currentPowerState == PowerState.On;

		private void Start()
		{
			if (itemStorage == null)
			{
				Loggy.Error($"[CureTester/Start] No Item Storage set on {gameObject.ExpensiveName()}!");
				return;
			}

			CureItemSlot = itemStorage.GetIndexedItemSlot(1);
			DishItemSlot = itemStorage.GetIndexedItemSlot(0);
		}

		public void RequestLoadRemoveItem(PositionalHandApply interaction, ItemSlot slot, ItemTrait requiredTrait)
		{
			if (interaction.HandObject&& Validations.HasItemTrait(interaction, requiredTrait))
			{
				Inventory.ServerTransfer(interaction.HandSlot, slot);
				if (slot == DishItemSlot)
				{
					dishSpriteHandler.SetSpriteVariant(1);
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You place the {interaction.HandObject.ExpensiveName()} into the tester's blood sampler.",
						$"{interaction.Performer.ExpensiveName()} places the {interaction.HandObject.ExpensiveName()} into the tester's blood sampler.");
				}
				else
				{
					sampleSpriteHandler.SetSpriteVariant(1);
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You place the {interaction.HandObject.ExpensiveName()} into the tester's primary slot.",
						$"{interaction.Performer.ExpensiveName()} places the {interaction.HandObject.ExpensiveName()} into the tester's primary slot.");
				}

				return;
			}
			if (interaction.HandObject) return;

			Inventory.ServerTransfer(slot, interaction.HandSlot);
			if (slot == DishItemSlot)
			{
				dishSpriteHandler.SetSpriteVariant(0);
				_activeSicknessReagent = null;
			}
			else sampleSpriteHandler.SetSpriteVariant(0);
		}

		private bool RequestFindSicknessInSample(PositionalHandApply interaction)
		{
			if (DishItemSlot.ItemObject?.TryGetComponent<ReagentContainer>(out var container) == true) return FindSicknessInSample(container.CurrentReagentMix);

			Chat.AddWarningMsgFromServer(interaction.Performer, "No sample was found to test!");
			return false;
		}

		private bool FindSicknessInSample(ReagentMix currentMix)
		{
			foreach (KeyValuePair<Reagent, CureManager.Cure> curePair in CureManager.InitialisedSicknesses)
			{
				if (currentMix.reagents.ContainsKey(curePair.Key) == false) continue;

				_activeSicknessReagent = curePair.Key;
				return true;
			}

			Chat.AddLocalMsgToChat("No sickness was identified in the provided sample.", gameObject,
				doSpeechBubble: false);
			_activeSicknessReagent = null;

			return false;
		}

		public void RequestExamineCure(PositionalHandApply interaction)
		{
			_ = SoundManager.PlayNetworkedAtPosAsync(machineBeepSound, objectPhysics.OfficialPosition);
			_ = AnimateButtonPress();

			if(RequestFindSicknessInSample(interaction) == false) return;

			if (CureItemSlot.ItemObject?.TryGetComponent<ReagentContainer>(out var container) == true)
			{
				StringBuilder machineDialogue = new StringBuilder();
				machineDialogue.AppendLine($"Test results of cure against sickness {_activeSicknessReagent.Name}: ");
				TestCure(in machineDialogue, container.CurrentReagentMix);
				Chat.AddLocalMsgToChat(machineDialogue.ToString(), gameObject, doSpeechBubble: false);
			}
			else Chat.AddWarningMsgFromServer(interaction.Performer, "No cure was found to test!");
		}

		private void TestCure(in StringBuilder machineDialogue, ReagentMix proposedCure)
		{
			int numberOfMatches = 0;
			if (proposedCure.reagents.Contains(ActiveCure.CureReagentA)) numberOfMatches++;
			if (proposedCure.reagents.Contains(ActiveCure.CureReagentB)) numberOfMatches++;
			if (proposedCure.reagents.Contains(ActiveCure.InhibitorReagentA)) numberOfMatches--;
			if (proposedCure.reagents.Contains(ActiveCure.InhibitorReagentB)) numberOfMatches--;

			switch (numberOfMatches)
			{
				case 1:
					machineDialogue.AppendLine("The pathogen showed signs of weakening, but remained prevalent");
					break;
				case 2:
					machineDialogue.AppendLine("The pathogen was severely degraded by the chemical mix");
					break;
				default:
					machineDialogue.AppendLine("No change in the pathogen was observed");
					break;
			}
		}

		private async UniTask AnimateButtonPress()
		{
			_isOnCooldown = true;
			buttonSpriteHandler.SetSpriteVariant(1);
			await UniTask.Delay(200);
			buttonSpriteHandler.SetSpriteVariant(0);
			_isOnCooldown = false;
		}

		public void StateUpdate(PowerState state)
		{
			currentPowerState = state;
		}

		public void PowerNetworkUpdate(float voltage)
		{
			//No required logic for input voltage
		}
	}
}
