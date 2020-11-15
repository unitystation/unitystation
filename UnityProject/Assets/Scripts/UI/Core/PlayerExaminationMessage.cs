using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Player;
using UnityEngine;

public class PlayerExaminationMessage : ServerMessage
{
	public string VisibleName;
	public string Species;
	public string Job;
	public string Status;

	/// <summary>
	/// List of informations divided by ';'
	/// </summary>
	public string AdditionalInformations;
	public uint ItemStorage;
	
	public bool Observed;

	public override void Process()
	{
		LoadNetworkObject(ItemStorage);

		var storageObject = NetworkObject;
		if (storageObject == null)
		{
			Logger.LogWarningFormat("Client could not find player storage with id {0}", Category.Inventory, ItemStorage);
			return;
		}

		var itemStorage = storageObject.GetComponent<ItemStorage>();
		if(Observed)
			UIManager.PlayerExaminationWindow.ExaminePlayer(itemStorage, VisibleName, Species, Job, Status, AdditionalInformations);
		else
			UIManager.PlayerExaminationWindow.CloseWindow();
	}

	/// <summary>
	/// Informs the recipient that they can now show/hide the player examination UI
	/// </summary>
	public static void Send(GameObject recipient, ItemStorage itemStorage, string visibleName, string species, string job, string status, string additionalInformations, bool observed)
	{
		var msg = new PlayerExaminationMessage()
		{
			ItemStorage = itemStorage.gameObject.NetId(),
			VisibleName = visibleName,
			Species = species,
			Job = job,
			Status = status,
			AdditionalInformations = additionalInformations,
			Observed = observed
		};

		msg.SendTo(recipient);
	}

	public static void Send(GameObject recipient, ExaminablePlayer examinablePlayer, bool observed)
	{
		var msg = new PlayerExaminationMessage()
		{
			ItemStorage = examinablePlayer.gameObject.NetId(),
			VisibleName = examinablePlayer.GetPlayerNameString(),
			Species = examinablePlayer.GetPlayerSpeciesString(),
			Job = examinablePlayer.GetPlayerJobString(),
			Status = examinablePlayer.GetPlayerStatusString(),
			AdditionalInformations = examinablePlayer.GetAdditionalInformations(),
			Observed = observed
		};

		msg.SendTo(recipient);
	}
}