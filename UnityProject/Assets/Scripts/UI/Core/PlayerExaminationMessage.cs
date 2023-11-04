using System.Collections;
using System.Collections.Generic;
using Logs;
using Messages.Server;
using Mirror;
using Player;
using UnityEngine;

public class PlayerExaminationMessage : ServerMessage<PlayerExaminationMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public string VisibleName;
		public string Species;
		public string Job;
		public string Status;

		/// <summary>
		/// Extra information to be displayed on the extended examination view
		/// </summary>
		public string AdditionalInformation;
		public uint ItemStorage;

		public bool Observed;

		public string slotInformation;
	}

	public override void Process(NetMessage msg)
	{
		LoadNetworkObject(msg.ItemStorage);

		var storageObject = NetworkObject;
		if (storageObject == null)
		{
			Loggy.LogWarningFormat("Client could not find player storage with id {0}", Category.PlayerInventory, msg.ItemStorage);
			return;
		}

		var itemStorage = storageObject.GetComponent<DynamicItemStorage>();
		if (msg.Observed)
		{
			itemStorage.UpdateSlots(msg.slotInformation, msg.slotInformation);
			UIManager.PlayerExaminationWindow.ExaminePlayer(itemStorage, msg.VisibleName, msg.Species, msg.Job, msg.Status, msg.AdditionalInformation);
		}
		else
			UIManager.PlayerExaminationWindow.CloseWindow();
	}
	public static void Send(GameObject recipient, ExaminablePlayer examinablePlayer, bool observed)
	{

		var msg = new NetMessage()
		{
			slotInformation = examinablePlayer.GetComponent<DynamicItemStorage>().GetSetData,
			ItemStorage = examinablePlayer.gameObject.NetId(),
			VisibleName = examinablePlayer.GetPlayerNameString(),
			Species = examinablePlayer.GetPlayerSpeciesString(),
			Job = examinablePlayer.GetPlayerJobString(),
			Status = examinablePlayer.GetPlayerStatusString(),
			AdditionalInformation = examinablePlayer.GetAdditionalInformation(),
			Observed = observed
		};

		SendTo(recipient, msg);
	}
}