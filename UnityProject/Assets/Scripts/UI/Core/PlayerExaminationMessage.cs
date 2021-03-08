using System.Collections;
using System.Collections.Generic;
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
	}

	public override void Process(NetMessage msg)
	{
		LoadNetworkObject(msg.ItemStorage);

		var storageObject = NetworkObject;
		if (storageObject == null)
		{
			Logger.LogWarningFormat("Client could not find player storage with id {0}", Category.PlayerInventory, msg.ItemStorage);
			return;
		}

		var itemStorage = storageObject.GetComponent<ItemStorage>();
		if(msg.Observed)
			UIManager.PlayerExaminationWindow.ExaminePlayer(itemStorage, msg.VisibleName, msg.Species, msg.Job, msg.Status, msg.AdditionalInformation);
		else
			UIManager.PlayerExaminationWindow.CloseWindow();
	}

	/// <summary>
	/// Informs the recipient that they can now show/hide the player examination UI
	/// </summary>
	public static void Send(GameObject recipient, ItemStorage itemStorage, string visibleName, string species, string job, string status, string additionalInformations, bool observed)
	{
		var msg = new NetMessage()
		{
			ItemStorage = itemStorage.gameObject.NetId(),
			VisibleName = visibleName,
			Species = species,
			Job = job,
			Status = status,
			AdditionalInformation = additionalInformations,
			Observed = observed
		};

		SendTo(recipient, msg);
	}

	public static void Send(GameObject recipient, ExaminablePlayer examinablePlayer, bool observed)
	{
		var msg = new NetMessage()
		{
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