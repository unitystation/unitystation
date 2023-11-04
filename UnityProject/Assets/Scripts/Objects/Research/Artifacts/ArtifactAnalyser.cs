using UnityEngine;
using System;
using Items.Science;
using Mirror;

namespace Systems.Research.Objects
{
	public class ArtifactAnalyser : ResearchPointMachine, ICheckedInteractable<HandApply>
	{
		[SyncVar] public int storedRP;

		public ItemStorage itemStorage { get; set; }

		public ArtifactSliver ArtifactSliver { get; set; }

		[SerializeField]
		private ItemTrait sampleTrait;

		[HideInInspector]
		public AnalyserState analyserState { get; set; }

		public Action StateChange;

		private void Awake()
		{
			analyserState = AnalyserState.Idle;
			itemStorage = GetComponent<ItemStorage>();
		}

		public void EjectSample()
		{
			itemStorage.ServerDropAll();
		}

		public void DestroySample()
		{
			foreach(GameObject obj in ArtifactSliver.Composition)
			{
				Spawn.ServerPrefab(obj, gameObject.AssumedWorldPosServer());
			}
			Inventory.ServerDespawn(itemStorage.GetIndexedItemSlot(0));
			ArtifactSliver = null;
			UpdateGUI();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.HandSlot.IsEmpty) return false;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if(Validations.HasItemTrait(interaction.UsedObject, sampleTrait)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if(itemStorage.GetIndexedItemSlot(0).IsEmpty)
			{
				Inventory.ServerTransfer(interaction.HandSlot, itemStorage.GetIndexedItemSlot(0));
				ArtifactSliver = itemStorage.GetIndexedItemSlot(0).ItemObject.GetComponent<ArtifactSliver>();

				Chat.AddActionMsgToChat(interaction.Performer, "You insert the sample into the analyser.",
					interaction.Performer.ExpensiveName() + " inserts the sample into the analyser.");

				UpdateGUI();
			}
			else
			{
				Chat.AddExamineMsg(interaction.Performer, gameObject.ExpensiveName() + " already contains a sample");
			}

		}

		public void SetState(AnalyserState newState)
		{
			analyserState = newState;
		}

		private void UpdateGUI()
		{
			StateChange?.Invoke();
		}

	}

	public enum AnalyserState
	{
		Scanning = 1,
		Idle = 2,
		Destroying = 3,
	}
}
