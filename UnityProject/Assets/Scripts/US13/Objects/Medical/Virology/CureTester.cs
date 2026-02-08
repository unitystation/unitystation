using System.Text;
using Chemistry;
using Cysharp.Threading.Tasks;
using Logs;
using Mirror;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
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
	public class CureTester : NetworkBehaviour, IAPCPowerable, IMultitoolMasterable, IServerSpawn
	{
		[field: SyncVar] public bool IsFull { get; private set; } = false;
		[SyncVar] private PowerState _currentPowerState;

		[SerializeField] private SequenceAnalyzer connectedSequenceAnalyzer;

		[SerializeField] private SpriteHandler sampleSpriteHandler;
		[SerializeField] private SpriteHandler buttonSpriteHandler;
		[SerializeField] private ItemStorage itemStorage;

		[SerializeField] private UniversalObjectPhysics objectPhysics;
		[SerializeField] private AddressableAudioSource machineBeepSound;

		private bool _isOnCooldown;
		public bool CanExamineSample => _isOnCooldown == false;

		private void Start()
		{
			if (itemStorage == null)
			{
				Loggy.Error($"No Item Storage set on {gameObject.ExpensiveName()}!");
				return;
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			sampleSpriteHandler.SetSpriteVariant(1);
			buttonSpriteHandler.SetSpriteVariant(0);
		}

		public void RequestLoadRemoveItem(PositionalHandApply interaction, ItemTrait requiredTrait)
		{
			if (_currentPowerState != PowerState.On)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"{gameObject.ExpensiveName()} is unpowered!");
				return;
			}

			if (interaction.HandObject&& Validations.HasItemTrait(interaction, requiredTrait))
			{
				Inventory.ServerTransfer(interaction.HandSlot, itemStorage.GetIndexedItemSlot(0), ReplacementStrategy.DropOther);

				IsFull = true;
				sampleSpriteHandler.SetSpriteVariant(0);
				Chat.AddActionMsgToChat(interaction.Performer,
						$"You place the {interaction.HandObject.ExpensiveName()} into the tester's primary slot.",
						$"{interaction.Performer.ExpensiveName()} places the {interaction.HandObject.ExpensiveName()} into the tester's primary slot.");
				return;
			}
			if (interaction.HandObject) return;

			IsFull = false;
			Inventory.ServerTransfer(itemStorage.GetIndexedItemSlot(0), interaction.HandSlot);
			sampleSpriteHandler.SetSpriteVariant(1);
		}

		public void RequestExamineCure(PositionalHandApply interaction)
		{
			if (_isOnCooldown) return;

			if (_currentPowerState != PowerState.On)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"{gameObject.ExpensiveName()} is unpowered!");
				return;
			}

			_ = AnimateButtonPress();
			_ = SoundManager.PlayNetworkedAtPosAsync(machineBeepSound, objectPhysics.OfficialPosition);

			if (connectedSequenceAnalyzer is null)
			{
				Chat.AddWarningMsgFromServer(interaction.Performer, "Requires connection to a sequence analyzer!");
				return;
			}

			if (connectedSequenceAnalyzer.ActiveSickness is null)
			{
				Chat.AddWarningMsgFromServer(interaction.Performer, "No pathogen sample in sequence analyser!");
				return;
			}

			if (itemStorage.GetIndexedItemSlot(0).ItemObject?.TryGetComponent<ReagentContainer>(out var container) == true)
			{
				StringBuilder machineDialogue = new StringBuilder();
				machineDialogue.AppendLine($"Test results of cure against sickness {connectedSequenceAnalyzer.ActiveSickness.Name}: ");
				TestCure(in machineDialogue, container.CurrentReagentMix);
				Chat.AddCommMsgByMachineToChat(gameObject, machineDialogue.ToString(), ChatChannel.Local, Loudness.NORMAL);
			}
			else Chat.AddWarningMsgFromServer(interaction.Performer, "No cure was found to test!");
		}

		private void TestCure(in StringBuilder machineDialogue, ReagentMix proposedCure)
		{
			CureManager.Cure activeCure = CureManager.InitialisedSicknesses[connectedSequenceAnalyzer.ActiveSickness];

			int numberOfMatches = 0;
			if (proposedCure.reagents.Contains(activeCure.CureReagentA)) numberOfMatches++;
			if (proposedCure.reagents.Contains(activeCure.CureReagentB)) numberOfMatches++;
			if (proposedCure.reagents.Contains(activeCure.InhibitorReagentA)) numberOfMatches--;
			if (proposedCure.reagents.Contains(activeCure.InhibitorReagentB)) numberOfMatches--;

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
			buttonSpriteHandler.SetSpriteVariant(2);
			_isOnCooldown = false;
		}

		public void StateUpdate(PowerState state)
		{
			_currentPowerState = state;
			buttonSpriteHandler.SetSpriteVariant(state != PowerState.On ? 0 : 2);
		}

		public void PowerNetworkUpdate(float voltage)
		{
			//No required logic for input voltage
		}

		public void SetAnalyzer(SequenceAnalyzer analyzer)
		{
			connectedSequenceAnalyzer = analyzer;
		}

		public MultitoolConnectionType ConType { get; } = MultitoolConnectionType.CureTester;
		public bool CanRelink { get; } = true;
		public int MaxDistance { get; } = 30;
		public bool IgnoreMaxDistanceMapper { get; } = true;
	}
}
