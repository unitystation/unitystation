using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.ObjectConnection;
using Items.Science;

namespace Systems.Research.Objects
{
	public class ArtifactAnalyser : NetworkBehaviour, ICheckedInteractable<HandApply>, IMultitoolSlaveable
	{
		public int storedRP;

		public ResearchServer researchServer;
		private RegisterObject registerObject;
		public ItemStorage itemStorage;

		[HideInInspector]
		public ArtifactSliver artifactSliver;


		[HideInInspector]
		public AnalyserState analyserState;

		public delegate void StateChange();
		public static event StateChange stateChange;

		private void Awake()
		{
			analyserState = AnalyserState.Idle;
			itemStorage = GetComponent<ItemStorage>();
			registerObject = GetComponent<RegisterObject>();
		}

		public void EjectSample()
		{
			itemStorage.ServerDropAll();
		}

		public void DestroySample()
		{
			foreach(GameObject obj in artifactSliver.Composition)
			{
				Spawn.ServerPrefab(obj, registerObject.WorldPositionServer, transform.parent, count: 1);
			}
			Inventory.ServerDespawn(itemStorage.GetIndexedItemSlot(0));
			artifactSliver = null;
			UpdateGUI();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.HandSlot.IsEmpty) return false;
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if(Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.everyTraitOutThere[402])) return true;

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
			// Delegate calls method in all subscribers when material is changed
			if (stateChange != null)
			{
				stateChange();
			}
		}

		#region Multitool Interaction

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.ResearchServer;
		IMultitoolMasterable IMultitoolSlaveable.Master => researchServer;
		bool IMultitoolSlaveable.RequireLink => false;

		bool IMultitoolSlaveable.TrySetMaster(PositionalHandApply interaction, IMultitoolMasterable master)
		{
			SetMaster(master);
			return true;
		}

		void IMultitoolSlaveable.SetMasterEditor(IMultitoolMasterable master)
		{
			SetMaster(master);
		}

		private void SetMaster(IMultitoolMasterable master)
		{
			if (master is ResearchServer server && server != researchServer)
			{
				SubscribeToServerEvent(server);
			}
			else if (researchServer != null)
			{
				UnSubscribeFromServerEvent();
			}
			UpdateGUI();
		}

		public void SubscribeToServerEvent(ResearchServer server)
		{
			UnSubscribeFromServerEvent();
			researchServer = server;

		}

		public void UnSubscribeFromServerEvent()
		{
			if (researchServer == null) return;
			researchServer = null;
		}
		#endregion
	}

	public enum AnalyserState
	{
		Scanning = 1,
		Idle = 2,
		Destroying = 3,
	}
}
