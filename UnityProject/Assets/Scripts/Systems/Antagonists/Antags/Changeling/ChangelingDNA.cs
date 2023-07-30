using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Character;
using UnityEngine;
using Util;

namespace Changeling
{
	public class ChangelingDna : ICloneable
	{
		public int DnaID;
		public string PlayerName = "";
		public string Objectives;
		public CharacterSheet CharacterSheet;
		public JobType Job;
		public List<string> BodyClothesPrefabID = new();

		public void FormDna(PlayerScript playerDataForDna)
		{
			foreach (var clothe in playerDataForDna.Mind.Body.playerSprites.clothes)
			{
				if (clothe.Value.GameObjectReference != null)
				{
					BodyClothesPrefabID.Add(clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID);
				}
			}

			PlayerName = playerDataForDna.playerName;
			DnaID = playerDataForDna.Mind.bodyMobID;
			CharacterSheet = (CharacterSheet)playerDataForDna.Mind.CurrentCharacterSettings.Clone();
			try
			{
				Job = playerDataForDna.PlayerInfo.Job;
			}
			catch
			{
				Job = JobType.ASSISTANT;
				Logger.LogError($"[ChangelingDNA/FormDNA] When creating DNA can`t find {playerDataForDna.playerName} job", Category.Changeling);
			}
		}

		public void UpdateDma(PlayerScript playerDataForDna)
		{
			BodyClothesPrefabID.Clear();

			foreach (var clothe in playerDataForDna.Mind.Body.playerSprites.clothes)
			{
				if (clothe.Value.GameObjectReference != null)
					BodyClothesPrefabID.Add(clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID);
			}

			CharacterSheet = (CharacterSheet)playerDataForDna.characterSettings.Clone();
		}

		public object Clone()
		{
			string json = JsonConvert.SerializeObject(this);
			return JsonConvert.DeserializeObject<ChangelingDna>(json);
		}
	}
}