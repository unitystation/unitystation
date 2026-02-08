using System.Collections.Generic;
using Mirror;
using UnityEngine;
using US13.Clothing;
using US13.Core.Lifecycle;
using US13.Player;
using US13.Systems.Antagonists.Antags.Changeling;
using US13.Systems.Lobby;
using Util;

namespace US13.Systems.Antagonists.Antags
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/ChangelingImposter/ChangelingImposter")]
	public class ChangelingImposter : Antagonist
	{
		public override Mind ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			if (spawnRequest.CharacterSettings.GetRaceSoNoValidation().Base.allowedToChangeling == false)
			{
				var racesToAdd = new List<PlayerHealthData>();

				foreach (PlayerHealthData x in RaceSOSingleton.GetPlayerSpecies())
				{
					if (x.Base.allowedToChangeling)
					{
						racesToAdd.Add(x);
					}
				}

				CharacterSheet chSh = CharacterSheet.GenerateRandomCharacter(racesToAdd);
				chSh.SerialisedBodyPartCustom = new List<global::US13.UI.Systems.Lobby.CustomisationStorage>
				{
					new global::US13.UI.Systems.Lobby.CustomisationStorage()
				};

				return PlayerSpawn.NewSpawnCharacterV2(spawnRequest.Player, spawnRequest.RequestedOccupation, chSh);
			}

			// spawn them normally, with their preferred occupation
			return PlayerSpawn.NewSpawnCharacterV2(spawnRequest.Player, spawnRequest.RequestedOccupation, spawnRequest.CharacterSettings);
		}

		public override void AfterSpawn(Mind NewMind)
		{
			var ch = NewMind.Body.playerHealth.brain.gameObject.GetComponent<ChangelingMain>();
			SweetExtensions.NetEnable(ch);

			PlayerSpawn.TransferOwnershipFromToConnection(NewMind.ControlledBy, null, ch.gameObject.GetComponent<NetworkIdentity>());

			ch.Init(NewMind);
		}
	}
}