using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items.Science;
using Objects.Research;
using Mirror;
using Systems.ObjectConnection;

namespace Objects.Research
{
	public class ArtifactConsole : NetworkBehaviour, ICheckedInteractable<HandApply>, IMultitoolSlaveable
	{
		private RegisterObject registerObject;
		public ItemStorage itemStorage;

		public Artifact connectedArtifact;
		public ArtifactDataDisk dataDisk;

		[SyncVar] public ArtifactData inputData = new ArtifactData();

		public delegate void StateChange();
		public static event StateChange stateChange;

		private void Awake()
		{
			itemStorage = GetComponent<ItemStorage>();
			registerObject = GetComponent<RegisterObject>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.HandSlot.IsEmpty) return false;
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.everyTraitOutThere[403])) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (itemStorage.GetIndexedItemSlot(0).IsEmpty)
			{
				Inventory.ServerTransfer(interaction.HandSlot, itemStorage.GetIndexedItemSlot(0));
				dataDisk = itemStorage.GetIndexedItemSlot(0).ItemObject.GetComponent<ArtifactDataDisk>();

				Chat.AddActionMsgToChat(interaction.Performer, "You insert the drive into the console.",
					interaction.Performer.ExpensiveName() + " inserts the dirve into the console.");

				UpdateGUI();
			}
			else
			{
				Chat.AddActionMsgToChat(interaction.Performer, gameObject.ExpensiveName() + " already contains a drive", gameObject.ExpensiveName() + " already contains a drive");
			}

		}

		public void DropDisk()
		{
			itemStorage.ServerDropAll();
			dataDisk = null;
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

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.Artifact;
		IMultitoolMasterable IMultitoolSlaveable.Master => connectedArtifact;
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
			if (master is Artifact arti && arti != connectedArtifact)
			{
				SubscribeToServerEvent(arti);
			}
			else if (connectedArtifact != null)
			{
				UnSubscribeFromServerEvent();
			}
			UpdateGUI();
		}

		public void SubscribeToServerEvent(Artifact arti)
		{
			UnSubscribeFromServerEvent();
			connectedArtifact = arti;

		}

		public void UnSubscribeFromServerEvent()
		{
			if (connectedArtifact == null) return;
			connectedArtifact = null;
		}
		#endregion
	}
}
