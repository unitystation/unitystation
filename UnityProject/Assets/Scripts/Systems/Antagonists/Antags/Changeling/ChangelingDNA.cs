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
		private PlayerScript playerData = null;
		public PlayerScript PlayerData => playerData;
		[SyncVar] private CharacterSheet characterSheet;
		public CharacterSheet CharacterSheet => characterSheet;
		[SyncVar] private string dnaID;
		public string DnaID => dnaID;
		[SerializeField] private Sprite preview;
		//public Sprite Preview => preview;
		[SyncVar] List<string> bodyClothesPrefabID = new ();
		public List<string> BodyClothesPrefabID => bodyClothesPrefabID;

		public void FormDNA(PlayerScript playerDataForDNA, ChangelingMain changelingOwnerSet)
		{
			playerData = playerDataForDNA;

			foreach (var clothe in playerDataForDNA.Mind.Body.playerSprites.clothes)
			{
				if (clothe.Value.GameObjectReference != null)
				{
					bodyClothesPrefabID.Add(clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID);
					// how to get object
					// var obj = CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID]; 
				}
			}

			//dnaID = playerData.Mind.Body.netId;
			dnaID = playerData.Mind.Body.GetComponent<PrefabTracker>().ForeverID;
			characterSheet = (CharacterSheet)playerData.characterSettings.Clone();
			changelingOwner = changelingOwnerSet;
		}

		public void UpdateDNA(PlayerScript playerDataForDNA, ChangelingMain changelingOwnerSet)
		{
			bodyClothesPrefabID.Clear();

			foreach (var clothe in playerDataForDNA.Mind.Body.playerSprites.clothes)
			{
				if (clothe.Value.GameObjectReference != null)
					bodyClothesPrefabID.Add(clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID);
			}

			characterSheet = (CharacterSheet)playerData.characterSettings.Clone();
			changelingOwner = changelingOwnerSet;
		}
	}
}