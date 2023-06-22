using Antagonists;
using Blob;
using Player;
using System.Collections;
using System.Collections.Generic;
using Systems.Ai;
using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Changeling/Changeling")]
	public class Changeling : Antagonist
	{
		//[SerializeField] private Objective aiTraitorObjective;
		[Header("Changeling attributes")]
		[Tooltip("Abilites what is gona be in changeling store")]
		[SerializeField] List<ChangelingAbilityBase> abilitiesToBuy = new List<ChangelingAbilityBase>();
		[Tooltip("Abilites what is gona added on round start to changeling hands")]
		[SerializeField] List<ChangelingAbilityBase> abilitiesDefault = new List<ChangelingAbilityBase>();

		public override Mind ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			return PlayerSpawn.NewSpawnPlayerV2(spawnRequest.Player, spawnRequest.RequestedOccupation, spawnRequest.CharacterSettings);
		}

		public override void AfterSpawn(Mind NewMind)
		{
			var ch = NewMind.Body.gameObject.AddComponent<ChangelingMain>();
			ch.SetAbilities(abilitiesToBuy, abilitiesDefault);
		}
	}
}