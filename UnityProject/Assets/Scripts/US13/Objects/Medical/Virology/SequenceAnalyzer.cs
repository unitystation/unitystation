using System.Collections.Generic;
using System.Text;
using Chemistry;
using Cysharp.Threading.Tasks;
using Mirror;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interactions.Internal;
using US13.Core.Lifecycle;
using US13.Core.ObjectConnection;
using US13.Core.Physics;
using US13.Core.Sprite_Handler;
using US13.HealthV2.Living.MedicalChemistry;
using US13.Items.Traits;
using US13.Managers;
using US13.Objects.Engineering;
using US13.Systems.Electricity.Interfaces;
using US13.Systems.Inventory;
using Util;

namespace US13.Objects.Medical.Virology
{
	public class SequenceAnalyzer : NetworkBehaviour, IAPCPowerable, IMultitoolSlaveable, IServerSpawn
	{
		[SyncVar] private PowerState _currentPowerState = PowerState.On;
		[SyncVar] private bool _isOnCooldown = false;
		[field: SyncVar] public bool IsFull { get; private set; } = false;

		private Reagent _activeSickness;
		public Reagent ActiveSickness => _activeSickness;

		[SerializeField] private ItemStorage dishItemStorage;
		[SerializeField] private UniversalObjectPhysics objectPhysics;

		[SerializeField] private SpriteHandler buttonSpriteHandler;
		[SerializeField] private SpriteHandler mainSpriteHandler;

		[SerializeField] private ItemTrait dishItemTrait;
		[SerializeField] private AddressableAudioSource machineBeepSound;

		public bool CanExamineSample => _isOnCooldown == false;

		public void RequestLoadRemoveDish(PositionalHandApply interaction)
		{
			if (interaction.HandObject&& Validations.HasItemTrait(interaction, dishItemTrait))
			{
				Inventory.ServerTransfer(interaction.HandSlot, dishItemStorage.GetIndexedItemSlot(0));
				IsFull = true;
				mainSpriteHandler.SetSpriteVariant(0);
				return;
			}
			if (interaction.HandObject) return;

			Inventory.ServerTransfer(dishItemStorage.GetIndexedItemSlot(0), interaction.HandSlot);
			mainSpriteHandler.SetSpriteVariant(1);
			IsFull = false;
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			mainSpriteHandler.SetSpriteVariant(1);
			buttonSpriteHandler.SetSpriteVariant(0);
		}

		public void RequestExamineDish(Interaction interaction)
		{
			if (CanExamineSample == false) return;

			_ = AnimateButtonPress();

			if (_currentPowerState != PowerState.On)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"{gameObject.ExpensiveName()} is unpowered!");
				return;
			}

			_ = SoundManager.PlayNetworkedAtPosAsync(machineBeepSound, objectPhysics.OfficialPosition);

			_activeSickness = null;
			if (IsFull == false)
			{
				Chat.AddWarningMsgFromServer(interaction.Performer, "No pathogen sample in sequence analyser!");
				return;
			}


			if(dishItemStorage.GetIndexedItemSlot(0).Item.TryGetCachedComponent<ReagentContainer>(out var container, includeDisabled: false) == false) return;

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
