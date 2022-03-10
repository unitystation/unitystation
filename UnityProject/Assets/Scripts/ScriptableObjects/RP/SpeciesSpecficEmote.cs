using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.RP
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/SpeciesSpecificEmote")]
	public class SpeciesSpecificEmote : GenderedEmote
	{

		[SerializeField] private List<PlayerHealthData> allowedSpecies = new List<PlayerHealthData>();
		[SerializeField] private string wrongSpeciesText = "Your species can't do that!";

		public override void Do(GameObject player)
		{
			if (player.TryGetComponent<PlayerScript>(out var playerScript) == false)
			{
				FailText(player, FailType.Normal);
				return;
			}
			var race = CharacterSettings.GetRaceData(playerScript.characterSettings);
			if (race == null || allowedSpecies.Contains(race) == false)
			{
				Chat.AddExamineMsg(player, wrongSpeciesText);
				return;
			}
			base.Do(player);
		}
	}
}