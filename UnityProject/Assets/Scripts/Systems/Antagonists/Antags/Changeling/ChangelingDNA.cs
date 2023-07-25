using HealthV2;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Character;
using UI.CharacterCreator;
using UnityEngine;
using Util;

namespace Changeling
{
	//public class ChangelingDNA// : NetworkBehaviour
	//{
	//	//private ChangelingMain changelingOwner = null;
	//	//public ChangelingMain ChangelingOwner => changelingOwner;
	//	public CharacterSheet CharacterSheet;
	//	//public CharacterSheet CharacterSheet => characterSheet;
	//	public int DnaID;
	//	//public int DnaID => dnaID;
	//	public List<string> BodyClothesPrefabID = new ();
	//	//public List<string> BodyClothesPrefabID => bodyClothesPrefabID;

	//	public string PlayerName = "";
	//	//public string PlayerName => playerName;

	//	public JobType Job;
	//	//public JobType Job => job;

	//	public string Objectives;
	//	//public string Objectives => objectives;

	//	public void FormDNA(PlayerScript playerDataForDNA)
	//	{
	//		foreach (var clothe in playerDataForDNA.Mind.Body.playerSprites.clothes)
	//		{
	//			if (clothe.Value.GameObjectReference != null)
	//			{
	//				BodyClothesPrefabID.Add(clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID);
	//				// var obj = CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID]; 
	//			}
	//		}

	//		//dnaID = playerData.Mind.Body.netId;
	//		PlayerName = playerDataForDNA.playerName;
	//		DnaID = playerDataForDNA.Mind.bodyMobID;
	//		CharacterSheet = (CharacterSheet)playerDataForDNA.Mind.CurrentCharacterSettings.Clone();
	//		try
	//		{
	//			Job = playerDataForDNA.PlayerInfo.Job;
	//		} catch
	//		{
	//			Job = JobType.ASSISTANT;
	//			Logger.LogError("When creating DNA can`t find target job", Category.Changeling);
	//		}
	//	}

	//	public void UpdateDNA(PlayerScript playerDataForDNA)
	//	{
	//		BodyClothesPrefabID.Clear();

	//		foreach (var clothe in playerDataForDNA.Mind.Body.playerSprites.clothes)
	//		{
	//			if (clothe.Value.GameObjectReference != null)
	//				BodyClothesPrefabID.Add(clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID);
	//		}

	//		CharacterSheet = (CharacterSheet)playerDataForDNA.characterSettings.Clone();
	//	}
	//}
}