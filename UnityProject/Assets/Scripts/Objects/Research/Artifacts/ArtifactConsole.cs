using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Items.Science;
using Objects.Research;
using Mirror;
using Shared.Systems.ObjectConnection;

namespace Objects.Research
{
	public class ArtifactConsole : NetworkBehaviour, ICheckedInteractable<HandApply>, IMultitoolSlaveable
	{
		private ItemStorage itemStorage;

		public Artifact ConnectedArtifact { get; set; }

		public ArtifactDataDisk dataDisk { get; set; }

		[NonSerialized, SyncVar(hook = nameof(SyncConsoleData))] internal ArtifactData InputData = new ArtifactData();

		[SyncVar,NonSerialized] public bool HasDisk = false;

		[SerializeField] ItemTrait ArtifactDiskTrait = null;

		public Action StateChange;

		private void Awake()
		{
			itemStorage = GetComponent<ItemStorage>();
		}

		[Command(requiresAuthority = false)]
		internal void CmdSetInputData(ArtifactData InputDataClient, NetworkConnectionToClient sender = null) //TODO This is insecure due to hacked client can say anything in ArtifactData
		{
			if (sender == null) return;
			if (Validations.CanApply(PlayerList.Instance.Get(sender).Script, this.gameObject, NetworkSide.Server, false, ReachRange.Standard) == false) return;
			InputData = InputDataClient;
		}

		[Server]
		internal void SetInputDataServer(ArtifactData InputDataServer)
		{
			InputData = InputDataServer;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.HandSlot.IsEmpty) return false;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (Validations.HasItemTrait(interaction.UsedObject, ArtifactDiskTrait)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (itemStorage.GetIndexedItemSlot(0).IsEmpty)
			{
				Inventory.ServerTransfer(interaction.HandSlot, itemStorage.GetIndexedItemSlot(0));
				dataDisk = itemStorage.GetIndexedItemSlot(0).ItemObject.GetComponent<ArtifactDataDisk>();
				HasDisk = true;

				Chat.AddActionMsgToChat(interaction.Performer, "You insert the drive into the console.",
					$"{interaction.Performer.ExpensiveName()} inserts the drive into the console.");

				UpdateGUI();
			}
			else
			{
				Chat.AddExamineMsg(interaction.Performer, $"{gameObject.ExpensiveName()} already contains a drive");
			}

		}

		public void DropDisk()
		{
			itemStorage.ServerDropAll();
			HasDisk = false;
			dataDisk = null;
		}

		private void UpdateGUI()
		{
			StateChange?.Invoke();
		}

		private void SyncConsoleData(ArtifactData oldData, ArtifactData newData)
		{
			InputData = newData;
			UpdateGUI();
		}

		#region Multitool Interaction

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.Artifact;
		IMultitoolMasterable IMultitoolSlaveable.Master => ConnectedArtifact;
		bool IMultitoolSlaveable.RequireLink => false;

		bool IMultitoolSlaveable.TrySetMaster(GameObject performer, IMultitoolMasterable master)
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
			if (master is Artifact arti && arti != ConnectedArtifact)
			{
				SubscribeToServerEvent(arti);
			}
			else if (ConnectedArtifact != null)
			{
				UnSubscribeFromServerEvent();
			}
			UpdateGUI();
		}

		public void SubscribeToServerEvent(Artifact arti)
		{
			UnSubscribeFromServerEvent();
			ConnectedArtifact = arti;

		}

		public void UnSubscribeFromServerEvent()
		{
			if (ConnectedArtifact == null) return;
			ConnectedArtifact = null;
		}
		#endregion
	}
}
