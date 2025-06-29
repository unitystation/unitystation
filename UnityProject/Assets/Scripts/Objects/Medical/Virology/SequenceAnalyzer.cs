using System.Collections.Generic;
using System.Text;
using AddressableReferences;
using Chemistry;
using Chemistry.Components;
using Core.Physics;
using Cysharp.Threading.Tasks;
using HealthV2.Sickness;
using Mirror;
using Shared.Systems.ObjectConnection;
using UnityEngine;
using Systems.Electricity;

namespace Objects.Medical.Virology
{
	public class SequenceAnalyzer : NetworkBehaviour, IAPCPowerable, IMultitoolSlaveable, IServerSpawn
	{
		[SyncVar] private PowerState _currentPowerState = PowerState.On;
		[SyncVar] private bool _isOnCooldown = false;

		private Reagent _activeSickness;
		public Reagent ActiveSickness => _activeSickness;

		[SerializeField] private ItemStorage dishItemStorage;
		[SerializeField] private UniversalObjectPhysics objectPhysics;

		[SerializeField] private SpriteHandler buttonSpriteHandler;
		[SerializeField] private SpriteHandler mainSpriteHandler;

		[SerializeField] private ItemTrait dishItemTrait;
		[SerializeField] private AddressableAudioSource machineBeepSound;

		private ItemSlot DishItemSlot => dishItemStorage.GetIndexedItemSlot(0);

		public bool CanExamineSample => _isOnCooldown == false;
		public bool HasDishLoaded => DishItemSlot.IsOccupied;

		public void RequestLoadRemoveDish(PositionalHandApply interaction)
		{
			if (interaction.HandObject&& Validations.HasItemTrait(interaction, dishItemTrait))
			{
				Inventory.ServerTransfer(interaction.HandSlot, DishItemSlot);
				mainSpriteHandler.SetSpriteVariant(0);
				return;
			}
			if (interaction.HandObject) return;

			Inventory.ServerTransfer(DishItemSlot, interaction.HandSlot);
			mainSpriteHandler.SetSpriteVariant(1);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			mainSpriteHandler.SetSpriteVariant(1);
			buttonSpriteHandler.SetSpriteVariant(0);
		}

		public void RequestExamineDish(Interaction interaction)
		{
			_ = AnimateButtonPress();

			if (_currentPowerState != PowerState.On)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"{gameObject.ExpensiveName()} is unpowered!");
				return;
			}

			_ = SoundManager.PlayNetworkedAtPosAsync(machineBeepSound, objectPhysics.OfficialPosition);

			_activeSickness = null;
			if (CanExamineSample == false) return;
			if (DishItemSlot.IsEmpty)
			{
				Chat.AddWarningMsgFromServer(interaction.Performer, "No pathogen sample in sequence analyser!");
				return;
			}


			if(DishItemSlot.ItemObject.TryGetComponent<ReagentContainer>(out var container) == false) return;

			StringBuilder machineDialogue = new StringBuilder();
			foreach (KeyValuePair<Reagent, CureManager.Cure> curePair in CureManager.InitialisedSicknesses)
			{
				if (container.CurrentReagentMix.reagents.ContainsKey(curePair.Key) == false) continue;
				_activeSickness = curePair.Key;
				machineDialogue.AppendLine(
					$"Sickness {curePair.Key.Name} has been identified in the sample.\nFormulating possible cure reagents:");
				AddCluesToStringBuilder(in machineDialogue, curePair.Value);

			}
			if(_activeSickness is not null) machineDialogue.AppendLine($"Sickness {_activeSickness.Name} is registered as active disease.");

			Chat.AddCommMsgByMachineToChat(gameObject,
				_activeSickness is null ? "No sickness was identified in the provided sample." : machineDialogue.ToString(),
				ChatChannel.Local, Loudness.NORMAL);
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
			buttonSpriteHandler.SetSpriteVariant(0);
			await UniTask.Delay(200);
			buttonSpriteHandler.SetSpriteVariant(1);
			_isOnCooldown = false;
		}

		public void StateUpdate(PowerState state)
		{
			_currentPowerState = state;
			buttonSpriteHandler.SetSpriteVariant(state != PowerState.On ? 0 : 1);
		}

		public void PowerNetworkUpdate(float voltage)
		{
			//No required logic for input voltage
		}

		public MultitoolConnectionType ConType { get; } = MultitoolConnectionType.CureTester;
		public bool CanRelink { get; } = true;
		public IMultitoolMasterable Master { get; } = null;

		public bool TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			if (master is not CureTester tester) return false;

			tester.SetAnalyzer(this);
			return true;
		}

		public void SetMasterEditor(IMultitoolMasterable master)
		{
			if (master is not CureTester tester) return;

			tester.SetAnalyzer(this);
		}

		public bool RequireLink { get; } = false;
	}
}
