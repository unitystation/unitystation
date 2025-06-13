using System.Collections.Generic;
using System.Text;
using AddressableReferences;
using Chemistry;
using Chemistry.Components;
using Core.Physics;
using Cysharp.Threading.Tasks;
using HealthV2.Sickness;
using UnityEngine;
using Systems.Electricity;

namespace Objects.Medical.Virology
{
	public class SequenceAnalyzer : MonoBehaviour, IAPCPowerable
	{
		private PowerState currentPowerState;

		[SerializeField] private ItemStorage dishItemStorage = null;
		[SerializeField] private UniversalObjectPhysics objectPhysics = null;

		[SerializeField] private SpriteHandler buttonSpriteHandler = null;
		[SerializeField] private SpriteHandler mainpriteHandler = null;

		[SerializeField] private ItemTrait dishItemTrait;
		[SerializeField] private AddressableAudioSource machineBeepSound = null;

		private ItemSlot DishItemSlot => dishItemStorage.GetIndexedItemSlot(0);
		private bool _isOnCooldown = false;

		public bool CanExamineSample => _isOnCooldown == false && currentPowerState == PowerState.On;
		public bool HasDishLoaded => DishItemSlot.IsOccupied;

		public void RequestLoadRemoveDish(PositionalHandApply interaction)
		{
			if (interaction.HandObject&& Validations.HasItemTrait(interaction, dishItemTrait))
			{
				Inventory.ServerTransfer(interaction.HandSlot, DishItemSlot);
				mainpriteHandler.SetSpriteVariant(1);
				return;
			}
			if (interaction.HandObject) return;

			Inventory.ServerTransfer(DishItemSlot, interaction.HandSlot);
			mainpriteHandler.SetSpriteVariant(0);
		}

		public void RequestExamineDish()
		{
			if (DishItemSlot.IsEmpty || CanExamineSample == false) return;

			if(DishItemSlot.ItemObject.TryGetComponent<ReagentContainer>(out var container) == false) return;

			_ = SoundManager.PlayNetworkedAtPosAsync(machineBeepSound, objectPhysics.OfficialPosition);
			_ = AnimateButtonPress();

			StringBuilder machineDialogue = new StringBuilder();
			bool sampleHasSickness = false;
			foreach (KeyValuePair<Reagent, CureManager.Cure> curePair in CureManager.InitialisedSicknesses)
			{
				if (container.CurrentReagentMix.reagents.ContainsKey(curePair.Key) == false) continue;
				sampleHasSickness = true;
				machineDialogue.AppendLine(
					$"Sickness {curePair.Key.Name} has been identified in the sample.\nFormulating possible cure reagents:");
				AddCluesToStringBuilder(in machineDialogue, curePair.Value);
			}

			Chat.AddLocalMsgToChat(
				sampleHasSickness == false
					? "No sickness was identified in the provided sample."
					: machineDialogue.ToString(), gameObject, doSpeechBubble: false);
		}

		private void AddCluesToStringBuilder(in StringBuilder builder, CureManager.Cure cure)
		{
			foreach (Reagent cureReagent in cure.ClueReagents)
			{
				builder.AppendLine($"- {cureReagent.Name}");
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
