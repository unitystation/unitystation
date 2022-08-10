using UnityEngine;
using System;
using Items.Science;

namespace Systems.Research.Objects
{
	public class ArtifactAnalyser : ResearchPointMachine, ICheckedInteractable<HandApply>
	{
		public int storedRP;
		public ItemStorage itemStorage { get; set; }

		[HideInInspector]
		public ArtifactSliver artifactSliver { get; set; }

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
			foreach(GameObject obj in artifactSliver.Composition)
			{
				Spawn.ServerPrefab(obj, gameObject.AssumedWorldPosServer());
			}
			Inventory.ServerDespawn(itemStorage.GetIndexedItemSlot(0));
			artifactSliver = null;
			UpdateGUI();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.HandSlot.IsEmpty) return false;
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if(Validations.HasItemTrait(interaction.UsedObject, sampleTrait)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if(itemStorage.GetIndexedItemSlot(0).IsEmpty)
			{
				Inventory.ServerTransfer(interaction.HandSlot, itemStorage.GetIndexedItemSlot(0));
				artifactSliver = itemStorage.GetIndexedItemSlot(0).ItemObject.GetComponent<ArtifactSliver>();

				Chat.AddActionMsgToChat(interaction.Performer, "You insert the sample into the analyser.",
					interaction.Performer.ExpensiveName() + " inserts the sample into the analyser.");

				UpdateGUI();
			}
			else
			{
				Chat.AddActionMsgToChat(interaction.Performer, gameObject.ExpensiveName() + " already contains a sample", gameObject.ExpensiveName() + " already contains a sample");
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
