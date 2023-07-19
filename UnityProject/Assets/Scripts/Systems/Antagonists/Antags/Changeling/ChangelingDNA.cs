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
	public class ChangelingDNA : NetworkBehaviour
	{
		private ChangelingMain changelingOwner = null;
		public ChangelingMain ChangelingOwner => changelingOwner;
		[SyncVar] private CharacterSheet characterSheet;
		public CharacterSheet CharacterSheet => characterSheet;
		[SyncVar] private int dnaID;
		public int DnaID => dnaID;
		[SyncVar] List<string> bodyClothesPrefabID = new ();
		public List<string> BodyClothesPrefabID => bodyClothesPrefabID;

		[SyncVar] private string playerName = "";
		public string PlayerName => playerName;

		[SyncVar] private JobType job;
		public JobType Job => job;

		[SyncVar] private string objectives;
		public string Objectives => objectives;

		public void FormDNA(PlayerScript playerDataForDNA, ChangelingMain changelingOwnerSet)
		{
			foreach (var clothe in playerDataForDNA.Mind.Body.playerSprites.clothes)
			{
				if (clothe.Value.GameObjectReference != null)
				{
					bodyClothesPrefabID.Add(clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID);
					// var obj = CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID]; 
				}
			}

			//dnaID = playerData.Mind.Body.netId;
			playerName = playerDataForDNA.playerName;
			dnaID = playerDataForDNA.Mind.bodyMobID;
			characterSheet = (CharacterSheet)playerDataForDNA.Mind.CurrentCharacterSettings.Clone();
			changelingOwner = changelingOwnerSet;
			try
			{
				job = playerDataForDNA.PlayerInfo.Job;
			} catch
			{
				job = JobType.ASSISTANT;
				Logger.LogError("When creating DNA can`t find target job", Category.Changeling);
			}
		}

		public void UpdateDNA(PlayerScript playerDataForDNA, ChangelingMain changelingOwnerSet)
		{
			bodyClothesPrefabID.Clear();

			foreach (var clothe in playerDataForDNA.Mind.Body.playerSprites.clothes)
			{
				if (clothe.Value.GameObjectReference != null)
					bodyClothesPrefabID.Add(clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID);
			}

			characterSheet = (CharacterSheet)playerDataForDNA.characterSettings.Clone();
			changelingOwner = changelingOwnerSet;
		}
	}
}