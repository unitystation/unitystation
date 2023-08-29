using Antagonists;
using Blob;
using Messages.Server;
using Mirror;
using Player;
using System.Collections;
using System.Collections.Generic;
using Systems.Ai;
using Systems.Character;
using UI.Systems.MainHUD.UI_Bottom;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Changeling/Changeling")]
	public class Changeling : Antagonist
	{
		public override Mind ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			if (spawnRequest.CharacterSettings.GetRaceSoNoValidation().Base.allowedToChangeling == false)
			{
				var racesToAdd = new List<PlayerHealthData>();

				foreach (PlayerHealthData x in RaceSOSingleton.Instance.Races)
				{
					if (x.Base.allowedToChangeling)
					{
						racesToAdd.Add(x);
					}
				}

				CharacterSheet chSh = CharacterSheet.GenerateRandomCharacter(racesToAdd);
				chSh.SerialisedBodyPartCustom = new List<UI.CharacterCreator.CustomisationStorage>
				{
					new UI.CharacterCreator.CustomisationStorage()
				};

				return PlayerSpawn.NewSpawnPlayerV2(spawnRequest.Player, spawnRequest.RequestedOccupation, chSh);
			}

			// spawn them normally, with their preferred occupation
			return PlayerSpawn.NewSpawnPlayerV2(spawnRequest.Player, spawnRequest.RequestedOccupation, spawnRequest.CharacterSettings);
		}

		public override void AfterSpawn(Mind NewMind)
		{
			var ch = NewMind.Body.playerHealth.brain.gameObject.GetComponent<ChangelingMain>();
			ch.NetEnable();

			PlayerSpawn.TransferOwnershipFromToConnection(NewMind.ControlledBy, null, ch.gameObject.GetComponent<NetworkIdentity>());

			ch.Init(NewMind);
		}
	}
}