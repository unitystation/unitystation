using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Character;
using UnityEngine;
using Util;

namespace Changeling
{
	public class ChangelingDNA : ICloneable
	{
		public int DnaID;
		public string PlayerName = "";
		public string Objectives;
		public CharacterSheet CharacterSheet;
		public JobType Job;
		public List<string> BodyClothesPrefabID = new();

		public void FormDNA(PlayerScript playerDataForDNA)
		{
			foreach (var clothe in playerDataForDNA.Mind.Body.playerSprites.clothes)
			{
				if (clothe.Value.GameObjectReference != null)
				{
					BodyClothesPrefabID.Add(clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID);
				}
			}

			PlayerName = playerDataForDNA.playerName;
			DnaID = playerDataForDNA.Mind.bodyMobID;
			CharacterSheet = (CharacterSheet)playerDataForDNA.Mind.CurrentCharacterSettings.Clone();
			try
			{
				Job = playerDataForDNA.PlayerInfo.Job;
			}
			catch
			{
				Job = JobType.ASSISTANT;
				Logger.LogError("When creating DNA can`t find target job", Category.Changeling);
			}
		}

		public void UpdateDNA(PlayerScript playerDataForDNA)
		{
			BodyClothesPrefabID.Clear();

			foreach (var clothe in playerDataForDNA.Mind.Body.playerSprites.clothes)
			{
				if (clothe.Value.GameObjectReference != null)
					BodyClothesPrefabID.Add(clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID);
			}

			CharacterSheet = (CharacterSheet)playerDataForDNA.characterSettings.Clone();
		}

		public object Clone()
		{
			string json = JsonConvert.SerializeObject(this);
			return JsonConvert.DeserializeObject<ChangelingDNA>(json);
		}
	}
}