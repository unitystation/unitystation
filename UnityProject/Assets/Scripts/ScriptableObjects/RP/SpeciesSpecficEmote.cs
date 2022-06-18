using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.RP
{
	[CreateAssetMenu(fileName = "SpeciesSpecificEmote", menuName = "ScriptableObjects/RP/Emotes/SpeciesSpecificEmote")]
	public class SpeciesSpecificEmote : GenderedEmote
	{

		[SerializeField] private List<PlayerHealthData> allowedSpecies = new List<PlayerHealthData>();
		[SerializeField] private string wrongSpeciesText = "Your species can't do that!";

		public override void Do(GameObject player)
		{
			if (IsSameSpecies(player) == false)
			{
				Chat.AddExamineMsg(player, wrongSpeciesText);
				return;
			}
			base.Do(player);
		}

		public bool IsSameSpecies(GameObject mobToCheck)
		{
			if (mobToCheck.TryGetComponent<PlayerScript>(out var playerScript) == false) return false;

			if (RaceSOSingleton.TryGetRaceByName(playerScript.characterSettings.Species, out var race) == false) return false;

			return allowedSpecies.Contains(race);
		}
	}
}
