using Clothing;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Logs;
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
			try
			{
				foreach (var clothe in playerDataForDna.Mind.Body.playerSprites.clothes)
				{
					if (clothe.Value.GameObjectReference != null)
					{
						BodyClothesPrefabID.Add(clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID);
					}
				}
			} catch
			{
				Loggy.LogError($"[ChangelingDNA/FormDNA] When creating DNA can`t find {playerDataForDna.playerName} Body", Category.Changeling);
				BodyClothesPrefabID = new ();
			}

			for (int i = 0; i < BodyClothesPrefabID.Count; i++)
			{
				string id = BodyClothesPrefabID[i];
				if (CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[id].TryGetComponent<ClothingSlots>(out var slots))
				{
					if (slots.NamedSlotFlagged.HasFlag(NamedSlotFlagged.Uniform))
					{
						var exchange = id;
						BodyClothesPrefabID[i] = BodyClothesPrefabID[0];
						BodyClothesPrefabID[0] = exchange;
						break;
					}
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
				Loggy.LogError($"[ChangelingDNA/FormDNA] When creating DNA can`t find {playerDataForDna.playerName} job", Category.Changeling);
			}
		}

		public void UpdateDna(PlayerScript playerDataForDna)
		{
			BodyClothesPrefabID.Clear();

			foreach (var clothe in playerDataForDna.Mind.Body.playerSprites.clothes)
			{
				if (clothe.Value.GameObjectReference != null)
					BodyClothesPrefabID.Add(clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID);
			}

			CharacterSheet = (CharacterSheet)playerDataForDna.characterSettings.Clone();
		}

		public void UpdateDna(ChangelingDna dna)
		{
			BodyClothesPrefabID.Clear();

			foreach (var clothe in dna.BodyClothesPrefabID)
			{
				BodyClothesPrefabID.Add(clothe);
			}

			CharacterSheet = (CharacterSheet)dna.CharacterSheet.Clone();
		}

		public object Clone()
		{
			string json = JsonConvert.SerializeObject(this);
			return JsonConvert.DeserializeObject<ChangelingDna>(json);
		}
	}
}