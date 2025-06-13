using Chemistry;
using HealthV2.Sickness;
using UnityEngine;
using System.Text;
using AddressableReferences;
using Chemistry.Components;
using Core.Physics;
using Cysharp.Threading.Tasks;
using Logs;
using Shared.Systems.ObjectConnection;
using Systems.Electricity;

namespace Objects.Medical.Virology
{
	public class CureTester : MonoBehaviour, IAPCPowerable, IMultitoolMasterable
	{
		private PowerState _currentPowerState;

		[SerializeField] private SequenceAnalyzer connectedSequenceAnalyzer;

		[SerializeField] private SpriteHandler sampleSpriteHandler;
		[SerializeField] private SpriteHandler buttonSpriteHandler;
		[SerializeField] private ItemStorage itemStorage;

		[SerializeField] private UniversalObjectPhysics objectPhysics;
		[SerializeField] private AddressableAudioSource machineBeepSound;

		public ItemSlot CureItemSlot { get; private set; }

		private bool _isOnCooldown;
		public bool CanExamineSample => _isOnCooldown == false && _currentPowerState == PowerState.On;

		private void Start()
		{
			if (itemStorage == null)
			{
				Loggy.Error($"No Item Storage set on {gameObject.ExpensiveName()}!");
				return;
			}
			CureItemSlot = itemStorage.GetIndexedItemSlot(0);
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
				Inventory.ServerTransfer(interaction.HandSlot, CureItemSlot, ReplacementStrategy.DropOther);

				sampleSpriteHandler.SetSpriteVariant(1);
				Chat.AddActionMsgToChat(interaction.Performer,
						$"You place the {interaction.HandObject.ExpensiveName()} into the tester's primary slot.",
						$"{interaction.Performer.ExpensiveName()} places the {interaction.HandObject.ExpensiveName()} into the tester's primary slot.");
				return;
			}
			if (interaction.HandObject) return;

			Inventory.ServerTransfer(CureItemSlot, interaction.HandSlot);
			sampleSpriteHandler.SetSpriteVariant(0);
		}

		public void RequestExamineCure(PositionalHandApply interaction)
		{
			_ = AnimateButtonPress();

			if (_currentPowerState != PowerState.On)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"{gameObject.ExpensiveName()} is unpowered!");
				return;
			}
			_ = SoundManager.PlayNetworkedAtPosAsync(machineBeepSound, objectPhysics.OfficialPosition);

			if (connectedSequenceAnalyzer.ActiveSickness is null)
			{
				Chat.AddWarningMsgFromServer(interaction.Performer, "No pathogen sample in sequence analyser!");
				return;
			}

			if (CureItemSlot.ItemObject?.TryGetComponent<ReagentContainer>(out var container) == true)
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
			buttonSpriteHandler.SetSpriteVariant(0);
			_isOnCooldown = false;
		}

		public void StateUpdate(PowerState state)
		{
			_currentPowerState = state;
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
